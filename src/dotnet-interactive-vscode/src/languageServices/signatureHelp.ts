// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientMapper } from './../clientMapper';
import { PositionLike } from './interfaces';
import { Document } from '../interfaces/vscode';
import { SignatureHelpProduced } from '../contracts';

export async function provideSignatureHelp(clientMapper: ClientMapper, language: string, document: Document, position: PositionLike, token?: string | undefined): Promise<SignatureHelpProduced> {
    let client = await clientMapper.getOrAddClient(document.uri);
    let sigHelp = await client.signatureHelp(language, document.getText(), position.line, position.character, token);
    return sigHelp;
}
