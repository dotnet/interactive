// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReportChannel } from "vscode-interfaces/out/notebook";

export class RecordingChannel implements ReportChannel {
    private name: string;
    constructor(channelName?: string) {
        this.name = channelName || "testChannel";
    }
    getName(): string {
        return this.name;
    }
    channelText: string = "";
    append(value: string): void {
        this.channelText = `${this.channelText}${value}`;
    }
    appendLine(value: string): void {
        this.channelText = `${this.channelText}${value}\n`;
    }
    clear(): void {
        this.channelText = "";
    }
    show(): void {
    }
    hide(): void {
    }
}
