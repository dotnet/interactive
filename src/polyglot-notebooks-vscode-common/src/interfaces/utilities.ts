// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from '../polyglot-notebooks/commandsAndEvents';
import * as vscodeLike from './vscode-like';

export function isErrorOutput(arg: any): arg is commandsAndEvents.ErrorElement {
    return arg
        && typeof arg.errorName === 'string'
        && typeof arg.errorValue === 'string'
        && Array.isArray(arg.stackTrace);
}

export function isDisplayOutput(arg: any): arg is commandsAndEvents.DisplayElement {
    return arg
        && typeof arg.data === 'object';
}

export function isTextOutput(arg: any): arg is commandsAndEvents.TextElement {
    return arg
        && typeof arg.text === 'string';
}

export function reshapeOutputValueForVsCode(value: Uint8Array | string, mime: string): Uint8Array {
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
    } else {
        return value;
    }
}

export function isUint8Array(arg: any): arg is Uint8Array {
    return arg
        && (typeof arg.length === 'number' || arg.type === 'Buffer');
}

export function isNotebookParserServerResponse(arg: any): arg is commandsAndEvents.NotebookParserServerResponse {
    return arg
        && typeof arg.id === 'string';
}

export function isNotebookParseResponse(arg: any): arg is commandsAndEvents.NotebookParseResponse {
    return arg
        && typeof arg.id === 'string'
        && typeof arg.document === 'object';
}

export function isNotebookSerializeResponse(arg: any): arg is commandsAndEvents.NotebookSerializeResponse {
    return arg
        && typeof arg.id === 'string'
        && isUint8Array(arg.rawData);
}
