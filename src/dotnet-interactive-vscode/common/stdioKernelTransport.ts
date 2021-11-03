// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import {
    DisposableSubscription,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    KernelTransport,
    DiagnosticLogEntryProducedType,
    DiagnosticLogEntryProduced,
    KernelReadyType,
    KernelCommandEnvelopeHandler,
    KernelCommandEnvelope
} from './interfaces/contracts';
import { ProcessStart } from './interfaces';
import { ReportChannel, Uri } from './interfaces/vscode-like';
import { LineReader } from './lineReader';
import { isNotNull, parse, stringify } from './utilities';
import { isKernelCommandEnvelope, isKernelEventEnvelope } from "./interfaces/utilities";

export class StdioKernelTransport implements KernelTransport {
    private childProcess: cp.ChildProcessWithoutNullStreams | null;
    private lineReader: LineReader;
    private notifyOnExit: boolean = true;
    private readyPromise: Promise<void>;
    private commandHandler: KernelCommandEnvelopeHandler = () => Promise.resolve();
    private eventSubscribers: Array<KernelEventEnvelopeObserver> = [];


    constructor(
        notebookPath: string,
        processStart: ProcessStart,
        private diagnosticChannel: ReportChannel,
        private parseUri: (uri: string) => Uri,
        private notification: { displayError: (message: string) => Promise<void>, displayInfo: (message: string) => Promise<void> },
        private processExited: (pid: number, code: number | undefined, signal: string | undefined) => void) {
        // prepare root event handler
        this.lineReader = new LineReader();
        this.lineReader.subscribe(line => this.handleLine(line));
        this.childProcess = null;

        // prepare one-time ready event
        this.readyPromise = new Promise<void>(async (resolve, reject) => {

            let args = processStart.args;
            // launch the process
            this.diagnosticChannel.appendLine(`Starting kernel for '${notebookPath}' using: ${processStart.command} ${args.join(' ')}`);
            let childProcess = cp.spawn(processStart.command, args, { cwd: processStart.workingDirectory });
            let pid = childProcess.pid;

            this.childProcess = childProcess;
            this.diagnosticChannel.appendLine(`Kernel for '${notebookPath}' started (${childProcess.pid}).`);

            childProcess.on('exit', (code: number, signal: string) => {
                const message = `Kernel for '${notebookPath}' ended (${pid})`;
                const messageCodeSuffix = (code && code !== 0)
                    ? ` with code ${code}`
                    : '';
                const messageSignalSuffix = signal
                    ? ` with signal ${signal}`
                    : '';
                const fullMessage = `${message}${messageCodeSuffix}${messageSignalSuffix}.`;
                this.diagnosticChannel.appendLine(fullMessage);
                if (this.notifyOnExit) {
                    this.processExited(pid, code, signal);
                }
            });

            childProcess.stdout.on('data', data => this.lineReader.onData(data));
            childProcess.stderr.on('data', data => this.diagnosticChannel.appendLine(`kernel (${pid}) stderr: ${data.toString('utf-8')}`));

            const readySubscriber = this.subscribeToKernelEvents(eventEnvelope => {
                if (eventEnvelope.eventType === KernelReadyType) {
                    readySubscriber.dispose();
                    resolve();
                }
            });
        });
    }

    private async handleLine(line: string): Promise<void> {
        try {
            const garbage = parse(line);
        }
        catch (e) {
            const x = e;
        }
        const envelope = parse(line);
        if (isKernelEventEnvelope(envelope)) {
            switch (envelope.eventType) {
                case DiagnosticLogEntryProducedType:
                    this.diagnosticChannel.appendLine((<DiagnosticLogEntryProduced>envelope.event).message);
                    break;
            }

            for (let i = this.eventSubscribers.length - 1; i >= 0; i--) {
                this.eventSubscribers[i](envelope);
            }
        } else if (isKernelCommandEnvelope(envelope)) {
            // TODO: pass in context with shortcut methods for publish, etc.
            // TODO: wrap and return succeed/failed
            // TODO: publish succeeded
            // TODO: publish failed
            await this.commandHandler(envelope);
        }
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.eventSubscribers.push(observer);
        return {
            dispose: () => {
                const i = this.eventSubscribers.indexOf(observer);
                if (i >= 0) {
                    this.eventSubscribers.splice(i, 1);
                }
            }
        };
    }

    setCommandHandler(handler: KernelCommandEnvelopeHandler) {
        this.commandHandler = handler;
    }

    submitCommand(commandEnvelope: KernelCommandEnvelope): Promise<void> {
        return this.submit(commandEnvelope);
    }

    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void> {
        return this.submit(eventEnvelope);
    }

    private submit(content: KernelEventEnvelope | KernelCommandEnvelope): Promise<void> {
        return new Promise((resolve, reject) => {
            let str = stringify(content);
            if (isNotNull(this.childProcess)) {
                try {
                    this.childProcess.stdin.write(str);
                    this.childProcess.stdin.write('\n');
                    resolve();
                } catch (e) {
                    reject(e);
                }
            }
            else {
                reject('Kernel process is `null`.');
            }
        });
    }

    waitForReady(): Promise<void> {
        return this.readyPromise;
    }

    dispose() {
        this.notifyOnExit = false;
        if (this.childProcess) {
            this.childProcess.kill();
        }
    }
}
