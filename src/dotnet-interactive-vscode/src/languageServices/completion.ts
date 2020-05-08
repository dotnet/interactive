import { ClientMapper } from './../clientMapper';
import { CompletionItem } from './../contracts';
import { DocumentLike, PositionLike } from './interfaces';

export async function provideCompletion(clientMapper: ClientMapper, language: string, document: DocumentLike, position: PositionLike, token?: string | undefined): Promise<Array<CompletionItem>> {
    let client = clientMapper.getOrAddClient(document.uri);
    let completion = await client.completion(language, document.getText(), position.line, position.character, token);
    let completionItems = completion.completionList;
    return completionItems;
}
