// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../interfaces/contracts';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import * as vscodeLike from '../interfaces/vscode-like';
import { ClientMapper } from '../clientMapper';

import { StdioKernelTransport } from '../stdioKernelTransport';
import { registerLanguageProviders } from './languageProvider';
import { registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './commands';

import { getSimpleLanguage, isDotnetInteractiveLanguage, isJupyterNotebookViewType } from '../interactiveNotebook';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from '../interfaces';

import { executeSafe, getWorkingDirectoryForNotebook, isDotNetUpToDate, processArguments } from '../utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';

import * as versionSpecificFunctions from '../../versionSpecificFunctions';

import { isInsidersBuild, isStableBuild } from './vscodeUtilities';
import { getDotNetMetadata } from '../ipynbUtilities';

export const KernelIdForJupyter = 'dotnet-interactive-for-jupyter';

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
    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion') || '5.0';
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));
    DotNetPathManager.setOutputChannelAdapter(diagnosticsChannel);

    // pause if an sdk installation is currently running
    await waitForSdkInstall(minDotNetSdkVersion);

    // this must happen early, because some following functions use the acquisition command
    registerAcquisitionCommands(context, diagnosticsChannel);

    vscode.window.registerUriHandler({
        handleUri(uri: vscode.Uri): vscode.ProviderResult<void> {
            const params = new URLSearchParams(uri.query);
            switch (uri.path) {
                case '/newNotebook':
                    // Examples:
                    //   vscode://ms-dotnettools.dotnet-interactive-vscode/newNotebook?as=dib
                    //   vscode://ms-dotnettools.dotnet-interactive-vscode/newNotebook?as=ipynb
                    const asType = params.get('as');
                    vscode.commands.executeCommand('dotnet-interactive.acquire').then(() => {
                        const commandName = asType === 'ipynb'
                            ? 'dotnet-interactive.newNotebookIpynb'
                            : 'dotnet-interactive.newNotebookDib';
                        vscode.commands.executeCommand(commandName).then(() => { });
                    });
                    break;
                case '/openNotebook':
                    // Example
                    //   vscode://ms-dotnettools.dotnet-interactive-vscode/openNotebook?path=C%3A%5Cpath%5Cto%5Cnotebook.dib
                    const path = params.get('path');
                    if (path) {
                        vscode.commands.executeCommand('dotnet-interactive.acquire').then(() => {
                            vscode.commands.executeCommand('dotnet-interactive.openNotebook', vscode.Uri.file(path)).then(() => { });
                        });
                    }
                    break;
            }
        }
    });

    async function kernelTransportCreator(notebookUri: vscodeLike.Uri): Promise<contracts.KernelTransport> {
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

        // try to use $HOME/Downloads as a fallback for remote notebooks, but use the home directory if all else fails
        const homeDir = os.homedir();
        const downloadsDir = path.join(homeDir, 'Downloads');
        const fallbackWorkingDirectory = fs.existsSync(downloadsDir) ? downloadsDir : homeDir;

        const workspaceFolderUris = vscode.workspace.workspaceFolders?.map(folder => folder.uri) || [];
        const workingDirectory = getWorkingDirectoryForNotebook(notebookUri, workspaceFolderUris, fallbackWorkingDirectory);
        const processStart = processArguments(argsTemplate, workingDirectory, DotNetPathManager.getDotNetPath(), launchOptions!.workingDirectory);
        let notification = {
            displayError: async (message: string) => { await vscode.window.showErrorMessage(message, { modal: false }); },
            displayInfo: async (message: string) => { await vscode.window.showInformationMessage(message, { modal: false }); },
        };
        const transport = new StdioKernelTransport(notebookUri.toString(), processStart, diagnosticsChannel, vscode.Uri.parse, notification, (pid, code, signal) => {
            clientMapper.closeClient(notebookUri, false);
        });

        await transport.waitForReady();
        let externalUri = vscode.Uri.parse(`http://127.0.0.1:${transport.httpPort}`);
        //let externalUri = vscode.Uri.parse(`http://localhost:${transport.httpPort}`);
        try {

            await transport.setExternalUri(externalUri);
        }
        catch (e) {
            vscode.window.showErrorMessage(`Error configuring http connection with .NET Interactive on ${externalUri.toString()} : ${e.message}`);
        }

        return transport;
    }

    // register with VS Code
    const clientMapperConfig = {
        kernelTransportCreator,
        createErrorOutput: versionSpecificFunctions.createErrorOutput,
        diagnosticsChannel,
    };
    const clientMapper = new ClientMapper(clientMapperConfig);

    registerKernelCommands(context, clientMapper);

    const hostVersionSuffix = isInsidersBuild() ? 'Insiders' : 'Stable';
    diagnosticsChannel.appendLine(`Extension started for VS Code ${hostVersionSuffix}.`);
    const languageServiceDelay = config.get<number>('languageServiceDelay') || 500; // fall back to something reasonable

    // notebook kernels
    const apiBootstrapperUri = vscode.Uri.file(path.join(context.extensionPath, 'resources', 'kernelHttpApiBootstrapper.js'));
    if (!fs.existsSync(apiBootstrapperUri.fsPath)) {
        throw new Error(`Unable to find bootstrapper API expected at '${apiBootstrapperUri.fsPath}'.`);
    }

    versionSpecificFunctions.registerWithVsCode(context, clientMapper, diagnosticsChannel, clientMapperConfig.createErrorOutput, apiBootstrapperUri);

    registerFileCommands(context, clientMapper);

    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(vscode.workspace.onDidRenameFiles(e => handleFileRenames(e, clientMapper)));

    // language registration
    context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(async e => await updateNotebookCellLanguageInMetadata(e)));
    context.subscriptions.push(registerLanguageProviders(clientMapper, languageServiceDelay));
}

