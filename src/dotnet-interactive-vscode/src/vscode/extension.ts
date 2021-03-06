// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { ClientMapper } from '../clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './notebookContentProvider';
import { StdioKernelTransport } from '../stdioKernelTransport';
import { registerLanguageProviders } from './languageProvider';
import { registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './commands';

import { getSimpleLanguage, isDotnetInteractiveLanguage } from '../interactiveNotebook';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from '../interfaces';

import { DotNetCellMetadata, withDotNetMetadata } from '../ipynbUtilities';
import { executeSafe, isDotNetUpToDate, processArguments } from '../utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';
import { KernelId, updateCellLanguages, updateDocumentKernelspecMetadata } from './notebookKernel';
import { DotNetInteractiveNotebookKernelProvider } from './notebookKernelProvider';

import * as jupyter from './jupyter';

import { isInsidersBuild } from './vscodeUtilities';

export class CachedDotNetPathManager {
    private dotNetPath: string = 'dotnet'; // default to global tool if possible
    private outputChannelAdapter: OutputChannelAdapter | undefined = undefined;

    getDotNetPath(): string {
        return this.dotNetPath;
    }

    setDotNetPath(dotNetPath: string) {
        if (this.dotNetPath !== dotNetPath) {
            this.dotNetPath = dotNetPath;
            if (this.outputChannelAdapter) {
                this.outputChannelAdapter.appendLine(`dotnet path set to '${this.dotNetPath}'`);
            }
        }
    }

    setOutputChannelAdapter(outputChannelAdapter: OutputChannelAdapter) {
        this.outputChannelAdapter = outputChannelAdapter;
    }
}

export const DotNetPathManager = new CachedDotNetPathManager();

export async function activate(context: vscode.ExtensionContext) {
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));
    DotNetPathManager.setOutputChannelAdapter(diagnosticsChannel);

    // this must happen early, because some following functions use the acquisition command
    registerAcquisitionCommands(context, diagnosticsChannel);

    // register with VS Code
    const clientMapper = new ClientMapper(async (notebookPath) => {
        const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion');
        if (!await checkForDotNetSdk(minDotNetSdkVersion!)) {
            const message = 'Unable to find appropriate .NET SDK.';
            vscode.window.showErrorMessage(message);
            throw new Error(message);
        }

        const launchOptions = await getInteractiveLaunchOptions();
        if (!launchOptions) {
            throw new Error(`Unable to get interactive launch options.  Please see the '${diagnosticsChannel.getName()}' output window for details.`);
        }

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

        const processStart = processArguments(argsTemplate, notebookPath, fallbackWorkingDirectory, DotNetPathManager.getDotNetPath(), launchOptions!.workingDirectory);
        let notification = {
            displayError: async (message: string) => { await vscode.window.showErrorMessage(message, { modal: false }); },
            displayInfo: async (message: string) => { await vscode.window.showInformationMessage(message, { modal: false }); },
        };
        const transport = new StdioKernelTransport(notebookPath, processStart, diagnosticsChannel, vscode.Uri.parse, notification, (pid, code, signal) => {
            clientMapper.closeClient({ fsPath: notebookPath }, false);
        });
        await transport.waitForReady();

        let externalUri = await vscode.env.asExternalUri(vscode.Uri.parse(`http://localhost:${transport.httpPort}`));
        //create tunnel for teh kernel transport
        await transport.setExternalUri(externalUri);

        return transport;
    }, diagnosticsChannel);

    registerKernelCommands(context, clientMapper);

    const hostVersionSuffix = isInsidersBuild() ? 'Insiders' : 'Stable';
    diagnosticsChannel.appendLine(`Extension started for VS Code ${hostVersionSuffix}.`);

    const jupyterExtension = vscode.extensions.getExtension('ms-toolsai.jupyter');

    // Default to using the Jupyter extension for .ipynb handling if the extension is present, unless the user has
    // specified otherwise.
    const jupyterExtensionIsPresent = jupyterExtension !== undefined;
    let useJupyterExtension = jupyterExtensionIsPresent;
    const forceDotNetIpynbHandling = config.get<boolean>('useDotNetInteractiveExtensionForIpynbFiles') || false;
    if (forceDotNetIpynbHandling) {
        useJupyterExtension = false;
    }

    let jupyterApi: jupyter.IJupyterExtensionApi | undefined = undefined;
    if (useJupyterExtension) {
        try {
            jupyterApi = <jupyter.IJupyterExtensionApi>await jupyterExtension!.activate();
            jupyterApi.registerNewNotebookContent({ defaultCellLanguage: 'dotnet-interactive.csharp' });
        } catch (err) {
            diagnosticsChannel.appendLine(`Error activating and registering with Jupyter extension: ${err}.  Defaulting to local file handling.`);
            useJupyterExtension = false;
            jupyterApi = undefined;
        }
    }

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
    const notebookContentProvider = new DotNetInteractiveNotebookContentProvider(diagnosticsChannel, clientMapper);

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

    registerFileCommands(context, clientMapper, jupyterApi);

    context.subscriptions.push(vscode.notebook.onDidChangeActiveNotebookKernel(async e => await updateDocumentMetadata(e, clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(vscode.workspace.onDidRenameFiles(e => handleFileRenames(e, clientMapper)));

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
        try {
            // update various metadata
            await updateDocumentKernelspecMetadata(e.document);
            await updateCellLanguages(e.document);

            // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
            await clientMapper.getOrAddClient(e.document.uri);
        } catch (err) {
            vscode.window.showErrorMessage(`Failed to set document metadata for '${e.document.uri}': ${err}`);
        }
    }
}

function handleFileRenames(e: vscode.FileRenameEvent, clientMapper: ClientMapper) {
    for (const fileRename of e.files) {
        clientMapper.reassociateClient(fileRename.oldUri, fileRename.newUri);
    }
}

async function getInteractiveLaunchOptions(): Promise<InteractiveLaunchOptions | undefined> {
    // use dotnet-interactive or install
    const installArgs: InstallInteractiveArgs = {
        dotnetPath: DotNetPathManager.getDotNetPath(),
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);
    return launchOptions;
}

async function checkForDotNetSdk(minVersion: string): Promise<boolean> {
    const result = await executeSafe(DotNetPathManager.getDotNetPath(), ['--version']);
    const checkResult = isDotNetUpToDate(minVersion, result);
    return checkResult;
}
