// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { acquireDotnetInteractive } from './acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from './interfaces';
import { ClientMapper } from './clientMapper';
import { getEol, isAzureDataStudio, toNotebookDocument } from './vscodeUtilities';
import { DotNetPathManager, KernelIdForJupyter } from './extension';
import { computeToolInstallArguments, executeSafe, executeSafeAndLog, extensionToDocumentType, getVersionNumber } from './utilities';

import * as notebookControllers from '../notebookControllers';
import * as ipynbUtilities from './ipynbUtilities';
import { ReportChannel } from './interfaces/vscode-like';
import { jupyterViewType } from './interactiveNotebook';
import { NotebookParserServer } from './notebookParserServer';
import { PromiseCompletionSource } from './dotnet-interactive/genericChannel';
import * as versionSpecificFunctions from '../versionSpecificFunctions';

export function registerAcquisitionCommands(context: vscode.ExtensionContext, diagnosticChannel: ReportChannel) {
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const minDotNetInteractiveVersion = config.get<string>('minimumInteractiveToolVersion');
    const interactiveToolSource = config.get<string>('interactiveToolSource');

    let cachedInstallArgs: InstallInteractiveArgs | undefined = undefined;
    let acquirePromise: Promise<InteractiveLaunchOptions> | undefined = undefined;

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.acquire', async (args?: InstallInteractiveArgs | string | undefined): Promise<InteractiveLaunchOptions | undefined> => {
        try {
            const installArgs = computeToolInstallArguments(args);
            DotNetPathManager.setDotNetPath(installArgs.dotnetPath);

            if (cachedInstallArgs) {
                if (installArgs.dotnetPath !== cachedInstallArgs.dotnetPath ||
                    installArgs.toolVersion !== cachedInstallArgs.toolVersion) {
                    // if specified install args are different than what we previously computed, invalidate the acquisition
                    acquirePromise = undefined;
                }
            }

            if (!acquirePromise) {
                const installationPromiseCompletionSource = new PromiseCompletionSource<void>();
                acquirePromise = acquireDotnetInteractive(
                    installArgs,
                    minDotNetInteractiveVersion!,
                    context.globalStorageUri.fsPath,
                    getInteractiveVersion,
                    createToolManifest,
                    (version: string) => {
                        vscode.window.withProgress(
                            { location: vscode.ProgressLocation.Notification, title: `Installing .NET Interactive version ${version}...` },
                            (_progress, _token) => installationPromiseCompletionSource.promise);
                    },
                    installInteractiveTool,
                    () => { installationPromiseCompletionSource.resolve(); });
            }
            const launchOptions = await acquirePromise;
            return launchOptions;
        } catch (err) {
            diagnosticChannel.appendLine(`Error acquiring dotnet-interactive tool: ${err}`);
        }
    }));

    async function createToolManifest(dotnetPath: string, globalStoragePath: string): Promise<void> {
        const result = await executeSafeAndLog(diagnosticChannel, 'create-tool-manifest', dotnetPath, ['new', 'tool-manifest'], globalStoragePath);
        if (result.code !== 0) {
            throw new Error(`Unable to create local tool manifest.  Command failed with code ${result.code}.\n\nSTDOUT:\n${result.output}\n\nSTDERR:\n${result.error}`);
        }
    }

    async function installInteractiveTool(args: InstallInteractiveArgs, globalStoragePath: string): Promise<void> {
        // remove previous tool; swallow errors in case it's not already installed
        let uninstallArgs = [
            'tool',
            'uninstall',
            'Microsoft.dotnet-interactive'
        ];
        await executeSafeAndLog(diagnosticChannel, 'tool-uninstall', args.dotnetPath, uninstallArgs, globalStoragePath);

        let toolArgs = [
            'tool',
            'install',
            '--add-source',
            interactiveToolSource!,
            '--ignore-failed-sources',
            'Microsoft.dotnet-interactive'
        ];
        if (args.toolVersion) {
            toolArgs.push('--version', args.toolVersion);
        }

        return new Promise(async (resolve, reject) => {
            const result = await executeSafeAndLog(diagnosticChannel, 'tool-install', args.dotnetPath, toolArgs, globalStoragePath);
            if (result.code === 0) {
                resolve();
            } else {
                reject();
            }
        });
    }
}

