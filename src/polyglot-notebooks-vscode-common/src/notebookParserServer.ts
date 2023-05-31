// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import { MessageClient } from './messageClient';
import { isNotebookParserServerResponse, isNotebookParserServerError, isNotebookParseResponse, isNotebookSerializeResponse } from './interfaces/utilities';
import { Eol } from './interfaces';
import * as constants from './constants';

export class NotebookParserServer {
    private nextId: number = 1;

    constructor(private readonly messageClient: MessageClient) {
    }

    async parseInteractiveDocument(serializationType: commandsAndEvents.DocumentSerializationType, rawData: Uint8Array): Promise<commandsAndEvents.InteractiveDocument> {
        const request: commandsAndEvents.NotebookParseRequest = {
            type: commandsAndEvents.RequestType.Parse,
            id: this.getNextId(),
            serializationType,
            defaultLanguage: constants.CellLanguageIdentifier,
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
                        kernelName: 'csharp',
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

    async serializeNotebook(serializationType: commandsAndEvents.DocumentSerializationType, eol: Eol, document: commandsAndEvents.InteractiveDocument): Promise<Uint8Array> {
        const request: commandsAndEvents.NotebookSerializeRequest = {
            type: commandsAndEvents.RequestType.Serialize,
            id: this.getNextId(),
            serializationType,
            defaultLanguage: 'csharp',
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
