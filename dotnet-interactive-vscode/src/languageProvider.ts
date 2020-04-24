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
                if (eventType === 'CommandHandled' && !handled) {
                    handled = true;
                    reject();
                } else if (eventType === 'CompletionRequestCompleted') {
                    handled = true;
                    let completion = <CompletionRequestCompleted>event;
                    let completionItems: Array<vscode.CompletionItem> = [];
                    for (let item of completion.completionList) {
                        let vscodeItem = new vscode.CompletionItem(item.displayText, this.mapCompletionItem(item.kind));
                        completionItems.push(vscodeItem);
                    }

                    let completionList = new vscode.CompletionList(completionItems, false);
                    resolve(completionList);
                }
            });
        });
    }

    private mapCompletionItem(completionItemText: string): vscode.CompletionItemKind {
        // incomplete mapping from http://sourceroslyn.io/#Microsoft.CodeAnalysis.Workspaces/Tags/WellKnownTags.cs
        switch (completionItemText) {
            case "Class": return vscode.CompletionItemKind.Class;
            case "Constant": return vscode.CompletionItemKind.Constant;
            case "Delegate": return vscode.CompletionItemKind.Function;
            case "Enum": return vscode.CompletionItemKind.Enum;
            case "EnumMember": return vscode.CompletionItemKind.EnumMember;
            case "Event": return vscode.CompletionItemKind.Event;
            case "ExtensionMethod": return vscode.CompletionItemKind.Method;
            case "Field": return vscode.CompletionItemKind.Field;
            case "Interface": return vscode.CompletionItemKind.Interface;
            case "Local": return vscode.CompletionItemKind.Variable;
            case "Method": return vscode.CompletionItemKind.Method;
            case "Module": return vscode.CompletionItemKind.Module;
            case "Namespace": return vscode.CompletionItemKind.Module;
            case "Property": return vscode.CompletionItemKind.Property;
            case "Structure": return vscode.CompletionItemKind.Struct;
            default: return vscode.CompletionItemKind.Text; // what's an appropriate default?
        }
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
                    case 'CommandHandled':
                        if (!handled) {
                            reject();
                        }
                        break;
                    case 'HoverMarkdownProduced':
                        handled = true;
                        {
                            let hover = <HoverMarkdownProduced>event;
                            content = new vscode.MarkdownString(hover.content);
                            range = convertToRange(hover.range);
                        }
                        break;
                    case 'HoverPlainTextProduced':
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
