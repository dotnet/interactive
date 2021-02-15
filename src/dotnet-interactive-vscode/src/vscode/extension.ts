// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { ClientMapper } from '../clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './notebookContentProvider';
import { StdioKernelTransport } from '../stdioKernelTransport';
import { registerLanguageProviders } from './languageProvider';
import { execute, registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './commands';

import { getSimpleLanguage, isDotnetInteractiveLanguage, notebookCellLanguages } from '../interactiveNotebook';
import { IDotnetAcquireResult } from 'vscode-interfaces/out/dotnet';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from '../interfaces';

import compareVersions = require("compare-versions");
import { DotNetCellMetadata, withDotNetMetadata } from '../ipynbUtilities';
import { processArguments } from '../utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';
import { KernelId, updateCellLanguages, updateDocumentKernelspecMetadata } from './notebookKernel';
import { DotNetInteractiveNotebookKernelProvider } from './notebookKernelProvider';

import { isInsidersBuild } from './vscodeUtilities';

export async function activate(context: vscode.ExtensionContext) {
    // this must happen first, because some following functions use the acquisition command
    registerAcquisitionCommands(context);

    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));

    // n.b., this is _not_ resolved here because it's potentially really slow, but needs to be chained off of later
    const dotnetPromise = getDotnetPath(diagnosticsChannel);

    // register with VS Code
    const clientMapper = new ClientMapper(async (notebookPath) => {
        diagnosticsChannel.appendLine(`Creating client for notebook "${notebookPath}"`);
        const dotnetPath = await dotnetPromise;
        const launchOptions = await getInteractiveLaunchOptions(dotnetPath);

        // prepare kernel transport launch arguments and working directory using a fresh config item so we don't get cached values

        const kernelTransportArgs = config.get<Array<string>>('kernelTransportArgs')!;
        const argsTemplate = {
            args: kernelTransportArgs,
            workingDirectory: config.get<string>('kernelTransportWorkingDirectory')!
        };

        // ensure a reasonable working directory is selected
        const fallbackWorkingDirectory = (vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0)
            ? vscode.workspace.workspaceFolders[0].uri.fsPath
            : '.';

        const processStart = processArguments(argsTemplate, notebookPath, fallbackWorkingDirectory, dotnetPath, launchOptions!.workingDirectory);
        let notification = {
            displayError: async (message: string) => { await vscode.window.showErrorMessage(message, { modal: false }); },
            displayInfo: async (message: string) => { await vscode.window.showInformationMessage(message, { modal: false }); },
        };
        const transport = new StdioKernelTransport(processStart, diagnosticsChannel, vscode.Uri.parse, notification);
        await transport.waitForReady();

        let externalUri = await vscode.env.asExternalUri(vscode.Uri.parse(`http://localhost:${transport.httpPort}`));
        //create tunnel for teh kernel transport
        await transport.setExternalUri(externalUri);

        return transport;
    });

    registerKernelCommands(context, clientMapper);

    const hostVersionSuffix = isInsidersBuild() ? 'Insiders' : 'Stable';
    diagnosticsChannel.appendLine(`Extension started for VS Code ${hostVersionSuffix}.`);

    const jupyterExtensionIsPresent = vscode.extensions.getExtension('ms-toolsai.jupyter') !== undefined;
    const useJupyterExtension = isInsidersBuild() && jupyterExtensionIsPresent && (config.get<boolean>('useJupyterExtensionForIpynbFiles') || false);
    registerFileCommands(context, clientMapper, useJupyterExtension);

    const diagnosticDelay = config.get<number>('liveDiagnosticDelay') || 500; // fall back to something reasonable
    const selectorDib = {
        viewType: ['dotnet-interactive'],
        filenamePattern: '*.{dib,dotnet-interactive}'
    };
    const selectorIpynbWithJupyter = {
        viewType: ['jupyter-notebook'],
        filenamePattern: '*.ipynb'
    };
    const selectorIpynbWithDotNetInteractive = {
        viewType: ['dotnet-interactive-jupyter'],
        filenamePatter: '*.ipynb'
    };
    const notebookContentProvider = new DotNetInteractiveNotebookContentProvider(clientMapper);

    // notebook content
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', notebookContentProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', notebookContentProvider));

    // notebook kernels
    const apiBootstrapperUri = vscode.Uri.file(path.join(context.extensionPath, 'resources', 'kernelHttpApiBootstrapper.js'));
    const notebookKernelProvider = new DotNetInteractiveNotebookKernelProvider(apiBootstrapperUri, clientMapper);
    context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorDib, notebookKernelProvider));
    if (useJupyterExtension) {
        context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorIpynbWithJupyter, notebookKernelProvider));
    } else {
        context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorIpynbWithDotNetInteractive, notebookKernelProvider));
    }

    context.subscriptions.push(vscode.notebook.onDidChangeActiveNotebookKernel(async e => await updateDocumentMetadata(e, clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));

    // language registration
    context.subscriptions.push(vscode.notebook.onDidChangeCellLanguage(async e => await updateCellLanguageInMetadata(e)));
    context.subscriptions.push(registerLanguageProviders(clientMapper, diagnosticDelay));
}

