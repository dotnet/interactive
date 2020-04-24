import * as vscode from 'vscode';
import { DotNetInteractiveNotebookProvider } from './notebookProvider';
import { InteractiveClient } from './interactiveClient';
import { registerLanguageProviders } from './languageProvider';

export function activate(context: vscode.ExtensionContext) {
    let client = new InteractiveClient();
    context.subscriptions.push(vscode.notebook.registerNotebookProvider('dotnet-interactive', new DotNetInteractiveNotebookProvider(client)));
    context.subscriptions.push(registerLanguageProviders(client));
}

export function deactivate() {
    // TODO: shutdown server?
}
