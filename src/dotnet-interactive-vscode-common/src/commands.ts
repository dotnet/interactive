// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { acquireDotnetInteractive } from './acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from './interfaces';
import { ClientMapper } from './clientMapper';
import { getEol, toNotebookDocument } from './vscodeUtilities';
import { DotNetPathManager } from './extension';
import { computeToolInstallArguments, executeSafe, executeSafeAndLog, extensionToDocumentType, getVersionNumber } from './utilities';

import * as notebookControllers from '../notebookControllers';
import * as metadataUtilities from './metadataUtilities';
import { ReportChannel } from './interfaces/vscode-like';
import { NotebookParserServer } from './notebookParserServer';
import * as versionSpecificFunctions from '../versionSpecificFunctions';
import { PromiseCompletionSource } from './dotnet-interactive/promiseCompletionSource';

import * as constants from './constants';

export function registerAcquisitionCommands(context: vscode.ExtensionContext, diagnosticChannel: ReportChannel) {
    const dotnetConfig = vscode.workspace.getConfiguration(constants.DotnetConfigurationSectionName);
    const minDotNetInteractiveVersion = dotnetConfig.get<string>('minimumInteractiveToolVersion');
    const interactiveToolSource = dotnetConfig.get<string>('interactiveToolSource');

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

function getCurrentNotebookDocument(): vscode.NotebookDocument | undefined {
    if (!vscode.window.activeNotebookEditor) {
        return undefined;
    }

    return versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
}

export function registerLegacyKernelCommands(context: vscode.ExtensionContext, clientMapper: ClientMapper) {

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.restartCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Restart the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Restart the current notebook's kernel' command instead.`);
        await await vscode.commands.executeCommand('polyglot-notebook.restartCurrentNotebookKernel', notebook);
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Stop the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Stop the current notebook's kernel' command instead.`);
        await await vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', notebook);
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopAllNotebookKernels', async () => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Stop the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Stop the current notebook's kernel' command instead.`);
        await await vscode.commands.executeCommand('polyglot-notebook.stopAllNotebookKernels');
    }));
}

