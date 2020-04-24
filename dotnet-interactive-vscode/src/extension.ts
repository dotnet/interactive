import * as vscode from 'vscode';
import { DotNetInteractiveNotebookProvider } from './notebookProvider';

export function activate(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider()));
}

export function deactivate() {
    // TODO: shutdown server?
}
