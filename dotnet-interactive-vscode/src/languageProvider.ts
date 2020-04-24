import * as vscode from 'vscode';

const selector = { language: 'dotnet-interactive' };

export class HoverProvider implements vscode.HoverProvider {
    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Hover> {
        return Promise.resolve(new vscode.Hover(new vscode.MarkdownString(`Executing hover at position \`(${position.line}, ${position.character})\``)));
    }
}

export function registerLanguageProviders(): vscode.Disposable {
    const disposables: vscode.Disposable[] = [];

    disposables.push(vscode.languages.registerHoverProvider(selector, new HoverProvider()));

    return vscode.Disposable.from(...disposables);
}