export function deactivate() {
}

interface DotnetPackExtensionExports {
    getDotnetPath(version?: string): Promise<string | undefined>;
}

async function waitForSdkInstall(requiredSdkVersion: string): Promise<void> {
    const sdkExtension = vscode.extensions.getExtension<DotnetPackExtensionExports | undefined>("ms-dotnettools.vscode-dotnet-pack");
    if (sdkExtension) {
        const sdkExports = sdkExtension.isActive
            ? sdkExtension.exports
            : await sdkExtension.activate();
        if (sdkExports) {
            const dotnetPath = await sdkExports.getDotnetPath(requiredSdkVersion);
            if (dotnetPath) {
                DotNetPathManager.setDotNetPath(dotnetPath);
            }
        }
    }
}

// keep the cell's language in metadata in sync with what VS Code thinks it is
async function updateNotebookCellLanguageInMetadata(candidateNotebookCellDocument: vscode.TextDocument) {
    const notebook = candidateNotebookCellDocument.notebook;
    if (notebook &&
        isJupyterNotebookViewType(notebook.viewType) &&
        isDotnetInteractiveLanguage(candidateNotebookCellDocument.languageId)) {
        const cell = versionSpecificFunctions.getCells(notebook).find(c => c.document === candidateNotebookCellDocument);
        if (cell) {
            const cellLanguage = cell.kind === vscode.NotebookCellKind.Code
                ? getSimpleLanguage(candidateNotebookCellDocument.languageId)
                : 'markdown';

            const dotnetMetadata = getDotNetMetadata(cell.metadata);
            if (dotnetMetadata.language !== cellLanguage) {
                const newMetadata = cell.metadata.with({
                    custom: {
                        metadata: {
                            dotnet_interactive: {
                                language: cellLanguage
                            }
                        }
                    }
                });
                const edit = new vscode.WorkspaceEdit();
                edit.replaceNotebookCellMetadata(notebook.uri, cell.index, newMetadata);
                await vscode.workspace.applyEdit(edit);
            }
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
