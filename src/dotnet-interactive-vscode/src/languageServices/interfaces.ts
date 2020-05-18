// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LinePositionSpan } from './../contracts';

export interface DocumentLike {
    uri: {path: string};
    getText: {(): string};
}

export interface HoverResult {
    contents: string,
    isMarkdown: boolean;
    range: LinePositionSpan | undefined,
}

export interface PositionLike {
    line: number;
    character: number;
}
