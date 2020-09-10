// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from '../clientMapper';
import { provideCompletion } from '../languageServices/completion';
import { provideHover } from './../languageServices/hover';
import { notebookCellLanguages, getSimpleLanguage, notebookCellChanged } from '../interactiveNotebook';
import { convertToRange, toVsCodeDiagnostic } from './vscodeUtilities';
import { getDiagnosticCollection } from './diagnostics';

export class CompletionItemProvider implements vscode.CompletionItemProvider {
    static readonly triggerCharacters = ['.', '('];

    constructor(readonly clientMapper: ClientMapper) {
    }

    provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
        const completionPromise = provideCompletion(this.clientMapper, getSimpleLanguage(document.languageId), document, position);
        return ensureErrorsAreRejected(completionPromise, result => {
            let range: vscode.Range | undefined = undefined;
            if (result.linePositionSpan) {
                range = new vscode.Range(
                    new vscode.Position(result.linePositionSpan.start.line, result.linePositionSpan.start.character),
                    new vscode.Position(result.linePositionSpan.end.line, result.linePositionSpan.end.character));
            }
            const completionItems: Array<vscode.CompletionItem> = [];
            for (const item of result.completions) {
                const vscodeItem: vscode.CompletionItem = {
                    label: item.displayText,
                    documentation: item.documentation,
                    filterText: item.filterText,
                    insertText: item.insertText,
                    sortText: item.sortText,
                    range: range,
                    kind: this.mapCompletionItem(item.kind)
                };
                completionItems.push(vscodeItem);
            }

            const completionList = new vscode.CompletionList(completionItems, false);
            return completionList;
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
            case "Value": return vscode.CompletionItemKind.Value;
            default: return vscode.CompletionItemKind.Text; // what's an appropriate default?
        }
    }
}

export class HoverProvider implements vscode.HoverProvider {
    constructor(readonly clientMapper: ClientMapper) {
    }

    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Hover> {
        const hoverPromise = provideHover(this.clientMapper, getSimpleLanguage(document.languageId), document, position);
        return ensureErrorsAreRejected(hoverPromise, result => {
            const contents = result.isMarkdown
                ? new vscode.MarkdownString(result.contents)
                : result.contents;
            const hover = new vscode.Hover(contents, convertToRange(result.range));
            return hover;
        });
    }
}

function ensureErrorsAreRejected<TInterimResult, TFinalResult>(promise: Promise<TInterimResult>, successHandler: { (result: TInterimResult): TFinalResult }): Promise<TFinalResult> {
    return new Promise<TFinalResult>((resolve, reject) => {
        promise.then(interimResult => {
            const finalResult = successHandler(interimResult);
            resolve(finalResult);
        }).catch(err => {
            reject(err);
        });
    });
}

export function registerLanguageProviders(clientMapper: ClientMapper, diagnosticDelay: number): vscode.Disposable {
    const disposables: Array<vscode.Disposable> = [];

    let languages = [...notebookCellLanguages, "dotnet-interactive.magic-commands"];
    disposables.push(vscode.languages.registerCompletionItemProvider(languages, new CompletionItemProvider(clientMapper), ...CompletionItemProvider.triggerCharacters));
    disposables.push(vscode.languages.registerHoverProvider(languages, new HoverProvider(clientMapper)));
    disposables.push(vscode.workspace.onDidChangeTextDocument(e => {
        if (vscode.languages.match(notebookCellLanguages, e.document)) {
            const cell = vscode.notebook.activeNotebookEditor?.document.cells.find(cell => cell.document === e.document);
            if (cell) {
                notebookCellChanged(clientMapper, e.document, getSimpleLanguage(cell.language), diagnosticDelay, diagnostics => {
                    const collection = getDiagnosticCollection(e.document.uri);
                    collection.set(e.document.uri, diagnostics.map(toVsCodeDiagnostic));
                });
            }
        }
    }));

    return vscode.Disposable.from(...disposables);
}
