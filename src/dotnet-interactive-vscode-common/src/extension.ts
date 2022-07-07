// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './dotnet-interactive/contracts';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { MessageClient } from './messageClient';

import { StdioDotnetInteractiveChannel } from './stdioDotnetInteractiveChannel';
import { registerLanguageProviders } from './languageProvider';
import { registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './commands';

import { getNotebookSpecificLanguage, getSimpleLanguage, isDotnetInteractiveLanguage, isJupyterNotebookViewType, languageToCellKind } from './interactiveNotebook';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from './interfaces';

import { createOutput, getDotNetVersionOrThrow, getWorkingDirectoryForNotebook, isVersionSufficient, processArguments } from './utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';

import * as notebookControllers from '../notebookControllers';
import * as notebookSerializers from '../notebookSerializers';
import * as versionSpecificFunctions from '../versionSpecificFunctions';
import { ErrorOutputCreator } from './interactiveClient';

import { isInsidersBuild } from './vscodeUtilities';
import { getDotNetMetadata, withDotNetCellMetadata } from './ipynbUtilities';
import fetch from 'node-fetch';
import { CompositeKernel } from './dotnet-interactive/compositeKernel';
import { Logger, LogLevel } from './dotnet-interactive/logger';
import { ChildProcessLineAdapter } from './childProcessLineAdapter';
import { NotebookParserServer } from './notebookParserServer';
import { registerVariableExplorer } from './variableExplorer';

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
    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion') || '6.0';
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));
    const loggerChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : logger'));
    DotNetPathManager.setOutputChannelAdapter(diagnosticsChannel);

    Logger.configure('extension host', logEntry => {
        const config = vscode.workspace.getConfiguration('dotnet-interactive');
        const loggerLevelString = config.get<string>('logLevel') || LogLevel[LogLevel.Error];
        const loggerLevelKey = loggerLevelString as keyof typeof LogLevel;
        const logLevel = LogLevel[loggerLevelKey];
        if (logEntry.logLevel >= logLevel) {
            const messageLogLevel = LogLevel[logEntry.logLevel];
            loggerChannel.appendLine(`[${messageLogLevel}] ${logEntry.source}: ${logEntry.message}`);
        }
    });

    await waitForSdkPackExtension();

    // this must happen early, because some following functions use the acquisition command
    registerAcquisitionCommands(context, diagnosticsChannel);

    // check sdk version
    try {
        const dotnetVersion = await getDotNetVersionOrThrow(DotNetPathManager.getDotNetPath(), diagnosticsChannel);
        if (!isVersionSufficient(dotnetVersion, minDotNetSdkVersion)) {
            const message = `The .NET SDK version ${dotnetVersion} is not sufficient. The minimum required version is ${minDotNetSdkVersion}.`;
            diagnosticsChannel.appendLine(message);
            vscode.window.showErrorMessage(message);
            throw new Error(message);
        }
    } catch (e) {
        vscode.window.showErrorMessage(`Please install the .NET SDK version ${minDotNetSdkVersion} from https://dotnet.microsoft.com/en-us/download`);
        throw e;
    }

    async function kernelChannelCreator(notebookUri: vscodeLike.Uri): Promise<contracts.KernelCommandAndEventChannel> {
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
        const channel = new StdioDotnetInteractiveChannel(notebookUri.toString(), processStart, diagnosticsChannel, (pid, code, signal) => {
            clientMapper.closeClient(notebookUri, false);
        });

        await channel.waitForReady();

        return channel;
    }

    function configureKernel(compositeKernel: CompositeKernel, notebookUri: vscodeLike.Uri) {
        compositeKernel.setDefaultTargetKernelNameForCommand(contracts.RequestInputType, compositeKernel.name);
        compositeKernel.setDefaultTargetKernelNameForCommand(contracts.SendEditableCodeType, compositeKernel.name);

        compositeKernel.registerCommandHandler({
            commandType: contracts.RequestInputType, handle: async (commandInvocation) => {
                const requestInput = <contracts.RequestInput>commandInvocation.commandEnvelope.command;
                const prompt = requestInput.prompt;
                const password = requestInput.isPassword;
                const value = await vscode.window.showInputBox({ prompt, password });
                if (!value) {
                    commandInvocation.context.fail('Input request cancelled');
                } else {
                    commandInvocation.context.publish({
                        eventType: contracts.InputProducedType,
                        event: {
                            value
                        },
                        command: commandInvocation.commandEnvelope,
                    });
                }
            }
        });

        compositeKernel.registerCommandHandler({
            commandType: contracts.SendEditableCodeType, handle: async commandInvocation => {
                const addCell = <contracts.SendEditableCode>commandInvocation.commandEnvelope.command;
                const language = addCell.language;
                const contents = addCell.code;
                const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.uri.toString() === notebookUri.toString());
                if (notebookDocument) {
                    const range = new vscode.NotebookRange(notebookDocument.cellCount, notebookDocument.cellCount);
                    const cellKind = languageToCellKind(language);
                    const notebookCellLanguage = getNotebookSpecificLanguage(language);
                    const newCell = new vscode.NotebookCellData(cellKind, contents, notebookCellLanguage);
                    const succeeded = versionSpecificFunctions.replaceNotebookCells(notebookDocument.uri, range, [newCell]);
                    if (!succeeded) {
                        throw new Error(`Unable to add cell to notebook '${notebookUri.toString()}'.`);
                    }
                } else {
                    throw new Error(`Unable to get notebook document for URI '${notebookUri.toString()}'.`);
                }
            }
        });
    }

    // register with VS Code
    const clientMapperConfig = {
        channelCreator: kernelChannelCreator,
        createErrorOutput,
        diagnosticsChannel,
        configureKernel,
    };
    const clientMapper = new ClientMapper(clientMapperConfig);
    registerKernelCommands(context, clientMapper);
    registerVariableExplorer(context, clientMapper);

    const hostVersionSuffix = isInsidersBuild() ? 'Insiders' : 'Stable';
    diagnosticsChannel.appendLine(`Extension started for VS Code ${hostVersionSuffix}.`);
    const languageServiceDelay = config.get<number>('languageServiceDelay') || 500; // fall back to something reasonable

    const preloads = getPreloads(context.extensionPath);

    ////////////////////////////////////////////////////////////////////////////////
    const launchOptions = await getInteractiveLaunchOptions();
    const serializerCommand = <string[]>config.get('notebookParserArgs') || []; // TODO: fallback values?
    const serializerCommandProcessStart = processArguments({ args: serializerCommand, workingDirectory: launchOptions!.workingDirectory }, '.', DotNetPathManager.getDotNetPath(), context.globalStorageUri.fsPath);
    const serializerLineAdapter = new ChildProcessLineAdapter(serializerCommandProcessStart.command, serializerCommandProcessStart.args, serializerCommandProcessStart.workingDirectory, true, diagnosticsChannel);
    const messageClient = new MessageClient(serializerLineAdapter);
    const parserServer = new NotebookParserServer(messageClient);
    // startup time consistently <300ms
    // old startup time ~4800ms
    ////////////////////////////////////////////////////////////////////////////////

    const serializerMap = registerWithVsCode(context, clientMapper, parserServer, clientMapperConfig.createErrorOutput, ...preloads);
    registerFileCommands(context, parserServer, clientMapper);

    context.subscriptions.push(vscode.workspace.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(vscode.workspace.onDidRenameFiles(e => handleFileRenames(e, clientMapper)));

    // clean up processes
    context.subscriptions.push(serializerLineAdapter);
    clientMapper.onClientCreate((_uri, client) => {
        context.subscriptions.push(client);
    });

    // language registration
    context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(async e => await updateNotebookCellLanguageInMetadata(e)));
    context.subscriptions.push(registerLanguageProviders(clientMapper, languageServiceDelay));

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
                    // Open a local notebook
                    //   vscode://ms-dotnettools.dotnet-interactive-vscode/openNotebook?path=C%3A%5Cpath%5Cto%5Cnotebook.dib
                    // New untitled notebook from remote source
                    //   vscode://ms-dotnettools.dotnet-interactive-vscode/openNotebook?url=http%3A%2F%2Fexample.com%2Fnotebook.dib
                    const notebookPath = params.get('path');
                    const url = params.get('url');
                    const notebookFormat = params.get('notebookFormat');
                    if (notebookPath) {
                        vscode.commands.executeCommand('dotnet-interactive.acquire').then(() => {
                            vscode.commands.executeCommand('dotnet-interactive.openNotebook', vscode.Uri.file(notebookPath)).then(() => { });
                        });
                    } else if (url) {
                        openNotebookFromUrl(url, notebookFormat, serializerMap, diagnosticsChannel).then(() => { });
                    }
                    break;
            }
        }
    });
}

