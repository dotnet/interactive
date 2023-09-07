// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { PositionLike } from './interfaces';
import { Uri } from '../interfaces/vscode-like';

import * as commandsAndEvents from '../polyglot-notebooks/commandsAndEvents';
import { debounceAndReject } from '../utilities';

export function provideCompletion(clientMapper: ClientMapper, kernelName: string, documentUri: Uri, documentText: string, position: PositionLike, languageServiceDelay: number): Promise<commandsAndEvents.CompletionsProduced> {
    return debounceAndReject(`completion-${documentUri.toString()}`, languageServiceDelay, async () => {
        const client = await clientMapper.getOrAddClient(documentUri);
        const completion = await client.completion(kernelName, documentText, position.line, position.character);
        return completion;
    });
}
