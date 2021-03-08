// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { acquireDotnetInteractive } from '../acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from '../interfaces';
import { ClientMapper } from '../clientMapper';
import { getEol, isUnsavedNotebook } from './vscodeUtilities';
import { toNotebookDocument } from './notebookContentProvider';
import { KernelId, updateCellMetadata } from './notebookKernel';
import { DotNetPathManager } from './extension';
import { computeToolInstallArguments, executeSafe, executeSafeAndLog } from '../utilities';

import * as jupyter from './jupyter';
import { ReportChannel } from 'dotnet-interactive-vscode-interfaces/out/notebook';

export function registerAcquisitionCommands(context: vscode.ExtensionContext, diagnosticChannel: ReportChannel) {
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const minDotNetInteractiveVersion = config.get<string>('minimumInteractiveToolVersion');
    const interactiveToolSource = config.get<string>('interactiveToolSource');

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.acquire', async (args?: InstallInteractiveArgs | string | undefined): Promise<InteractiveLaunchOptions | undefined> => {
        try {
            const installArgs = computeToolInstallArguments(args);
            DotNetPathManager.setDotNetPath(installArgs.dotnetPath);

            const launchOptions = await acquireDotnetInteractive(
                installArgs,
                minDotNetInteractiveVersion!,
                context.globalStorageUri.fsPath,
                getInteractiveVersion,
                createToolManifest,
                async (version: string) => { await vscode.window.showInformationMessage(`Installing .NET Interactive version ${version}...`); },
                installInteractiveTool,
                async () => { await vscode.window.showInformationMessage('.NET Interactive installation complete.'); });
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
            const d = document;
            document.cells.forEach(async cell => {
                await updateCellMetadata(d, cell, { runState: vscode.NotebookCellRunState.Idle });
            });

            clientMapper.closeClient(document.uri);
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopAllNotebookKernels', async () => {
        vscode.notebook.notebookDocuments
            .filter(document => clientMapper.isDotNetClient(document.uri))
            .forEach(async document => await vscode.commands.executeCommand('dotnet-interactive.stopCurrentNotebookKernel', document));
    }));
}

export function registerFileCommands(context: vscode.ExtensionContext, clientMapper: ClientMapper, jupyterApi: jupyter.IJupyterExtensionApi | undefined) {

    const eol = getEol();

    const fileFormatFilters = {
        'Jupyter Notebooks': ['ipynb'],
        '.NET Interactive Notebooks': ['dib', 'dotnet-interactive']
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

    function getNewNotebookName(): string {
        let suffix = 1;
        while (workspaceHasUnsavedNotebookWithName(`Untitled-${suffix}.ipynb`)) {
            suffix++;
        }

        return `Untitled-${suffix}.ipynb`;
    }

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebook', async () => {
        if (jupyterApi) {
            jupyterApi.createBlankNotebook({ defaultCellLanguage: 'dotnet-interactive.csharp' });
        } else {
            const fileName = getNewNotebookName();
            const newUri = vscode.Uri.file(fileName).with({ scheme: 'untitled', path: fileName });
            await vscode.commands.executeCommand('vscode.openWith', newUri, 'dotnet-interactive-jupyter');
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.openNotebook', async (notebookUri: vscode.Uri | undefined) => {
        // ensure we have a notebook uri
        if (!notebookUri) {
            const uris = await vscode.window.showOpenDialog({
                filters: fileFormatFilters
            });

            if (uris && uris.length > 0) {
                notebookUri = uris[0];
            }

            if (!notebookUri) {
                // no appropriate uri
                return;
            }
        }

        if (jupyterApi) {
            await vscode.commands.executeCommand('vscode.openWith', notebookUri, 'jupyter-notebook');
            await switchToInteractiveKernel();
        } else {
            await vscode.commands.executeCommand('vscode.openWith', notebookUri, 'dotnet-interactive-jupyter');
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.saveAsNotebook', async () => {
        if (vscode.window.activeNotebookEditor) {
            const uri = await vscode.window.showSaveDialog({
                filters: fileFormatFilters
            });

            if (!uri) {
                return;
            }

            const { document } = vscode.window.activeNotebookEditor;
            const notebook = toNotebookDocument(document);
            const client = await clientMapper.getOrAddClient(uri);
            const buffer = await client.serializeNotebook(uri.fsPath, notebook, eol);
            await vscode.workspace.fs.writeFile(uri, buffer);
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