export function deactivate() {
}

function getPreloads(extensionPath: string): vscode.Uri[] {
    const preloads: vscode.Uri[] = [];
    const errors: string[] = [];
    const apiFiles: string[] = [
        'kernelApiBootstrapper.js'
    ];

    for (const apiFile of apiFiles) {
        const apiFileUri = vscode.Uri.file(path.join(extensionPath, 'resources', apiFile));
        if (fs.existsSync(apiFileUri.fsPath)) {
            preloads.push(apiFileUri);
        } else {
            errors.push(`Unable to find API file expected at  ${apiFileUri.fsPath}`);
        }
    }

    if (errors.length > 0) {
        const error = errors.join("\n");
        throw new Error(error);
    }

    return preloads;
}

function createErrorOutput(message: string, outputId?: string): vscodeLike.NotebookCellOutput {
    const error = { name: 'Error', message };
    const errorItem = vscode.NotebookCellOutputItem.error(error);
    const cellOutput = createOutput([errorItem], outputId);
    return cellOutput;
}

function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, parserServer: NotebookParserServer, createErrorOutput: ErrorOutputCreator, ...preloadUris: vscode.Uri[]): Map<string, vscode.NotebookSerializer> {
    const config = {
        clientMapper,
        preloadUris,
        createErrorOutput,
    };
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(config));
    return notebookSerializers.createAndRegisterNotebookSerializers(context, parserServer);
}

