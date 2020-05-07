import { ClientMapper } from './../clientMapper';
import { DocumentLike, HoverResult, PositionLike } from './interfaces';

export async function provideHover(clientMapper: ClientMapper, language: string, document: DocumentLike, position: PositionLike, token?: string | undefined): Promise<HoverResult> {
    let client = clientMapper.getOrAddClient(document.uri);
    let hoverText = await client.hover(language, document.getText(), position.line, position.character, token);
    let content = hoverText.content.sort((a, b) => mimeTypeToPriority(a.mimeType) - mimeTypeToPriority(b.mimeType))[0];
    let hoverResult = {
        contents: content.value,
        isMarkdown: content.mimeType === 'text/markdown' || content.mimeType === 'text/x-markdown',
        range: hoverText.range
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
