// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './contracts';

export function isKernelEventEnvelope(obj: any): obj is contracts.KernelEventEnvelope {
    return obj.eventType
        && obj.event;
}

export function isKernelCommandEnvelope(obj: any): obj is contracts.KernelCommandEnvelope {
    return obj.commandType
        && obj.command;
}
