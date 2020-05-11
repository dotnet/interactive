import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { registerCommands } from './vscode/commands';

import * as acquisition from './acquisition';

export async function activate(context: vscode.ExtensionContext) {
    const { dotnetPath, launchOptions } = await acquisition.getInteractiveLaunchOptions(context.globalStoragePath);
    const clientMapper = new ClientMapper(() => new StdioKernelTransport(dotnetPath, launchOptions));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', new DotNetInteractiveNotebookContentProvider(clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
    registerCommands(context);
}

export function deactivate() {
}
