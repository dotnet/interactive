import * as vscode from 'vscode';
import { DotNetInteractiveNotebookProvider } from './notebookProvider';
import { registerLanguageProviders } from './languageProvider';
import { ClientMapper } from './clientMapper';

export function activate(context: vscode.ExtensionContext) {
    let clientMapper = new ClientMapper();
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider(clientMapper)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
}

export function deactivate() {
}
