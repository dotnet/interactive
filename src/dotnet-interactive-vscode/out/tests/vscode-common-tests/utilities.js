"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.decodeNotebookCellOutputs = exports.decodeToString = exports.createChannelConfig = exports.withFakeGlobalStorageLocation = void 0;
const fs = require("fs");
const path = require("path");
const tmp = require("tmp");
const vscodeLike = require("../../src/vscode-common/interfaces/vscode-like");
const utilities_1 = require("../../src/vscode-common/utilities");
function withFakeGlobalStorageLocation(createLocation, callback) {
    return new Promise((resolve, reject) => {
        tmp.dir({ unsafeCleanup: true }, (err, dir) => {
            if (err) {
                reject();
                throw err;
            }
            // VS Code doesn't guarantee that the global storage path is present, so we have to go one directory deeper
            let globalStoragePath = path.join(dir, 'globalStoragePath');
            if (createLocation) {
                fs.mkdirSync(globalStoragePath);
            }
            callback(globalStoragePath).then(() => {
                resolve();
            });
        });
    });
}
exports.withFakeGlobalStorageLocation = withFakeGlobalStorageLocation;
function createChannelConfig(channelCreator) {
    function defaultChannelCreator(notebookUri) {
        throw new Error('Each test needs to override this.');
    }
    const encoder = new TextEncoder();
    function configureKernel() {
        // noop
    }
    const defaultClientMapperConfig = {
        channelCreator: defaultChannelCreator,
        createErrorOutput: (message, outputId) => {
            const errorItem = {
                mime: 'application/vnd.code.notebook.error',
                data: encoder.encode(JSON.stringify({
                    name: 'Error',
                    message,
                })),
            };
            const cellOutput = (0, utilities_1.createOutput)([errorItem], outputId);
            return cellOutput;
        },
        diagnosticChannel: undefined,
        configureKernel,
    };
    return Object.assign(Object.assign({}, defaultClientMapperConfig), { channelCreator: channelCreator });
}
exports.createChannelConfig = createChannelConfig;
function decodeToString(data) {
    const decoder = new TextDecoder('utf-8');
    const decoded = decoder.decode(data);
    return decoded;
}
exports.decodeToString = decodeToString;
function decodeNotebookCellOutputs(outputs) {
    const jsonLikeMimeTypes = new Set();
    jsonLikeMimeTypes.add('application/json');
    jsonLikeMimeTypes.add(vscodeLike.ErrorOutputMimeType);
    return outputs.map(o => (Object.assign(Object.assign({}, o), { items: o.items.map(oi => {
            const decoded = decodeToString(oi.data);
            let result = Object.assign(Object.assign({}, oi), { decodedData: jsonLikeMimeTypes.has(oi.mime) ? JSON.parse(decoded) : decoded });
            delete result.data;
            return result;
        }) })));
}
exports.decodeNotebookCellOutputs = decodeNotebookCellOutputs;
//# sourceMappingURL=utilities.js.map