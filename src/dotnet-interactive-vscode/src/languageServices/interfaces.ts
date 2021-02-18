// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from 'dotnet-interactive-vscode-interfaces/out/contracts';

export interface HoverResult {
    contents: string,
    isMarkdown: boolean;
    range: contracts.LinePositionSpan | undefined,
}

export interface PositionLike {
    line: number;
    character: number;
}
