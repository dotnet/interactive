// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { PositionLike } from './interfaces';
import { Uri } from '../interfaces/vscode-like';

import * as contracts from '../dotnet-interactive/contracts';
import { debounceAndReject } from '../utilities';

export function provideCompletion(clientMapper: ClientMapper, language: string, documentUri: Uri, documentText: string, position: PositionLike, languageServiceDelay: number, token?: string | undefined): Promise<contracts.CompletionsProduced> {
    return debounceAndReject(`completion-${documentUri.toString()}`, languageServiceDelay, async () => {
        const client = await clientMapper.getOrAddClient(documentUri);
        const completion = await client.completion(language, documentText, position.line, position.character, token);
        return completion;
    });
}
