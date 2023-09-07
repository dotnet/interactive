// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from '../polyglot-notebooks/commandsAndEvents';

export interface HoverResult {
    contents: string,
    isMarkdown: boolean;
    range: commandsAndEvents.LinePositionSpan | undefined,
}

export interface PositionLike {
    line: number;
    character: number;
}
