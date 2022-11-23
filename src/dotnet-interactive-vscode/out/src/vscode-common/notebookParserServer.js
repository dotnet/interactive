"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.NotebookParserServer = void 0;
const contracts = require("./dotnet-interactive/contracts");
const utilities_1 = require("./interfaces/utilities");
const constants = require("./constants");
class NotebookParserServer {
    constructor(messageClient) {
        this.messageClient = messageClient;
        this.nextId = 1;
    }
    parseInteractiveDocument(serializationType, rawData) {
        return __awaiter(this, void 0, void 0, function* () {
            const request = {
                type: contracts.RequestType.Parse,
                id: this.getNextId(),
                serializationType,
                defaultLanguage: constants.CellLanguageIdentifier,
                rawData,
            };
            let errorMessage = 'unknown';
            const response = yield this.messageClient.sendMessageAndGetResponse(request);
            if ((0, utilities_1.isNotebookParserServerResponse)(response)) {
                if ((0, utilities_1.isNotebookParseResponse)(response)) {
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
                }
                else if ((0, utilities_1.isNotebookParserServerError)(response)) {
                    errorMessage = response.errorMessage;
                }
            }
            errorMessage = `Unexpected response: ${JSON.stringify(response)}`;
            throw new Error(`Error parsing interactive document: ${errorMessage}`);
        });
    }
    serializeNotebook(serializationType, eol, document) {
        return __awaiter(this, void 0, void 0, function* () {
            const request = {
                type: contracts.RequestType.Serialize,
                id: this.getNextId(),
                serializationType,
                defaultLanguage: 'csharp',
                newLine: eol,
                document,
            };
            let errorMessage = 'unknown';
            const response = yield this.messageClient.sendMessageAndGetResponse(request);
            if ((0, utilities_1.isNotebookParserServerResponse)(response)) {
                if ((0, utilities_1.isNotebookSerializeResponse)(response)) {
                    return response.rawData;
                }
                else if ((0, utilities_1.isNotebookParserServerError)(response)) {
                    errorMessage = response.errorMessage;
                }
            }
            errorMessage = `Unexepcted response: ${JSON.stringify(response)}`;
            throw new Error(`Error serializing interactive document: ${errorMessage}`);
        });
    }
    getNextId() {
        return `${this.nextId++}`;
    }
}
exports.NotebookParserServer = NotebookParserServer;
//# sourceMappingURL=notebookParserServer.js.map