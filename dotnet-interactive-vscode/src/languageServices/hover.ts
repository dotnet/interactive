import { ClientMapper } from './../clientMapper';
import { HoverMarkdownProduced } from './../events';
import { CancellationTokenLike, DocumentLike, HoverResult, PositionLike } from './interfaces';

export class Hover {
    static provideHover(clientMapper: ClientMapper, document: DocumentLike, position: PositionLike, token?: CancellationTokenLike): Promise<HoverResult> {
        return new Promise<HoverResult>((resolve, reject) => {
            let handled = false;
            let client = clientMapper.getClient(document.uri);
            if (client === undefined) {
                reject();
                return;
            }

            client.hover(document.getText(), position.line, position.character).subscribe({
                next: value => {
                    let hoverResult: HoverResult | undefined = undefined;
                    switch (value.eventType) {
                        case 'CommandHandled':
                            if (!handled) {
                                reject();
                            }
                            break;
                        case 'HoverMarkdownProduced':
                            handled = true;
                            let hoverMarkdown = <HoverMarkdownProduced>value.event;
                            hoverResult = {
                                contents: hoverMarkdown.content,
                                isMarkdown: true,
                                range: hoverMarkdown.range
                            };
                            break;
                        case 'HoverPlainTextProduced':
                            handled = true;
                            let hoverPlain = <HoverMarkdownProduced>value.event;
                            hoverResult = {
                                contents: hoverPlain.content,
                                isMarkdown: false,
                                range: hoverPlain.range
                            };
                            break;
                    }

                    if (hoverResult !== undefined) {
                        resolve(hoverResult);
                    }
                }
            });
        });
    }
}
