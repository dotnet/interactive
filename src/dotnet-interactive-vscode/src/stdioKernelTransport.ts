// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import { DisposableSubscription, KernelCommand, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver, DiagnosticLogEntryProducedType, DiagnosticLogEntryProduced } from "./contracts";
import { ProcessStart } from './interfaces';
import { ReportChannel } from './interfaces/vscode';

export class StdioKernelTransport {
    private buffer: string = '';
    private childProcess: cp.ChildProcessWithoutNullStreams;
    private subscribers: Array<KernelEventEnvelopeObserver> = [];

    constructor(processStart: ProcessStart, private diagnosticChannel: ReportChannel) {
        this.childProcess = cp.spawn(processStart.command, processStart.args, { cwd: processStart.workingDirectory });
        this.diagnosticChannel.appendLine(`Kernel started with pid ${this.childProcess.pid}.`);
        this.childProcess.on('exit', (code: number, _signal: string) => {
            let message = `Kernel pid ${this.childProcess.pid} ended`;
            let messageSuffix = (code && code !== 0)
                ? ` with code ${code}`
                : '';
            this.diagnosticChannel.appendLine(message + messageSuffix);
        });
        this.childProcess.stdout.on('data', (data) => {
            let str: string = data.toString();
            this.buffer += str;

            let i = this.buffer.indexOf('\n');
            while (i >= 0) {
                let temp = this.buffer.substr(0, i + 1);
                this.buffer = this.buffer.substr(i + 1);
                i = this.buffer.indexOf('\n');
                try {
                    let obj = JSON.parse(temp);
                    let envelope = <KernelEventEnvelope>obj;
                    switch (envelope.eventType){
                        case DiagnosticLogEntryProducedType:
                            this.diagnosticChannel.appendLine((<DiagnosticLogEntryProduced>envelope.event).message);
                            break;
                    }
                    for (let i = this.subscribers.length - 1; i >= 0; i--) {
                        this.subscribers[i](envelope);
                    }
                } catch {
                }
            }
        });
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.subscribers.push(observer);
        return {
            dispose: () => {
                let i = this.subscribers.indexOf(observer);
                if (i >= 0) {
                    this.subscribers.splice(i, 1);
                }
            }
        };
    }

    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {
        return new Promise((resolve, reject) => {
            let submit = {
                token,
                commandType,
                command
            };

            let str = JSON.stringify(submit);
            this.childProcess.stdin.write(str);
            this.childProcess.stdin.write('\n');
            resolve();
        });
    }

    dispose() {
        this.childProcess.kill();
    }
}