async function openNotebookFromUrl(notebookUrl: string, notebookFormat: string | null, serializerMap: Map<string, vscode.NotebookSerializer>, diagnosticsChannel: OutputChannelAdapter): Promise<void> {
    await vscode.commands.executeCommand('dotnet-interactive.acquire');

    try {
        Logger.default.info(`Opening notebook from URL: ${notebookUrl}`);
        const response = await fetch(notebookUrl);
        if (response.redirected) {
            Logger.default.info(`Redirected to: ${response.url}`);
        }
        if (response.status >= 400) {
            const errorText = await response.text();
            Logger.default.error(`Failed to open notebook from URL: ${notebookUrl}.  ${errorText}`);
            vscode.window.showErrorMessage(`Failed to open notebook from URL: ${notebookUrl}.  ${errorText}`);
            return;
        }
        const resolvedUri = vscode.Uri.parse(response.url);
        if (response.redirected && !notebookFormat) {
            // if no notebook format was passed in _and_ we redirected, check the final resolved url for a format
            const searchParams = new URLSearchParams(resolvedUri.query);
            notebookFormat = searchParams.get('notebookFormat');
        }

        if (!notebookFormat) {
            // if no format was specified on either uri, fall back to the extension
            let extension = path.extname(resolvedUri.path);
            switch (extension) {
                case '.dib':
                case '.dotnet-interactive':
                    notebookFormat = 'dib';
                    break;
                case '.ipynb':
                    notebookFormat = 'ipynb';
                    break;
                default:
                    throw new Error(`Unsupported notebook extension '${extension}'`);
            }
        }

        let viewType: string | undefined = undefined;
        switch (notebookFormat) {
            case 'dib':
                viewType = 'dotnet-interactive';
                break;
            case 'ipynb':
                viewType = 'jupyter-notebook';
                break;
            default:
                throw new Error(`Unsupported notebook format: ${notebookFormat}`);
        }

        const serializer = serializerMap.get(viewType);
        if (!serializer) {
            throw new Error(`Unsupported notebook view type: ${viewType}`);
        }

        const arrayBuffer = await response.arrayBuffer();
        const content = new Uint8Array(arrayBuffer);
        const cancellationTokenSource = new vscode.CancellationTokenSource();
        const notebookData = await serializer.deserializeNotebook(content, cancellationTokenSource.token);
        const notebook = await vscode.workspace.openNotebookDocument(viewType, notebookData);
        const _editor = await vscode.window.showNotebookDocument(notebook);
    } catch (e) {
        vscode.window.showWarningMessage(`Unable to read notebook from '${notebookUrl}': ${e}`);
    }
}

interface DotnetPackExtensionExports {
    getDotnetPath(version?: string): Promise<string | undefined>;
}

async function waitForSdkPackExtension(): Promise<void> {
    const sdkExtension = vscode.extensions.getExtension<DotnetPackExtensionExports | undefined>("ms-dotnettools.vscode-dotnet-pack");
    if (sdkExtension && !sdkExtension.isActive) {
        await sdkExtension.activate();
    }
}

// keep the cell's language in metadata in sync with what VS Code thinks it is
async function updateNotebookCellLanguageInMetadata(candidateNotebookCellDocument: vscode.TextDocument) {
    const notebook = vscode.workspace.notebookDocuments.find(notebook => notebook.getCells().some(cell => cell.document === candidateNotebookCellDocument));
    if (notebook &&
        isJupyterNotebookViewType(notebook.notebookType) &&
        isDotnetInteractiveLanguage(candidateNotebookCellDocument.languageId)) {
        const cell = notebook.getCells().find(c => c.document === candidateNotebookCellDocument);
        if (cell) {
            const cellLanguage = cell.kind === vscode.NotebookCellKind.Code
                ? getSimpleLanguage(candidateNotebookCellDocument.languageId)
                : 'markdown';

            const dotnetMetadata = getDotNetMetadata(cell.metadata);
            if (dotnetMetadata.language !== cellLanguage) {
                const newMetadata = withDotNetCellMetadata(cell.metadata, cellLanguage);
                const _succeeded = await versionSpecificFunctions.replaceNotebookCellMetadata(notebook.uri, cell.index, newMetadata);
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