export function registerKernelCommands(context: vscode.ExtensionContext, clientMapper: ClientMapper) {

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.restartCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        if (!notebook) {
            if (!vscode.window.activeNotebookEditor) {
                // no notebook to operate on
                return;
            }

            notebook = versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
        }

        if (notebook) {
            await vscode.commands.executeCommand('dotnet-interactive.stopCurrentNotebookKernel', notebook);
            const _client = await clientMapper.getOrAddClient(notebook.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        if (!notebook) {
            if (!vscode.window.activeNotebookEditor) {
                // no notebook to operate on
                return;
            }

            notebook = versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
        }

        if (notebook) {
            for (const cell of notebook.getCells()) {
                notebookControllers.endExecution(cell, false);
            }

            clientMapper.closeClient(notebook.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopAllNotebookKernels', async () => {
        vscode.workspace.notebookDocuments
            .filter(document => clientMapper.isDotNetClient(document.uri))
            .forEach(async document => await vscode.commands.executeCommand('dotnet-interactive.stopCurrentNotebookKernel', document));
    }));
}

export function registerFileCommands(context: vscode.ExtensionContext, parserServer: NotebookParserServer, clientMapper: ClientMapper) {

    const eol = getEol();

    const notebookFileFilters = {
        '.NET Interactive Notebooks': ['dib', 'dotnet-interactive'],
        'Jupyter Notebooks': ['ipynb'],
    };

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebook', async () => {
        if (isAzureDataStudio(context)) {
            // only `.dib` is allowed
            await vscode.commands.executeCommand('dotnet-interactive.newNotebookDib');
        } else {
            // offer to create either `.dib` or `.ipynb`
            const newDibNotebookText = `Create as '.dib'`;
            const newIpynbNotebookText = `Create as '.ipynb'`;
            const selected = await vscode.window.showQuickPick([newDibNotebookText, newIpynbNotebookText]);
            switch (selected) {
                case newDibNotebookText:
                    await vscode.commands.executeCommand('dotnet-interactive.newNotebookDib');
                    break;
                case newIpynbNotebookText:
                    await vscode.commands.executeCommand('dotnet-interactive.newNotebookIpynb');
                    break;
                default:
                    break;
            }
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebookDib', async () => {
        await newNotebook('.dib');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebookIpynb', async () => {
        // note, new .ipynb notebooks are currently affected by this bug: https://github.com/microsoft/vscode/issues/121974
        await newNotebook('.ipynb');
        await selectDotNetInteractiveKernelForJupyter();
    }));

    async function newNotebook(extension: string): Promise<void> {
        const viewType = extension === '.dib' || extension === '.dotnet-interactive'
            ? 'dotnet-interactive'
            : jupyterViewType;

        // get language
        const newNotebookCSharp = `C#`;
        const newNotebookFSharp = `F#`;
        const newNotebookPowerShell = `PowerShell`;
        const notebookLanguage = await vscode.window.showQuickPick([newNotebookCSharp, newNotebookFSharp, newNotebookPowerShell], { title: 'Default Language' });
        if (!notebookLanguage) {
            return;
        }

        const ipynbLanguageName = ipynbUtilities.mapIpynbLanguageName(notebookLanguage);
        const cellMetadata = {
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: ipynbLanguageName
                    }
                }
            }
        };
        const cell = new vscode.NotebookCellData(vscode.NotebookCellKind.Code, '', `dotnet-interactive.${ipynbLanguageName}`);
        cell.metadata = cellMetadata;
        const documentMetadata = {
            custom: {
                metadata: {
                    kernelspec: {
                        display_name: `.NET (${notebookLanguage})`,
                        language: notebookLanguage,
                        name: `.net-${ipynbLanguageName}`
                    },
                    language_info: {
                        name: notebookLanguage
                    }
                }
            }
        };
        const content = new vscode.NotebookData([cell]);
        content.metadata = documentMetadata;
        const notebook = await vscode.workspace.openNotebookDocument(viewType, content);

        // The document metadata isn't preserved from the previous call.  This is addressed in the following issues:
        // - https://github.com/microsoft/vscode-jupyter/issues/6187
        // - https://github.com/microsoft/vscode-jupyter/issues/5622
        // In the meantime, the metadata can be set again to ensure it's persisted.
        const _succeeded = await versionSpecificFunctions.replaceNotebookMetadata(notebook.uri, documentMetadata);
        const _editor = await vscode.window.showNotebookDocument(notebook);
    }

    if (!isAzureDataStudio(context)) {
        context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.openNotebook', async (notebookUri: vscode.Uri | undefined) => {
            // ensure we have a notebook uri
            if (!notebookUri) {
                const uris = await vscode.window.showOpenDialog({
                    filters: notebookFileFilters
                });

                if (uris && uris.length > 0) {
                    notebookUri = uris[0];
                }

                if (!notebookUri) {
                    // no appropriate uri
                    return;
                }
            }

            await openNotebook(notebookUri);
        }));
    }

    async function openNotebook(uri: vscode.Uri): Promise<void> {
        const extension = path.extname(uri.toString());
        const viewType = extension === '.dib' || extension === '.dotnet-interactive'
            ? 'dotnet-interactive'
            : jupyterViewType;
        await vscode.commands.executeCommand('vscode.openWith', uri, viewType);
    }

    if (!isAzureDataStudio(context)) {
        context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.saveAsNotebook', async () => {
            if (vscode.window.activeNotebookEditor) {
                const uri = await vscode.window.showSaveDialog({
                    filters: notebookFileFilters
                });

                if (!uri) {
                    return;
                }

                const notebook = versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
                const interactiveDocument = toNotebookDocument(notebook);
                const uriPath = uri.toString();
                const extension = path.extname(uriPath);
                const documentType = extensionToDocumentType(extension);
                const buffer = await parserServer.serializeNotebook(documentType, eol, interactiveDocument);
                await vscode.workspace.fs.writeFile(uri, buffer);
                switch (path.extname(uriPath)) {
                    case '.dib':
                    case '.dotnet-interactive':
                        await vscode.commands.executeCommand('dotnet-interactive.openNotebook', uri);
                        break;
                }
            }
        }));
    }

    if (!isAzureDataStudio(context)) {
        context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.createNewInteractive', async () => {
            const interactiveOpenArgs = [
                {}, // showOptions
                undefined, // resource uri
                `${context.extension.id}/dotnet-interactive-window`, // controllerId
                '.NET Interactive', // title
            ];
            const result = <any>(await vscode.commands.executeCommand('interactive.open', ...interactiveOpenArgs));
            if (result && result.notebookUri && typeof result.notebookUri.toString === 'function') {
                // this looks suspiciously like a uri, let's pre-load the backing process
                clientMapper.getOrAddClient(result.notebookUri.toString());
            }
        }));
    }
}

export async function selectDotNetInteractiveKernelForJupyter(): Promise<void> {
    const extension = 'ms-dotnettools.dotnet-interactive-vscode';
    const id = KernelIdForJupyter;
    await vscode.commands.executeCommand('notebook.selectKernel', { extension, id });
}

// callbacks used to install interactive tool

async function getInteractiveVersion(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
    const result = await executeSafe(dotnetPath, ['tool', 'run', 'dotnet-interactive', '--', '--version'], globalStoragePath);
    if (result.code === 0) {
        const versionString = getVersionNumber(result.output);
        return versionString;
    }

    return undefined;
}
