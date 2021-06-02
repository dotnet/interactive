// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { PositionLike } from './interfaces';
import { Document } from '../interfaces/vscode-like';

import * as contracts from '../interfaces/contracts';
import { debounceAndReject } from '../utilities';

export function provideSignatureHelp(clientMapper: ClientMapper, language: string, document: Document, position: PositionLike, languageServiceDelay: number, token?: string | undefined): Promise<contracts.SignatureHelpProduced> {
    return debounceAndReject(`sighelp-${document.uri.toString()}`, languageServiceDelay, async () => {
        const client = await clientMapper.getOrAddClient(document.notebook?.uri || document.uri);
        const sigHelp = await client.signatureHelp(language, document.getText(), position.line, position.character, token);
        return sigHelp;
    });
}
