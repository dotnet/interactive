"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.OutputChannelAdapter = void 0;
class OutputChannelAdapter {
    constructor(channel) {
        this.channel = channel;
    }
    getName() {
        return this.channel.name;
    }
    append(value) {
        this.channel.append(value);
    }
    appendLine(value) {
        this.channel.appendLine(value);
    }
    clear() {
        this.channel.clear();
    }
    show() {
        this.channel.show(true);
    }
    hide() {
        this.channel.hide();
    }
}
exports.OutputChannelAdapter = OutputChannelAdapter;
//# sourceMappingURL=OutputChannelAdapter.js.map