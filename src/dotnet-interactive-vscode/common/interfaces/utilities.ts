// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './contracts';
import * as vscodeLike from './vscode-like';

export function isKernelEventEnvelope(obj: any): obj is contracts.KernelEventEnvelope {
    return obj.eventType
        && obj.event;
}

export function isKernelCommandEnvelope(obj: any): obj is contracts.KernelCommandEnvelope {
    return obj.commandType
        && obj.command;
}

export function isErrorOutput(arg: any): arg is contracts.ErrorElement {
    return arg
        && typeof arg.errorName === 'string'
        && typeof arg.errorValue === 'string'
        && Array.isArray(arg.stackTrace);
}

export function isDisplayOutput(arg: any): arg is contracts.DisplayElement {
    return arg
        && typeof arg.data === 'object';
}

export function isTextOutput(arg: any): arg is contracts.TextElement {
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
