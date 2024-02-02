// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { HoverResult, PositionLike } from './interfaces';
import { Uri } from '../interfaces/vscode-like';
import { debounceAndReject } from '../utilities';

export function provideHover(clientMapper: ClientMapper, language: string, documentUri: Uri, documentText: string, position: PositionLike, languageServiceDelay: number): Promise<HoverResult> {
    return debounceAndReject(`hover-${documentUri.toString()}`, languageServiceDelay, async () => {
        const client = await clientMapper.getOrAddClient(documentUri);
        const hoverText = await client.hover(language, documentText, position.line, position.character);
        if (hoverText.content.length > 0) {
            const content = hoverText.content.sort((a, b) => mimeTypeToPriority(a.mimeType) - mimeTypeToPriority(b.mimeType))[0];
            const hoverResult = {
                contents: content.value,
                isMarkdown: content.mimeType === 'text/markdown' || content.mimeType === 'text/x-markdown',
                range: hoverText.linePositionSpan
            };
            return hoverResult;
        } return {
            contents: "",
            isMarkdown: false,
            range: undefined
        };
    });
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
