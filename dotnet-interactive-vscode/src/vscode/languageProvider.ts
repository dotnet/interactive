import * as vscode from 'vscode';
import { LinePositionSpan, LinePosition, CompletionRequestCompleted } from '../events';
import { ClientMapper } from '../clientMapper';
import { Hover } from './../languageServices/hover';
import { provideCompletion } from '../languageServices/completion';
import { editorLanguages } from '../interactiveNotebook';

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

    constructor(readonly clientMapper: ClientMapper) {
    }

    provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
        return new Promise<vscode.CompletionList>((resolve, reject) => {
            provideCompletion(this.clientMapper, document.languageId, document, position, token).then(result => {
                let completionItems: Array<vscode.CompletionItem> = [];
                for (let item of result) {
                    let vscodeItem = new vscode.CompletionItem(item.displayText, this.mapCompletionItem(item.kind));
                    completionItems.push(vscodeItem);
                }
                let completionList = new vscode.CompletionList(completionItems, false);
                resolve(completionList);
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
    constructor(readonly clientMapper: ClientMapper) {
    }

    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Hover> {
        return new Promise<vscode.Hover>((resolve, reject) => {
            Hover.provideHover(this.clientMapper, document.languageId, document, position, token).then(result => {
                let contents = result.isMarkdown
                    ? new vscode.MarkdownString(result.contents)
                    : result.contents;
                let hover = new vscode.Hover(contents, convertToRange(result.range));
                resolve(hover);
            });
        });
    }
}

export function registerLanguageProviders(clientMapper: ClientMapper): vscode.Disposable {
    const disposables: Array<vscode.Disposable> = [];

    disposables.push(vscode.languages.registerCompletionItemProvider(editorLanguages, new CompletionItemProvider(clientMapper), ...CompletionItemProvider.triggerCharacters));
    disposables.push(vscode.languages.registerHoverProvider(editorLanguages, new HoverProvider(clientMapper)));

    return vscode.Disposable.from(...disposables);
}
