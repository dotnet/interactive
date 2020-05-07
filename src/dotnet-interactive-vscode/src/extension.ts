import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { DotNetInteractiveNotebookProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { registerCommands } from './vscode/commands';

export function activate(context: vscode.ExtensionContext) {
    let clientMapper = new ClientMapper(() => new StdioKernelTransport());
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider(clientMapper)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
    registerCommands(context);
}

export function deactivate() {
}
