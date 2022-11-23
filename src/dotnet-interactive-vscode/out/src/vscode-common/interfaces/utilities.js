"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.isNotebookParserServerError = exports.isNotebookSerializeResponse = exports.isNotebookParseResponse = exports.isNotebookParserServerResponse = exports.isUint8Array = exports.reshapeOutputValueForVsCode = exports.isTextOutput = exports.isDisplayOutput = exports.isErrorOutput = void 0;
const vscodeLike = require("./vscode-like");
function isErrorOutput(arg) {
    return arg
        && typeof arg.errorName === 'string'
        && typeof arg.errorValue === 'string'
        && Array.isArray(arg.stackTrace);
}
exports.isErrorOutput = isErrorOutput;
function isDisplayOutput(arg) {
    return arg
        && typeof arg.data === 'object';
}
exports.isDisplayOutput = isDisplayOutput;
function isTextOutput(arg) {
    return arg
        && typeof arg.text === 'string';
}
exports.isTextOutput = isTextOutput;
function reshapeOutputValueForVsCode(value, mime) {
    if (typeof value === 'string') {
        const encoder = new TextEncoder();
        const dataString = mime === vscodeLike.ErrorOutputMimeType
            ? JSON.stringify({
                ename: 'Error',
                evalue: value,
                traceback: [],
            })
            : value;
        const data = encoder.encode(dataString);
        return data;
    }
    else {
        return value;
    }
}
exports.reshapeOutputValueForVsCode = reshapeOutputValueForVsCode;
function isUint8Array(arg) {
    return arg
        && (typeof arg.length === 'number' || arg.type === 'Buffer');
}
exports.isUint8Array = isUint8Array;
function isNotebookParserServerResponse(arg) {
    return arg
        && typeof arg.id === 'string';
}
exports.isNotebookParserServerResponse = isNotebookParserServerResponse;
function isNotebookParseResponse(arg) {
    return arg
        && typeof arg.id === 'string'
        && typeof arg.document === 'object';
}
exports.isNotebookParseResponse = isNotebookParseResponse;
function isNotebookSerializeResponse(arg) {
    return arg
        && typeof arg.id === 'string'
        && isUint8Array(arg.rawData);
}
exports.isNotebookSerializeResponse = isNotebookSerializeResponse;
function isNotebookParserServerError(arg) {
    return arg
        && typeof arg.id === 'string'
        && typeof arg.errorMessage === 'string';
}
exports.isNotebookParserServerError = isNotebookParserServerError;
//# sourceMappingURL=utilities.js.map