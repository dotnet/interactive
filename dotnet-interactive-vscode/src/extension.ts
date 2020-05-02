import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { DotNetInteractiveNotebookProvider } from './vscode/notebookProvider';
import { StdioClientTransport } from './stdioClientTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { registerCommands } from './vscode/commands';

export function activate(context: vscode.ExtensionContext) {
    let clientMapper = new ClientMapper(() => new StdioClientTransport());
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider(clientMapper)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
    registerCommands(context);
}

export function deactivate() {
}
