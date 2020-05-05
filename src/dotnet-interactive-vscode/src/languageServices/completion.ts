import { ClientMapper } from './../clientMapper';
import { CompletionRequestCompleted, CompletionRequestCompletedType, CompletionItem } from './../contracts';
import { CancellationTokenLike, DocumentLike, PositionLike } from './interfaces';

export function provideCompletion(clientMapper: ClientMapper, language: string, document: DocumentLike, position: PositionLike, token?: CancellationTokenLike): Promise<Array<CompletionItem>> {
    return new Promise<Array<CompletionItem>>((resolve, reject) => {
        let handled = false;
        let client = clientMapper.getOrAddClient(document.uri);
        client.completion(language, document.getText(), position.line, position.character).subscribe({
            next: value => {
                switch (value.eventType) {
                    case CompletionRequestCompletedType:
                        handled = true;
                        let completion = <CompletionRequestCompleted>value.event;
                        let completionItems = completion.completionList;
                        resolve(completionItems);
                        break;
                }
            }
        });
    });
}