export function registerKernelCommands(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
    // TODO: remove this
    registerLegacyKernelCommands(context, clientMapper);

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.notebookEditor.restartKernel', async (_notebookEditor) => {
        await vscode.commands.executeCommand('polyglot-notebook.restartCurrentNotebookKernel');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.notebookEditor.openValueViewer', async () => {
        // vscode creates a command named `<viewId>.focus` for all contributed views, so we need to match the id
        await vscode.commands.executeCommand('polyglot-notebook-panel-values.focus');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.restartCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        notebook = notebook || getCurrentNotebookDocument();
        if (notebook) {
            // notifty the client that the kernel is about to restart
            const restartCompletionSource = new PromiseCompletionSource<void>();
            vscode.window.withProgress({
                location: vscode.ProgressLocation.Notification,
                title: 'Restarting kernel...'
            },
                (_progress, _token) => restartCompletionSource.promise);
            await vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', notebook);
            await vscode.commands.executeCommand('polyglot-notebook.resetNotebookKernelCollection', notebook);
            const _client = await clientMapper.getOrAddClient(notebook.uri);
            restartCompletionSource.resolve();
            await vscode.commands.executeCommand('workbench.notebook.layout.webview.reset', notebook.uri);
            vscode.window.showInformationMessage('Kernel restarted.');
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.stopCurrentNotebookKernel', async (notebook?: vscode.NotebookDocument | undefined) => {
        notebook = notebook || getCurrentNotebookDocument();
        if (notebook) {
            for (const cell of notebook.getCells()) {
                notebookControllers.endExecution(undefined, cell, false);
            }

            const client = await clientMapper.tryGetClient(notebook.uri);
            if (client) {
                client.resetExecutionCount();
            }

            clientMapper.closeClient(notebook.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.stopAllNotebookKernels', async () => {
        vscode.workspace.notebookDocuments
            .filter(document => clientMapper.isDotNetClient(document.uri))
            .forEach(async document => await vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', document));
    }));
}

function registerLegacyFileCommands(context: vscode.ExtensionContext, parserServer: NotebookParserServer, clientMapper: ClientMapper) {

    const eol = getEol();

    const notebookFileFilters = {
        'Polyglot Notebooks': ['dib', 'dotnet-interactive'],
        'Jupyter Notebooks': ['ipynb'],
    };

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebook', async () => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Create new blank notebook' is deprecated.  Please use the 'Polyglot Notebook: Create new blank notebook' command instead.`);
        await vscode.commands.executeCommand('polyglot-notebook.newNotebook');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.openNotebook', async (notebookUri: vscode.Uri | undefined) => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Open notebook' is deprecated.  Please use the 'Polyglot Notebook: Open notebook' command instead.`);
        await vscode.commands.executeCommand('polyglot-notebook.openNotebook', notebookUri);
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.saveAsNotebook', async () => {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Save notebook as...' is deprecated.  Please use the 'Polyglot Notebook: Save notebook as...' command instead.`);
        await vscode.commands.executeCommand('polyglot-notebook.saveAsNotebook');
    }));
}

export function registerFileCommands(context: vscode.ExtensionContext, parserServer: NotebookParserServer, clientMapper: ClientMapper) {

    // todo: delete this later
    registerLegacyFileCommands(context, parserServer, clientMapper);

    const eol = getEol();

    const notebookFileFilters = {
        'Polyglot Notebooks': ['dib'],
        'Jupyter Notebooks': ['ipynb'],
    };

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebook', async () => {
        // offer to create either `.dib` or `.ipynb`
        const newDibNotebookText = `Create as '.dib'`;
        const newIpynbNotebookText = `Create as '.ipynb'`;
        const selected = await vscode.window.showQuickPick([newDibNotebookText, newIpynbNotebookText]);
        switch (selected) {
            case newDibNotebookText:
                await vscode.commands.executeCommand('polyglot-notebook.newNotebookDib');
                break;
            case newIpynbNotebookText:
                await vscode.commands.executeCommand('polyglot-notebook.newNotebookIpynb');
                break;
            default:
                break;
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebookDib', async () => {
        await newNotebook('.dib');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebookIpynb', async () => {
        // note, new .ipynb notebooks are currently affected by this bug: https://github.com/microsoft/vscode/issues/121974
        await newNotebook('.ipynb');
        await selectDotNetInteractiveKernelForJupyter();
    }));

    async function newNotebook(extension: string): Promise<void> {
        const viewType = extension === '.dib'
            ? constants.NotebookViewType
            : constants.JupyterViewType;

        // get language
        const languagesAndKernelNames: { [key: string]: string } = {
            'C#': 'csharp',
            'F#': 'fsharp',
            'HTML': 'html',
            'JavaScript': 'javascript',
            'KQL': 'kql',
            'Markdown': 'markdown',
            'Mermaid': 'mermaid',
            'PowerShell': 'pwsh',
            'SQL': 'sql',
        };

        const newLanguageOptions: string[] = [];
        for (const languageName in languagesAndKernelNames) {
            newLanguageOptions.push(languageName);
        }

        const notebookLanguage = await vscode.window.showQuickPick(newLanguageOptions, { title: 'Default Language' });
        if (!notebookLanguage) {
            return;
        }

        const kernelName = languagesAndKernelNames[notebookLanguage];
        const isMarkdown = kernelName.toLowerCase() === 'markdown';

        // the metadata needs an actual kernel name, not the special-cased 'markdown'
        const kernelNameInMetadata = isMarkdown ? 'csharp' : kernelName;
        const notebookCellMetadata: metadataUtilities.NotebookCellMetadata = {
            kernelName: kernelNameInMetadata,
        };
        const rawCellMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
        const [cellKind, cellLanguage] = isMarkdown ? [vscode.NotebookCellKind.Markup, 'markdown'] : [vscode.NotebookCellKind.Code, constants.CellLanguageIdentifier];
        const cell = new vscode.NotebookCellData(cellKind, '', cellLanguage);
        cell.metadata = rawCellMetadata;
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: kernelNameInMetadata,
                items: [
                    {
                        name: kernelNameInMetadata,
                        aliases: [],
                        languageName: kernelNameInMetadata // it just happens that the kernel names we allow are also the language names
                    }
                ]
            }
        };

        const createForIpynb = viewType === constants.JupyterViewType;
        const rawNotebookMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, createForIpynb);
        const content = new vscode.NotebookData([cell]);
        content.metadata = rawNotebookMetadata;
        const notebook = await vscode.workspace.openNotebookDocument(viewType, content);
        const _editor = await vscode.window.showNotebookDocument(notebook);
    }

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.openNotebook', async (notebookUri: vscode.Uri | undefined) => {
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

    async function openNotebook(uri: vscode.Uri): Promise<void> {
        const extension = path.extname(uri.toString());
        const viewType = extension === '.dib'
            ? constants.NotebookViewType
            : constants.JupyterViewType;
        await vscode.commands.executeCommand('vscode.openWith', uri, viewType);
    }

    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.saveAsNotebook', async () => {
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
                    await vscode.commands.executeCommand('polyglot-notebook.openNotebook', uri);
                    break;
            }
        }
    }));
}

export async function selectDotNetInteractiveKernelForJupyter(): Promise<void> {
    const extension = 'ms-dotnettools.dotnet-interactive-vscode';
    const id = constants.JupyterKernelId;
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
