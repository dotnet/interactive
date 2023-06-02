// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { PositionLike } from './interfaces';

import * as commandsAndEvents from '../polyglot-notebooks/commandsAndEvents';
import { debounceAndReject } from '../utilities';
import { Uri } from '../interfaces/vscode-like';

export function provideSignatureHelp(clientMapper: ClientMapper, language: string, documentUri: Uri, documentText: string, position: PositionLike, languageServiceDelay: number): Promise<commandsAndEvents.SignatureHelpProduced> {
    return debounceAndReject(`sighelp-${documentUri.toString()}`, languageServiceDelay, async () => {
        const client = await clientMapper.getOrAddClient(documentUri);
        const sigHelp = await client.signatureHelp(language, documentText, position.line, position.character);
        return sigHelp;
    });
}
