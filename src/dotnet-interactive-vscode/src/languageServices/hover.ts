import { ClientMapper } from './../clientMapper';
import { CommandHandledType, HoverTextProduced, HoverTextProducedType } from './../contracts';
import { CancellationTokenLike, DocumentLike, HoverResult, PositionLike } from './interfaces';

export class Hover {
    static provideHover(clientMapper: ClientMapper, language: string, document: DocumentLike, position: PositionLike, token?: CancellationTokenLike): Promise<HoverResult> {
        return new Promise<HoverResult>((resolve, reject) => {
            let handled = false;
            let client = clientMapper.getOrAddClient(document.uri);
            client.hover(language, document.getText(), position.line, position.character).subscribe({
                next: value => {
                    let hoverResult: HoverResult | undefined = undefined;
                    switch (value.eventType) {
                        case CommandHandledType:
                            if (!handled) {
                                reject();
                            }
                            break;
                        case HoverTextProducedType:
                            let hoverText = <HoverTextProduced>value.event;
                            let content = hoverText.content.sort((a, b) => mimeTypeToPriority(a.mimeType) - mimeTypeToPriority(b.mimeType))[0];
                            hoverResult = {
                                contents: content.value,
                                isMarkdown: content.mimeType === 'text/markdown' || content.mimeType === 'text/x-markdown',
                                range: hoverText.range
                            };
                            break;
                    }

                    if (hoverResult !== undefined && !handled) {
                        handled = true;
                        resolve(hoverResult);
                    }
                }
            });
        });
    }
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
