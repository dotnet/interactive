// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import {
    DisposableSubscription,
    KernelCommand,
    KernelCommandType,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    DiagnosticLogEntryProducedType,
    DiagnosticLogEntryProduced,
    KernelReadyType
} from "./contracts";
import { ProcessStart } from './interfaces';
import { ReportChannel } from './interfaces/vscode';
import { LineReader } from './lineReader';

export class StdioKernelTransport {
    private childProcess: cp.ChildProcessWithoutNullStreams;
    private lineReader: LineReader;
    private readyPromise: Promise<void>;
    private subscribers: Array<KernelEventEnvelopeObserver> = [];

    constructor(processStart: ProcessStart, private diagnosticChannel: ReportChannel) {
        // prepare root event handler
        this.lineReader = new LineReader();
        this.lineReader.subscribe(line => this.handleLine(line));

        // prepare one-time ready event
        this.readyPromise = new Promise<void>((resolve, reject) => {
            const readySubscriber = this.subscribeToKernelEvents(eventEnvelope => {
                if (eventEnvelope.eventType === KernelReadyType) {
                    readySubscriber.dispose();
                    resolve();
                }
            });
        });

        // launch the process
        this.childProcess = cp.spawn(processStart.command, processStart.args, { cwd: processStart.workingDirectory });
        this.diagnosticChannel.appendLine(`Kernel started with pid ${this.childProcess.pid}.`);
        this.childProcess.on('exit', (code: number, _signal: string) => {
            let message = `Kernel pid ${this.childProcess.pid} ended`;
            let messageSuffix = (code && code !== 0)
                ? ` with code ${code}`
                : '';
            this.diagnosticChannel.appendLine(message + messageSuffix);
        });
        this.childProcess.stdout.on('data', data => this.lineReader.onData(data));
    }

    private handleLine(line: string) {
        let obj = JSON.parse(line);
        let envelope = <KernelEventEnvelope>obj;
        switch (envelope.eventType) {
            case DiagnosticLogEntryProducedType:
                this.diagnosticChannel.appendLine((<DiagnosticLogEntryProduced>envelope.event).message);
                break;
        }

        for (let i = this.subscribers.length - 1; i >= 0; i--) {
            this.subscribers[i](envelope);
        }
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

    waitForReady(): Promise<void> {
        return this.readyPromise;
    }

    dispose() {
        this.childProcess.kill();
    }
}
