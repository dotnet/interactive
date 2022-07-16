// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './dotnet-interactive/contracts';
import { defaultNotebookCellLanguage } from './interactiveNotebook';
import { MessageClient } from './messageClient';
import { isNotebookParserServerResponse, isNotebookParserServerError, isNotebookParseResponse, isNotebookSerializeResponse } from './interfaces/utilities';
import { Eol } from './interfaces';

export class NotebookParserServer {
    private nextId: number = 1;

    constructor(private readonly messageClient: MessageClient) {
    }

    async parseInteractiveDocument(serializationType: contracts.DocumentSerializationType, rawData: Uint8Array): Promise<contracts.InteractiveDocument> {
        const request: contracts.NotebookParseRequest = {
            type: contracts.RequestType.Parse,
            id: this.getNextId(),
            serializationType,
            defaultLanguage: defaultNotebookCellLanguage,
            rawData,
        };

        let errorMessage = 'unknown';
        const response = await this.messageClient.sendMessageAndGetResponse(request);
        if (isNotebookParserServerResponse(response)) {
            if (isNotebookParseResponse(response)) {
                const responseDocument = response.document;
                const notebookCells = responseDocument.elements;
                if (notebookCells.length === 0) {
                    // ensure at least one cell
                    notebookCells.push({
                        executionOrder: 0,
                        language: defaultNotebookCellLanguage,
                        contents: '',
                        outputs: [],
                    });
                }

                return responseDocument;
            } else if (isNotebookParserServerError(response)) {
                errorMessage = response.errorMessage;
            }
        }

        errorMessage = `Unexpected response: ${JSON.stringify(response)}`;
        throw new Error(`Error parsing interactive document: ${errorMessage}`);
    }

    async serializeNotebook(serializationType: contracts.DocumentSerializationType, eol: Eol, document: contracts.InteractiveDocument): Promise<Uint8Array> {
        const request: contracts.NotebookSerializeRequest = {
            type: contracts.RequestType.Serialize,
            id: this.getNextId(),
            serializationType,
            defaultLanguage: defaultNotebookCellLanguage,
            newLine: eol,
            document,
        };

        let errorMessage = 'unknown';
        const response = await this.messageClient.sendMessageAndGetResponse(request);
        if (isNotebookParserServerResponse(response)) {
            if (isNotebookSerializeResponse(response)) {
                return response.rawData;
            } else if (isNotebookParserServerError(response)) {
                errorMessage = response.errorMessage;
            }
        }

        errorMessage = `Unexepcted response: ${JSON.stringify(response)}`;
        throw new Error(`Error serializing interactive document: ${errorMessage}`);
    }

    private getNextId(): string {
        return `${this.nextId++}`;
    }
}
