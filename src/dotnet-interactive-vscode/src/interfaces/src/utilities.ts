// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    NotebookCellDisplayOutput,
    NotebookCellErrorOutput,
    NotebookCellTextOutput,
} from './contracts';

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