export function deactivate() {
}

// keep the cell's language in metadata in sync with what VS Code thinks it is
async function updateCellLanguageInMetadata(languageChangeEvent: { cell: vscode.NotebookCell, document: vscode.NotebookDocument, language: string }) {
    if (isDotnetInteractiveLanguage(languageChangeEvent.language)) {
        const cellIndex = languageChangeEvent.document.cells.findIndex(c => c === languageChangeEvent.cell);
        if (cellIndex >= 0) {
            const edit = new vscode.WorkspaceEdit();
            const cellMetadata: DotNetCellMetadata = {
                language: getSimpleLanguage(languageChangeEvent.language),
            };
            const metadata = withDotNetMetadata(languageChangeEvent.cell.metadata, cellMetadata);
            edit.replaceNotebookCellMetadata(languageChangeEvent.document.uri, cellIndex, metadata);
            await vscode.workspace.applyEdit(edit);
        }
    }
}

async function updateDocumentMetadata(e: { document: vscode.NotebookDocument, kernel: vscode.NotebookKernel | undefined }, clientMapper: ClientMapper) {
    if (e.kernel?.id === KernelId) {
        if (!isInsidersBuild()) {
            // update document language (not supported on Insiders)
            e.document.languages = notebookCellLanguages;
        }

        // update various metadata
        await updateDocumentKernelspecMetadata(e.document);
        await updateCellLanguages(e.document);

        // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
        await clientMapper.getOrAddClient(e.document.uri);
    }
}

// this function can be slow and should only be called once
async function getDotnetPath(outputChannel: OutputChannelAdapter): Promise<string> {
    // use global dotnet or install
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion');
    let dotnetPath: string;
    if (await isDotnetUpToDate(minDotNetSdkVersion!)) {
        dotnetPath = 'dotnet';
    } else {
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: minDotNetSdkVersion, requestingExtensionId: 'ms-dotnettools.dotnet-interactive-vscode' });
        dotnetPath = commandResult!.dotnetPath;
    }

    outputChannel.appendLine(`Using dotnet from "${dotnetPath}"`);
    return dotnetPath;
}

async function getInteractiveLaunchOptions(dotnetPath: string): Promise<InteractiveLaunchOptions> {
    // use dotnet-interactive or install
    const installArgs: InstallInteractiveArgs = {
        dotnetPath,
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);
    return launchOptions!;
}

async function isDotnetUpToDate(minVersion: string): Promise<boolean> {
    const result = await execute('dotnet', ['--version']);
    return result.code === 0 && compareVersions.compare(result.output, minVersion, '>=');
}
