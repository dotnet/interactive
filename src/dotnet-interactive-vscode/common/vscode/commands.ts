// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { acquireDotnetInteractive } from '../acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from '../interfaces';
import { ClientMapper } from '../clientMapper';
import { getEol, isUnsavedNotebook, toNotebookDocument } from './vscodeUtilities';
import { DotNetPathManager, KernelId } from './extension';
import { computeToolInstallArguments, executeSafe, executeSafeAndLog } from '../utilities';

import * as versionSpecificFunctions from '../../versionSpecificFunctions';
import { ReportChannel } from '../interfaces/vscode-like';

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
                acquirePromise = acquireDotnetInteractive(
                    installArgs,
                    minDotNetInteractiveVersion!,
                    context.globalStorageUri.fsPath,
                    getInteractiveVersion,
                    createToolManifest,
                    async (version: string) => { await vscode.window.showInformationMessage(`Installing .NET Interactive version ${version}...`); },
                    installInteractiveTool,
                    async () => { await vscode.window.showInformationMessage('.NET Interactive installation complete.'); });
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

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.restartCurrentNotebookKernel', async (document?: vscode.NotebookDocument | undefined) => {
        if (!document) {
            if (!vscode.window.activeNotebookEditor) {
                // no notebook to operate on
                return;
            }

            document = vscode.window.activeNotebookEditor.document;
        }

        if (document) {
            await vscode.commands.executeCommand('dotnet-interactive.stopCurrentNotebookKernel', document);
            const _client = await clientMapper.getOrAddClient(document.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopCurrentNotebookKernel', async (document?: vscode.NotebookDocument | undefined) => {
        if (!document) {
            if (!vscode.window.activeNotebookEditor) {
                // no notebook to operate on
                return;
            }

            document = vscode.window.activeNotebookEditor.document;
        }

        if (document) {
            for (const cell of versionSpecificFunctions.getCells(document)) {
                versionSpecificFunctions.endExecution(cell, false);
            }

            clientMapper.closeClient(document.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopAllNotebookKernels', async () => {
        vscode.notebook.notebookDocuments
            .filter(document => clientMapper.isDotNetClient(document.uri))
            .forEach(async document => await vscode.commands.executeCommand('dotnet-interactive.stopCurrentNotebookKernel', document));
    }));
}

export function registerFileCommands(context: vscode.ExtensionContext, clientMapper: ClientMapper) {

    const eol = getEol();

    const fileOpenFilters = {
        '.NET Interactive Notebooks': ['dib', 'dotnet-interactive'],
    };

    const fileSaveAsFilters = {
        ...fileOpenFilters,
        'Jupyter Notebooks': ['ipynb'],
    };

    function workspaceHasUnsavedNotebookWithName(fileName: string): boolean {
        return vscode.workspace.textDocuments.findIndex(textDocument => {
            if (textDocument.notebook) {
                const notebookUri = textDocument.notebook.uri;
                return isUnsavedNotebook(notebookUri) && path.basename(notebookUri.fsPath) === fileName;
            }

            return false;
        }) >= 0;
    }

    // using .ipynb as default until https://github.com/microsoft/vscode/issues/121974 is fixed
    //const [newNotebookExtension, newNotebookViewType] = ['.dib', 'dotnet-interactive'];
    const [newNotebookExtension, newNotebookViewType] = ['.ipynb', 'jupyter-notebook'];

    function getNewNotebookName(): string {
        let suffix = 1;
        let filename = '';
        do {
            filename = `Untitled-${suffix++}${newNotebookExtension}`;
        } while (workspaceHasUnsavedNotebookWithName(filename));

        return filename;
    }

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebook', async () => {
        const fileName = getNewNotebookName();
        const newUri = vscode.Uri.file(fileName).with({ scheme: 'untitled', path: fileName });
        await vscode.commands.executeCommand('vscode.openWith', newUri, newNotebookViewType);
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.openNotebook', async (notebookUri: vscode.Uri | undefined) => {
        // ensure we have a notebook uri
        if (!notebookUri) {
            const uris = await vscode.window.showOpenDialog({
                filters: fileOpenFilters
            });

            if (uris && uris.length > 0) {
                notebookUri = uris[0];
            }

            if (!notebookUri) {
                // no appropriate uri
                return;
            }
        }

        await vscode.commands.executeCommand('vscode.openWith', notebookUri, 'dotnet-interactive');
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.saveAsNotebook', async () => {
        if (vscode.window.activeNotebookEditor) {
            const uri = await vscode.window.showSaveDialog({
                filters: fileSaveAsFilters
            });

            if (!uri) {
                return;
            }

            const { document } = vscode.window.activeNotebookEditor;
            const notebook = toNotebookDocument(document);
            const client = await clientMapper.getOrAddClient(uri);
            const buffer = await client.serializeNotebook(uri.fsPath, notebook, eol);
            await vscode.workspace.fs.writeFile(uri, buffer);
            switch (path.extname(uri.fsPath)) {
                case '.dib':
                case '.dotnet-interactive':
                    await vscode.commands.executeCommand('dotnet-interactive.openNotebook', uri);
                    break;
            }
        }
    }));
}

async function switchToInteractiveKernel() {
    const extension = 'ms-dotnettools.dotnet-interactive-vscode';
    const id = KernelId;
    await vscode.commands.executeCommand('notebook.selectKernel', { extension, id });
}

// callbacks used to install interactive tool

async function getInteractiveVersion(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
    const result = await executeSafe(dotnetPath, ['tool', 'run', 'dotnet-interactive', '--', '--version'], globalStoragePath);
    if (result.code === 0) {
        return result.output;
    }

    return undefined;
}
