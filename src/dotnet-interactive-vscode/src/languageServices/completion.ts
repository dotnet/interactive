// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { CompletionsProduced } from './../contracts';
import { PositionLike } from './interfaces';
import { Document } from 'vscode-interfaces/out/notebook';

export async function provideCompletion(clientMapper: ClientMapper, language: string, document: Document, position: PositionLike, token?: string | undefined): Promise<CompletionsProduced> {
    let client = await clientMapper.getOrAddClient(document.uri);
    let completion = await client.completion(language, document.getText(), position.line, position.character, token);
    return completion;
}
