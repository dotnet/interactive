// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import {
    CommandFailedType,
    CommandSucceededType,
    DisposableSubscription,
    DiagnosticLogEntryProducedType,
    DiagnosticLogEntryProduced,
    KernelCommandEnvelopeHandler,
    KernelCommandEnvelope,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    KernelReadyType,
    SubmitCodeType
} from './dotnet-interactive/contracts';
import { ProcessStart } from './interfaces';
import { ReportChannel } from './interfaces/vscode-like';
import { LineReader } from './lineReader';
import { isNotNull, parse, stringify } from './utilities';
import { isKernelCommandEnvelope, isKernelEventEnvelope } from "./dotnet-interactive/utilities";
import { DotnetInteractiveChannel } from './DotnetInteractiveChannel';

export class StdioDotnetInteractiveChannel implements DotnetInteractiveChannel {
    private childProcess: cp.ChildProcessWithoutNullStreams | null;
    private lineReader: LineReader;
    private notifyOnExit: boolean = true;
    private readyPromise: Promise<void>;
    private commandHandler: KernelCommandEnvelopeHandler = () => Promise.resolve();
    private eventSubscribers: Array<KernelEventEnvelopeObserver> = [];
    private pingTimer: NodeJS.Timer | null = null;

    constructor(
        notebookPath: string,
        processStart: ProcessStart,
        private diagnosticChannel: ReportChannel,
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
                    this.processExited(pid!, code, signal);
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
                case CommandFailedType:
                case CommandSucceededType:
                    if (this.pingTimer) {
                        clearInterval(this.pingTimer);
                        this.pingTimer = null;
                    }
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
                    // send the command
                    this.childProcess.stdin.write(str + '\n');

                    // Start ping the timer
                    // ====================
                    // On Windows in some cases the STDIN reader on the server side will appear to "hang" when trying
                    // to call `TextReader.ReadLineAsync()` on some long-running commands and this is ultimately by
                    // design.
                    //
                    // https://docs.microsoft.com/en-us/dotnet/api/system.console.in?view=net-6.0#remarks
                    //
                    // > Read operations on the standard input stream execute synchronously. That is, they block until
                    // > the specified read operation has completed. This is true even if an asynchronous method, such
                    // > as ReadLineAsync, is called on the TextReader object returned by the In property.
                    //
                    // This behavior is also present in the native `PeekConsoleInput` and `PeekConsoleInputW` functions
                    // so we can't solve this by using pinvoke.
                    //
                    // To work around this, only when starting a submission command and only on Windows we periodically
                    // ping the server with a single newline character.  This is enough to break the read hang, but
                    // doesn't negatively impact the command parsing or execution.  To prevent spamming the server,
                    // this timer is only active until the command succeeds or fails.
                    if (process.platform === 'win32' && this.pingTimer === null && isKernelCommandEnvelope(content)) {
                        if (content.commandType === SubmitCodeType) {
                            this.pingTimer = setInterval(() => {
                                if (this.childProcess) {
                                    this.childProcess.stdin.write('\n');
                                }
                            }, 500);
                        }
                    }

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
        if (this.pingTimer) {
            clearInterval(this.pingTimer);
            this.pingTimer = null;
        }
        if (this.childProcess) {
            this.childProcess.kill();
        }
    }
}
