// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommandEnvelope } from "./contracts";
import * as uuid from "uuid";

export class TokenGenerator {
    private _counter: number = 0;
    public createToken(parentCommand?: KernelCommandEnvelope): string {
        if (parentCommand) {
            if (!parentCommand.token) {
                parentCommand.token = this.createToken();
            }

            return `${parentCommand.token}.${this._counter++}`;
        }
        else {
            const guidBytes = uuid.parse(uuid.v4());
            const data = new Uint8Array(guidBytes);
            const buffer = Buffer.from(data.buffer);
            return buffer.toString('base64');
        }
    }

    public createId(): string {
        return uuid.v4();
    }
}

export function isSelforDescendantOf(thicCommand: KernelCommandEnvelope, otherCommand: KernelCommandEnvelope) {
    const otherToken = otherCommand.token;
    const thisToken = thicCommand.token;
    if (thisToken && otherToken) {
        return thisToken.startsWith(otherToken!);
    }

    throw new Error('both commands must have tokens');
}

export function hasSameRootCommandAs(thicCommand: KernelCommandEnvelope, otherCommand: KernelCommandEnvelope) {
    const otherToken = otherCommand.token;
    const thisToken = thicCommand.token;
    if (thisToken && otherToken) {
        const otherRootToken = getRootToken(otherToken);
        const thisRootToken = getRootToken(thisToken);
        return thisRootToken === otherRootToken;
    }
    throw new Error('both commands must have tokens');
}

export function getRootToken(token: string): string {
    const parts = token.split('.');
    return parts[0];
}