"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.RecordingChannel = void 0;
class RecordingChannel {
    constructor(channelName) {
        this.channelText = "";
        this.name = channelName || "testChannel";
    }
    getName() {
        return this.name;
    }
    append(value) {
        this.channelText = `${this.channelText}${value}`;
    }
    appendLine(value) {
        this.channelText = `${this.channelText}${value}\n`;
    }
    clear() {
        this.channelText = "";
    }
    show() {
    }
    hide() {
    }
}
exports.RecordingChannel = RecordingChannel;
//# sourceMappingURL=RecordingOutputChannel.js.map