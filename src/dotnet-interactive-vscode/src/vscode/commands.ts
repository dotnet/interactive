import * as vscode from 'vscode';
import * as jp from '../interop/jupyter';

export function registerCommands(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.exportAsJupyterNotebook', async () => {
        if (vscode.notebook.activeNotebookEditor) {
            const uri = await vscode.window.showSaveDialog({
                filters: {
                    'Jupyter Notebook Files': ['ipynb']
                }
            });
            if (!uri) {
                return;
            }

            const { document } = vscode.notebook.activeNotebookEditor;
            const jupyter = jp.convertToJupyter(document);
            await vscode.workspace.fs.writeFile(uri, Buffer.from(JSON.stringify(jupyter, null, 1)));
        }
    }));
}
