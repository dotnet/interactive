"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.selectDotNetInteractiveKernelForJupyter = exports.registerFileCommands = exports.registerKernelCommands = exports.registerLegacyKernelCommands = exports.registerAcquisitionCommands = void 0;
const vscode = require("vscode");
const path = require("path");
const acquisition_1 = require("./acquisition");
const vscodeUtilities_1 = require("./vscodeUtilities");
const extension_1 = require("./extension");
const utilities_1 = require("./utilities");
const notebookControllers = require("../notebookControllers");
const metadataUtilities = require("./metadataUtilities");
const versionSpecificFunctions = require("../versionSpecificFunctions");
const promiseCompletionSource_1 = require("./dotnet-interactive/promiseCompletionSource");
const constants = require("./constants");
function registerAcquisitionCommands(context, diagnosticChannel) {
    const dotnetConfig = vscode.workspace.getConfiguration(constants.DotnetConfigurationSectionName);
    const minDotNetInteractiveVersion = dotnetConfig.get('minimumInteractiveToolVersion');
    const interactiveToolSource = dotnetConfig.get('interactiveToolSource');
    let cachedInstallArgs = undefined;
    let acquirePromise = undefined;
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.acquire', (args) => __awaiter(this, void 0, void 0, function* () {
        try {
            const installArgs = (0, utilities_1.computeToolInstallArguments)(args);
            extension_1.DotNetPathManager.setDotNetPath(installArgs.dotnetPath);
            if (cachedInstallArgs) {
                if (installArgs.dotnetPath !== cachedInstallArgs.dotnetPath ||
                    installArgs.toolVersion !== cachedInstallArgs.toolVersion) {
                    // if specified install args are different than what we previously computed, invalidate the acquisition
                    acquirePromise = undefined;
                }
            }
            if (!acquirePromise) {
                const installationPromiseCompletionSource = new promiseCompletionSource_1.PromiseCompletionSource();
                acquirePromise = (0, acquisition_1.acquireDotnetInteractive)(installArgs, minDotNetInteractiveVersion, context.globalStorageUri.fsPath, getInteractiveVersion, createToolManifest, (version) => {
                    vscode.window.withProgress({ location: vscode.ProgressLocation.Notification, title: `Installing .NET Interactive version ${version}...` }, (_progress, _token) => installationPromiseCompletionSource.promise);
                }, installInteractiveTool, () => { installationPromiseCompletionSource.resolve(); });
            }
            const launchOptions = yield acquirePromise;
            return launchOptions;
        }
        catch (err) {
            diagnosticChannel.appendLine(`Error acquiring dotnet-interactive tool: ${err}`);
        }
    })));
    function createToolManifest(dotnetPath, globalStoragePath) {
        return __awaiter(this, void 0, void 0, function* () {
            const result = yield (0, utilities_1.executeSafeAndLog)(diagnosticChannel, 'create-tool-manifest', dotnetPath, ['new', 'tool-manifest'], globalStoragePath);
            if (result.code !== 0) {
                throw new Error(`Unable to create local tool manifest.  Command failed with code ${result.code}.\n\nSTDOUT:\n${result.output}\n\nSTDERR:\n${result.error}`);
            }
        });
    }
    function installInteractiveTool(args, globalStoragePath) {
        return __awaiter(this, void 0, void 0, function* () {
            // remove previous tool; swallow errors in case it's not already installed
            let uninstallArgs = [
                'tool',
                'uninstall',
                'Microsoft.dotnet-interactive'
            ];
            yield (0, utilities_1.executeSafeAndLog)(diagnosticChannel, 'tool-uninstall', args.dotnetPath, uninstallArgs, globalStoragePath);
            let toolArgs = [
                'tool',
                'install',
                '--add-source',
                interactiveToolSource,
                '--ignore-failed-sources',
                'Microsoft.dotnet-interactive'
            ];
            if (args.toolVersion) {
                toolArgs.push('--version', args.toolVersion);
            }
            return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
                const result = yield (0, utilities_1.executeSafeAndLog)(diagnosticChannel, 'tool-install', args.dotnetPath, toolArgs, globalStoragePath);
                if (result.code === 0) {
                    resolve();
                }
                else {
                    reject();
                }
            }));
        });
    }
}
exports.registerAcquisitionCommands = registerAcquisitionCommands;
function getCurrentNotebookDocument() {
    if (!vscode.window.activeNotebookEditor) {
        return undefined;
    }
    return versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
}
function registerLegacyKernelCommands(context, clientMapper) {
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.restartCurrentNotebookKernel', (notebook) => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Restart the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Restart the current notebook's kernel' command instead.`);
        yield yield vscode.commands.executeCommand('polyglot-notebook.restartCurrentNotebookKernel', notebook);
    })));
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopCurrentNotebookKernel', (notebook) => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Stop the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Stop the current notebook's kernel' command instead.`);
        yield yield vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', notebook);
    })));
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.stopAllNotebookKernels', () => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Stop the current notebook's kernel' is deprecated.  Please use the 'Polyglot Notebook: Stop the current notebook's kernel' command instead.`);
        yield yield vscode.commands.executeCommand('polyglot-notebook.stopAllNotebookKernels');
    })));
}
exports.registerLegacyKernelCommands = registerLegacyKernelCommands;
function registerKernelCommands(context, clientMapper) {
    // TODO: remove this
    registerLegacyKernelCommands(context, clientMapper);
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.notebookEditor.restartKernel', (_notebookEditor) => __awaiter(this, void 0, void 0, function* () {
        yield vscode.commands.executeCommand('polyglot-notebook.restartCurrentNotebookKernel');
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.notebookEditor.openValueViewer', () => __awaiter(this, void 0, void 0, function* () {
        // vscode creates a command named `<viewId>.focus` for all contributed views, so we need to match the id
        yield vscode.commands.executeCommand('polyglot-notebook-panel-values.focus');
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.restartCurrentNotebookKernel', (notebook) => __awaiter(this, void 0, void 0, function* () {
        notebook = notebook || getCurrentNotebookDocument();
        if (notebook) {
            // clear the value explorer view
            yield vscode.commands.executeCommand('polyglot-notebook.clearValueExplorer');
            // notifty the client that the kernel is about to restart
            const restartCompletionSource = new promiseCompletionSource_1.PromiseCompletionSource();
            vscode.window.withProgress({
                location: vscode.ProgressLocation.Notification,
                title: 'Restarting kernel...'
            }, (_progress, _token) => restartCompletionSource.promise);
            yield vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', notebook);
            yield vscode.commands.executeCommand('polyglot-notebook.resetNotebookKernelCollection', notebook);
            const _client = yield clientMapper.getOrAddClient(notebook.uri);
            restartCompletionSource.resolve();
            yield vscode.commands.executeCommand('workbench.notebook.layout.webview.reset', notebook.uri);
            vscode.window.showInformationMessage('Kernel restarted.');
            // notify the ValueExplorer that the kernel has restarted
            yield vscode.commands.executeCommand('polyglot-notebook.resetValueExplorerSubscriptions');
        }
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.stopCurrentNotebookKernel', (notebook) => __awaiter(this, void 0, void 0, function* () {
        notebook = notebook || getCurrentNotebookDocument();
        if (notebook) {
            for (const cell of notebook.getCells()) {
                notebookControllers.endExecution(undefined, cell, false);
            }
            const client = yield clientMapper.tryGetClient(notebook.uri);
            if (client) {
                client.resetExecutionCount();
            }
            clientMapper.closeClient(notebook.uri);
        }
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.stopAllNotebookKernels', () => __awaiter(this, void 0, void 0, function* () {
        vscode.workspace.notebookDocuments
            .filter(document => clientMapper.isDotNetClient(document.uri))
            .forEach((document) => __awaiter(this, void 0, void 0, function* () { return yield vscode.commands.executeCommand('polyglot-notebook.stopCurrentNotebookKernel', document); }));
    })));
}
exports.registerKernelCommands = registerKernelCommands;
function registerLegacyFileCommands(context, parserServer, clientMapper) {
    const eol = (0, vscodeUtilities_1.getEol)();
    const notebookFileFilters = {
        'Polyglot Notebooks': ['dib', 'dotnet-interactive'],
        'Jupyter Notebooks': ['ipynb'],
    };
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.newNotebook', () => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Create new blank notebook' is deprecated.  Please use the 'Polyglot Notebook: Create new blank notebook' command instead.`);
        yield vscode.commands.executeCommand('polyglot-notebook.newNotebook');
    })));
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.openNotebook', (notebookUri) => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Open notebook' is deprecated.  Please use the 'Polyglot Notebook: Open notebook' command instead.`);
        yield vscode.commands.executeCommand('polyglot-notebook.openNotebook', notebookUri);
    })));
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.saveAsNotebook', () => __awaiter(this, void 0, void 0, function* () {
        vscode.window.showWarningMessage(`The command '.NET Interactive: Save notebook as...' is deprecated.  Please use the 'Polyglot Notebook: Save notebook as...' command instead.`);
        yield vscode.commands.executeCommand('polyglot-notebook.saveAsNotebook');
    })));
}
function registerFileCommands(context, parserServer, clientMapper) {
    // todo: delete this later
    registerLegacyFileCommands(context, parserServer, clientMapper);
    const eol = (0, vscodeUtilities_1.getEol)();
    const notebookFileFilters = {
        'Polyglot Notebooks': ['dib', 'dotnet-interactive'],
        'Jupyter Notebooks': ['ipynb'],
    };
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebook', () => __awaiter(this, void 0, void 0, function* () {
        // offer to create either `.dib` or `.ipynb`
        const newDibNotebookText = `Create as '.dib'`;
        const newIpynbNotebookText = `Create as '.ipynb'`;
        const selected = yield vscode.window.showQuickPick([newDibNotebookText, newIpynbNotebookText]);
        switch (selected) {
            case newDibNotebookText:
                yield vscode.commands.executeCommand('polyglot-notebook.newNotebookDib');
                break;
            case newIpynbNotebookText:
                yield vscode.commands.executeCommand('polyglot-notebook.newNotebookIpynb');
                break;
            default:
                break;
        }
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebookDib', () => __awaiter(this, void 0, void 0, function* () {
        yield newNotebook('.dib');
    })));
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.newNotebookIpynb', () => __awaiter(this, void 0, void 0, function* () {
        // note, new .ipynb notebooks are currently affected by this bug: https://github.com/microsoft/vscode/issues/121974
        yield newNotebook('.ipynb');
        yield selectDotNetInteractiveKernelForJupyter();
    })));
    function newNotebook(extension) {
        return __awaiter(this, void 0, void 0, function* () {
            const viewType = extension === '.dib' || extension === '.dotnet-interactive'
                ? constants.NotebookViewType
                : constants.JupyterViewType;
            // get language
            const languagesAndKernelNames = {
                'C#': 'csharp',
                'F#': 'fsharp',
                'PowerShell': 'pwsh',
            };
            const newLanguageOptions = [];
            for (const languageName in languagesAndKernelNames) {
                newLanguageOptions.push(languageName);
            }
            const notebookLanguage = yield vscode.window.showQuickPick(newLanguageOptions, { title: 'Default Language' });
            if (!notebookLanguage) {
                return;
            }
            const kernelName = languagesAndKernelNames[notebookLanguage];
            const notebookCellMetadata = {
                kernelName
            };
            const rawCellMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
            const cell = new vscode.NotebookCellData(vscode.NotebookCellKind.Code, '', constants.CellLanguageIdentifier);
            cell.metadata = rawCellMetadata;
            const notebookDocumentMetadata = {
                kernelInfo: {
                    defaultKernelName: kernelName,
                    items: [
                        {
                            name: kernelName,
                            aliases: [],
                            languageName: kernelName // it just happens that the kernel names we allow are also the language names
                        }
                    ]
                }
            };
            const createForIpynb = viewType === constants.JupyterViewType;
            const rawNotebookMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, createForIpynb);
            const content = new vscode.NotebookData([cell]);
            content.metadata = rawNotebookMetadata;
            const notebook = yield vscode.workspace.openNotebookDocument(viewType, content);
            const _editor = yield vscode.window.showNotebookDocument(notebook);
        });
    }
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.openNotebook', (notebookUri) => __awaiter(this, void 0, void 0, function* () {
        // ensure we have a notebook uri
        if (!notebookUri) {
            const uris = yield vscode.window.showOpenDialog({
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
        yield openNotebook(notebookUri);
    })));
    function openNotebook(uri) {
        return __awaiter(this, void 0, void 0, function* () {
            const extension = path.extname(uri.toString());
            const viewType = extension === '.dib' || extension === '.dotnet-interactive'
                ? constants.NotebookViewType
                : constants.JupyterViewType;
            yield vscode.commands.executeCommand('vscode.openWith', uri, viewType);
        });
    }
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.saveAsNotebook', () => __awaiter(this, void 0, void 0, function* () {
        if (vscode.window.activeNotebookEditor) {
            const uri = yield vscode.window.showSaveDialog({
                filters: notebookFileFilters
            });
            if (!uri) {
                return;
            }
            const notebook = versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
            const interactiveDocument = (0, vscodeUtilities_1.toNotebookDocument)(notebook);
            const uriPath = uri.toString();
            const extension = path.extname(uriPath);
            const documentType = (0, utilities_1.extensionToDocumentType)(extension);
            const buffer = yield parserServer.serializeNotebook(documentType, eol, interactiveDocument);
            yield vscode.workspace.fs.writeFile(uri, buffer);
            switch (path.extname(uriPath)) {
                case '.dib':
                case '.dotnet-interactive':
                    yield vscode.commands.executeCommand('polyglot-notebook.openNotebook', uri);
                    break;
            }
        }
    })));
}
exports.registerFileCommands = registerFileCommands;
function selectDotNetInteractiveKernelForJupyter() {
    return __awaiter(this, void 0, void 0, function* () {
        const extension = 'ms-dotnettools.dotnet-interactive-vscode';
        const id = constants.JupyterKernelId;
        yield vscode.commands.executeCommand('notebook.selectKernel', { extension, id });
    });
}
exports.selectDotNetInteractiveKernelForJupyter = selectDotNetInteractiveKernelForJupyter;
// callbacks used to install interactive tool
function getInteractiveVersion(dotnetPath, globalStoragePath) {
    return __awaiter(this, void 0, void 0, function* () {
        const result = yield (0, utilities_1.executeSafe)(dotnetPath, ['tool', 'run', 'dotnet-interactive', '--', '--version'], globalStoragePath);
        if (result.code === 0) {
            const versionString = (0, utilities_1.getVersionNumber)(result.output);
            return versionString;
        }
        return undefined;
    });
}
//# sourceMappingURL=commands.js.map