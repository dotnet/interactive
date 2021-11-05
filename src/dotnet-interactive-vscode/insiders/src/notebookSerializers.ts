// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as utilities from './common/interfaces/utilities';
import * as vscodeLike from './common/interfaces/vscode-like';
import { getNotebookSpecificLanguage, getSimpleLanguage, languageToCellKind } from './common/interactiveNotebook';
import { getEol, vsCodeCellOutputToContractCellOutput } from './common/vscode/vscodeUtilities';
import { NotebookParserServer } from './common/notebookParserServer';
import { Eol } from './common/interfaces';

function toInteractiveDocumentElement(cell: vscode.NotebookCellData): contracts.InteractiveDocumentElement {
    const outputs = cell.outputs || [];
    return {
        language: getSimpleLanguage(cell.languageId),
        contents: cell.value,
        outputs: outputs.map(vsCodeCellOutputToContractCellOutput)
    };
}

async function deserializeNotebookByType(parserServer: NotebookParserServer, serializationType: contracts.DocumentSerializationType, rawData: Uint8Array): Promise<vscode.NotebookData> {
    const interactiveDocument = await parserServer.parseInteractiveDocument(serializationType, rawData);
    const notebookData: vscode.NotebookData = {
        cells: interactiveDocument.elements.map(toVsCodeNotebookCellData)
    };
    return notebookData;
}

async function serializeNotebookByType(parserServer: NotebookParserServer, serializationType: contracts.DocumentSerializationType, eol: Eol, data: vscode.NotebookData): Promise<Uint8Array> {
    const interactiveDocument: contracts.InteractiveDocument = {
        elements: data.cells.map(toInteractiveDocumentElement)
    };
    const rawData = await parserServer.serializeNotebook(serializationType, eol, interactiveDocument);
    return rawData;
}

export function createAndRegisterNotebookSerializers(context: vscode.ExtensionContext, parserServer: NotebookParserServer): Map<string, vscode.NotebookSerializer> {
    const eol = getEol();
    const createAndRegisterSerializer = (serializationType: contracts.DocumentSerializationType, notebookType: string): vscode.NotebookSerializer => {
        const serializer: vscode.NotebookSerializer = {
            deserializeNotebook(content: Uint8Array, _token: vscode.CancellationToken): Promise<vscode.NotebookData> {
                return deserializeNotebookByType(parserServer, serializationType, content);
            },
            serializeNotebook(data: vscode.NotebookData, _token: vscode.CancellationToken): Promise<Uint8Array> {
                return serializeNotebookByType(parserServer, serializationType, eol, data);
            },
        };
        const notebookSerializer = vscode.workspace.registerNotebookSerializer(notebookType, serializer);
        context.subscriptions.push(notebookSerializer);
        return serializer;
    };

    const serializers = new Map<string, vscode.NotebookSerializer>();
    serializers.set('dotnet-interactive', createAndRegisterSerializer(contracts.DocumentSerializationType.Dib, 'dotnet-interactive'));
    serializers.set('jupyter-notebook', createAndRegisterSerializer(contracts.DocumentSerializationType.Ipynb, 'dotnet-interactive-jupyter'));
    return serializers;
}

function toVsCodeNotebookCellData(cell: contracts.InteractiveDocumentElement): vscode.NotebookCellData {
    const cellData = new vscode.NotebookCellData(
        <number>languageToCellKind(cell.language),
        cell.contents,
        getNotebookSpecificLanguage(cell.language));
    cellData.outputs = cell.outputs.map(outputElementToVsCodeCellOutput);
    return cellData;
}

function outputElementToVsCodeCellOutput(output: contracts.InteractiveDocumentOutputElement): vscode.NotebookCellOutput {
    const outputItems: Array<vscode.NotebookCellOutputItem> = [];
    if (utilities.isDisplayOutput(output)) {
        for (const mimeKey in output.data) {
            outputItems.push(generateVsCodeNotebookCellOutputItem(output.data[mimeKey], mimeKey));
        }
    } else if (utilities.isErrorOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem(output.errorValue, vscodeLike.ErrorOutputMimeType));
    } else if (utilities.isTextOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem(output.text, 'text/plain'));
    }

    return new vscode.NotebookCellOutput(outputItems);
}

function generateVsCodeNotebookCellOutputItem(value: Uint8Array | string, mime: string): vscode.NotebookCellOutputItem {
    const displayValue = utilities.reshapeOutputValueForVsCode(value, mime);
    return new vscode.NotebookCellOutputItem(displayValue, mime);
}
