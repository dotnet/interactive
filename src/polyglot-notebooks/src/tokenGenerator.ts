// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommandEnvelope } from "./commandsAndEvents";

export function isSelforDescendantOf(thisCommand: KernelCommandEnvelope, otherCommand: KernelCommandEnvelope) {
    const otherToken = otherCommand.getOrCreateToken();
    const thisToken = thisCommand.getOrCreateToken();
    if (thisToken && otherToken) {
        return thisToken.startsWith(otherToken!);
    }

    throw new Error('both commands must have tokens');
}

export function hasSameRootCommandAs(thisCommand: KernelCommandEnvelope, otherCommand: KernelCommandEnvelope) {
    const otherToken = otherCommand.getOrCreateToken();
    const thisToken = thisCommand.getOrCreateToken();
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