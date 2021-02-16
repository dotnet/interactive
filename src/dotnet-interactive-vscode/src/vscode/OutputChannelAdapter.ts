// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ReportChannel } from 'vscode-interfaces/out/notebook';

export class OutputChannelAdapter implements ReportChannel {

    constructor(private channel: vscode.OutputChannel) {
    }

    getName(): string {
        return this.clear.name;
    }

    append(value: string): void {
        this.channel.append(value);
    }

    appendLine(value: string): void {
        this.channel.appendLine(value);
    }

    clear(): void {
        this.channel.clear();
    }

    show(): void {
        this.channel.show(true);
    }

    hide(): void {
        this.channel.hide();
    }
}
