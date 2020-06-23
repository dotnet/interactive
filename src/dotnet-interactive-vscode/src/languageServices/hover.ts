// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { HoverResult, PositionLike } from './interfaces';
import { Document } from '../interfaces/vscode';

export async function provideHover(clientMapper: ClientMapper, language: string, document: Document, position: PositionLike, token?: string | undefined): Promise<HoverResult> {
    let client = await clientMapper.getOrAddClient(document.uri);
    let hoverText = await client.hover(language, document.getText(), position.line, position.character, token);
    let content = hoverText.content.sort((a, b) => mimeTypeToPriority(a.mimeType) - mimeTypeToPriority(b.mimeType))[0];
    let hoverResult = {
        contents: content.value,
        isMarkdown: content.mimeType === 'text/markdown' || content.mimeType === 'text/x-markdown',
        range: hoverText.linePositionSpan
    };
    return hoverResult;
}

function mimeTypeToPriority(mimeType: string): number {
    switch (mimeType) {
        case 'text/markdown':
        case 'text/x-markdown':
            return 1;
        case 'text/plain':
            return 2;
        default:
            return 99;
    }
}
