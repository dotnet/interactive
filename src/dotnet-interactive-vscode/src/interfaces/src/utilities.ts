// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    NotebookCellDisplayOutput,
    NotebookCellErrorOutput,
    NotebookCellTextOutput,
} from './contracts';
import * as notebook from './notebook';

export function isErrorOutput(arg: any): arg is NotebookCellErrorOutput {
    return arg
        && typeof arg.errorName === 'string'
        && typeof arg.errorValue === 'string'
        && Array.isArray(arg.stackTrace);
}

export function isDisplayOutput(arg: any): arg is NotebookCellDisplayOutput {
    return arg
        && typeof arg.data === 'object';
}

export function isTextOutput(arg: any): arg is NotebookCellTextOutput {
    return arg
        && typeof arg.text === 'string';
}

export function reshapeOutputValueForVsCode(mimeType: string, value: unknown): unknown {
    if (mimeType === notebook.ErrorOutputMimeType &&
        typeof value === 'string') {
        // this looks suspiciously like an error message; make sure it's the shape that vs code expects
        return {
            ename: 'Error',
            evalue: value,
            traceback: [],
        };
    }

    // no change
    return value;
}
