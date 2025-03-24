// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import * as utilities from './interfaces/utilities';
import * as vscodeLike from './interfaces/vscode-like';
import { languageToCellKind } from './interactiveNotebook';
import * as vscodeUtilities from './vscodeUtilities';
import { NotebookParserServer } from './notebookParserServer';
import { Eol } from './interfaces';
import * as metadataUtilities from './metadataUtilities';
import * as constants from './constants';

function toInteractiveDocumentElement(cell: vscode.NotebookCellData): commandsAndEvents.InteractiveDocumentElement {
    // just need to match the shape
    const tempCell: vscodeLike.NotebookCell = {
        kind: vscodeLike.NotebookCellKind.Code,
        metadata: cell.metadata ?? {}
    };
    const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(tempCell);
    const outputs = cell.outputs || [];
    const kernelName = cell.languageId === 'markdown' ? 'markdown' : notebookCellMetadata.kernelName ?? 'csharp';

    const interactiveDocumentElement: commandsAndEvents.InteractiveDocumentElement = {
        executionOrder: cell.executionSummary?.executionOrder ?? 0,
        kernelName: kernelName,
        contents: cell.value,
        outputs: outputs.map(vscodeUtilities.vsCodeCellOutputToContractCellOutput)
    };

    return interactiveDocumentElement;
}

async function deserializeNotebookByType(parserServer: NotebookParserServer, serializationType: commandsAndEvents.DocumentSerializationType, rawData: Uint8Array): Promise<vscode.NotebookData> {
    const interactiveDocument = await parserServer.parseInteractiveDocument(serializationType, rawData);
    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromInteractiveDocument(interactiveDocument);
    const createForIpynb = serializationType === commandsAndEvents.DocumentSerializationType.Ipynb;
    const rawNotebookDocumentMetadata = metadataUtilities.getMergedRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookMetadata, {}, createForIpynb);
    const notebookData: vscode.NotebookData = {
        cells: interactiveDocument.elements.map(element => toVsCodeNotebookCellData(element)),
        metadata: rawNotebookDocumentMetadata
    };
    return notebookData;
}

async function serializeNotebookByType(parserServer: NotebookParserServer, serializationType: commandsAndEvents.DocumentSerializationType, eol: Eol, data: vscode.NotebookData): Promise<Uint8Array> {
    // just need to match the shape
    const fakeNotebookDocument: vscodeLike.NotebookDocument = {
        uri: {
            fsPath: 'unused',
            scheme: 'unused'
        },
        metadata: data.metadata ?? {}
    };
    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(fakeNotebookDocument);
    const interactiveDocument: commandsAndEvents.InteractiveDocument = {
        elements: data.cells.map(toInteractiveDocumentElement),
        metadata: notebookMetadata
    };
    const rawData = await parserServer.serializeNotebook(serializationType, eol, interactiveDocument);
    return rawData;
}

export function createAndRegisterNotebookSerializers(context: vscode.ExtensionContext, parserServer: NotebookParserServer): Map<string, vscode.NotebookSerializer> {
    const eol = vscodeUtilities.getEol();
    const createAndRegisterSerializer = (serializationType: commandsAndEvents.DocumentSerializationType, notebookType: string): vscode.NotebookSerializer => {
        const serializer: vscode.NotebookSerializer = {
            deserializeNotebook(content: Uint8Array, _token: vscode.CancellationToken): Promise<vscode.NotebookData> {
                return deserializeNotebookByType(parserServer, serializationType, content);
            },
            serializeNotebook(data: vscode.NotebookData, _token: vscode.CancellationToken): Promise<Uint8Array> {
                return serializeNotebookByType(parserServer, serializationType, eol, data);
            },
        };

        const notebookoptions: vscode.NotebookDocumentContentOptions = notebookType === commandsAndEvents.DocumentSerializationType.Dib
            // This is intended to prevent .dib files from being marked dirty when cells are run, since outputs aren't preserved.
            // FIX: This doesn't prevent the notebook from being marked dirty when the cell is run.
            ? {
                transientOutputs: true,
                transientDocumentMetadata: { custom: true },
                transientCellMetadata: { custom: true }
            }
            // .ipynb is handled directly by VS Code
            : {};

        const notebookSerializer = vscode.workspace.registerNotebookSerializer(notebookType, serializer, notebookoptions);
        context.subscriptions.push(notebookSerializer);
        return serializer;
    };

    const serializers = new Map<string, vscode.NotebookSerializer>();
    serializers.set(constants.NotebookViewType, createAndRegisterSerializer(commandsAndEvents.DocumentSerializationType.Dib, constants.NotebookViewType));
    serializers.set(constants.JupyterViewType, createAndRegisterSerializer(commandsAndEvents.DocumentSerializationType.Ipynb, constants.JupyterNotebookViewType));
    return serializers;
}

function toVsCodeNotebookCellData(cell: commandsAndEvents.InteractiveDocumentElement): vscode.NotebookCellData {
    const cellData = new vscode.NotebookCellData(
        <number>languageToCellKind(cell.kernelName),
        cell.contents,
        cell.kernelName === 'markdown' ? 'markdown' : constants.CellLanguageIdentifier);
    const notebookCellMetadata: metadataUtilities.NotebookCellMetadata = {
        kernelName: cell.kernelName
    };
    const rawNotebookCellMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
    cellData.metadata = rawNotebookCellMetadata;
    cellData.outputs = cell.outputs.map(outputElementToVsCodeCellOutput);
    return cellData;
}

export function outputElementToVsCodeCellOutput(output: commandsAndEvents.InteractiveDocumentOutputElement): vscode.NotebookCellOutput {
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
