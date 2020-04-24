import * as vscode from 'vscode';
import { DotNetInteractiveNotebookProvider } from './notebookProvider';
import { registerLanguageProviders } from './languageProvider';

export function activate(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider()));
    context.subscriptions.push(registerLanguageProviders());
}

export function deactivate() {
    // TODO: shutdown server?
}
