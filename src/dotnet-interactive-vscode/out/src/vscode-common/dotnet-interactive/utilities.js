"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.isKernelCommandEnvelope = exports.isKernelEventEnvelope = void 0;
function isKernelEventEnvelope(obj) {
    return obj.eventType
        && obj.event;
}
exports.isKernelEventEnvelope = isKernelEventEnvelope;
function isKernelCommandEnvelope(obj) {
    return obj.commandType
        && obj.command;
}
exports.isKernelCommandEnvelope = isKernelCommandEnvelope;
//# sourceMappingURL=utilities.js.map