import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { DotNetInteractiveNotebookProvider } from './vscode/notebookProvider';
import { registerLanguageProviders } from './vscode/languageProvider';
import { StdioClientAdapter } from './stdioClientAdapter';

export function activate(context: vscode.ExtensionContext) {
    let clientMapper = new ClientMapper(targetKernelName => new StdioClientAdapter(targetKernelName));
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider(clientMapper)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
}

export function deactivate() {
}
