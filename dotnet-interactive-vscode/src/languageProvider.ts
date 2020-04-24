import * as vscode from 'vscode';
import { InteractiveClient } from './interactiveClient';
import { HoverMarkdownProduced, LinePositionSpan, LinePosition, HoverPlainTextProduced, CompletionRequestCompleted } from './interfaces';

const selector = { language: 'dotnet-interactive' };

function convertToPosition(linePosition: LinePosition): vscode.Position {
    return new vscode.Position(linePosition.line, linePosition.character);
}

function convertToRange(linePositionSpan?: LinePositionSpan): (vscode.Range | undefined) {
    if (linePositionSpan === undefined) {
        return undefined;
    }

    return new vscode.Range(
        convertToPosition(linePositionSpan.start),
        convertToPosition(linePositionSpan.end)
    );
}

export class CompletionItemProvider implements vscode.CompletionItemProvider {
    static readonly triggerCharacters = ['.', '('];

    constructor(readonly client: InteractiveClient) {
    }

    provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
        return new Promise<vscode.CompletionList>((resolve, reject) => {
            let handled = false;
            this.client.completion(document.getText(), position.line, position.character, (event, eventType) => {
                if (eventType === "CommandHandled" && !handled) {
                    reject();
                } else if (eventType === 'CompletionRequestCompleted') {
                    handled = true;
                    let completion = <CompletionRequestCompleted>event;
                    let completionItems: Array<vscode.CompletionItem> = [];
                    for (let item of completion.completionList) {
                        let vscodeItem = new vscode.CompletionItem(item.displayText, vscode.CompletionItemKind.Function);
                        completionItems.push(vscodeItem);
                    }

                    let completionList = new vscode.CompletionList(completionItems, false);
                    resolve(completionList);
                }
            });
        });
    }
}

export class HoverProvider implements vscode.HoverProvider {
    constructor(readonly client: InteractiveClient) {
    }

    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Hover> {
        return new Promise<vscode.Hover>((resolve, reject) => {
            let handled = false;
            this.client.hover(document.getText(), position.line, position.character, (event, eventType) => {
                let content: vscode.MarkedString | undefined = undefined;
                let range: vscode.Range | undefined = undefined;
                switch (eventType) {
                    case "CommandHandled":
                        if (!handled) {
                            reject();
                        }
                        break;
                    case "HoverMarkdownProduced":
                        handled = true;
                        {
                            let hover = <HoverMarkdownProduced>event;
                            content = new vscode.MarkdownString(hover.content);
                            range = convertToRange(hover.range);
                        }
                        break;
                    case "HoverPlainTextProduced":
                        handled = true;
                        {
                            let hover = <HoverPlainTextProduced>event;
                            content = hover.content;
                            range = convertToRange(hover.range);
                        }
                        break;
                }

                if (content !== undefined) {
                    let hover = new vscode.Hover(content, range);
                    resolve(hover);
                }
            });
        });
    }
}

export function registerLanguageProviders(client: InteractiveClient): vscode.Disposable {
    const disposables: Array<vscode.Disposable> = [];

    disposables.push(vscode.languages.registerCompletionItemProvider(selector, new CompletionItemProvider(client), ...CompletionItemProvider.triggerCharacters));
    disposables.push(vscode.languages.registerHoverProvider(selector, new HoverProvider(client)));

    return vscode.Disposable.from(...disposables);
}
