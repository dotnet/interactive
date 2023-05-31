// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import {
    CommandFailedType,
    CommandSucceededType,
    KernelEventEnvelopeObserver,
    KernelReadyType,
    SubmitCodeType,
    KernelReady
} from './polyglot-notebooks/commandsAndEvents';
import { ProcessStart } from './interfaces';
import { ReportChannel } from './interfaces/vscode-like';
import { LineReader } from './lineReader';
import { isNotNull, parse, stringify } from './utilities';
import { DotnetInteractiveChannel } from './DotnetInteractiveChannel';
import {
    IKernelCommandAndEventReceiver,
    IKernelCommandAndEventSender,
    isKernelCommandEnvelope,
    isKernelEventEnvelope,
    KernelCommandAndEventReceiver,
    KernelCommandAndEventSender,
    KernelCommandOrEventEnvelope
} from './polyglot-notebooks/connection';
import { DisposableSubscription } from './polyglot-notebooks/disposables';
import { Subject } from 'rxjs';
import { Logger } from './polyglot-notebooks/logger';

export class StdioDotnetInteractiveChannel implements DotnetInteractiveChannel {
    private childProcess: cp.ChildProcessWithoutNullStreams | null;
    private lineReader: LineReader;
    private notifyOnExit: boolean = true;
    private readyPromise: Promise<KernelReady>;
    private pingTimer: NodeJS.Timer | null = null;
    private _receiverSubject: Subject<KernelCommandOrEventEnvelope>;
    private _sender: IKernelCommandAndEventSender;
    private _receiver: IKernelCommandAndEventReceiver;

    constructor(
        notebookPath: string,
        processStart: ProcessStart,
        private diagnosticChannel: ReportChannel,
        private processExited: (pid: number, code: number | undefined, signal: string | undefined) => void) {


        this._receiverSubject = new Subject<KernelCommandOrEventEnvelope>();

        this._sender = KernelCommandAndEventSender.FromFunction(envelope => {
            this.writeToProcessStdin(envelope);
        });

        this._receiver = KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);

        // prepare root event handler
        this.lineReader = new LineReader();
        this.lineReader.subscribe(line => this.handleLine(line));
        this.childProcess = null;

        // prepare one-time ready event
        this.readyPromise = new Promise<KernelReady>(async (resolve, reject) => {

            let args = processStart.args;
            // launch the process
            this.diagnosticChannel.appendLine(`Starting kernel for '${notebookPath}' using: ${processStart.command} ${args.join(' ')}`);
            const processEnv = { ...process.env, ...processStart.env };
            let childProcess = cp.spawn(processStart.command, args, { cwd: processStart.workingDirectory, env: processEnv });
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
                    resolve(<KernelReady>(eventEnvelope.event));
                }
            });
        });
    }

    public get sender() {
        return this._sender;
    }

    public get receiver() {
        return this._receiver;
    }

    private async handleLine(line: string): Promise<void> {
        try {
            const garbage = parse(line);
        }
        catch (e) {
            const x = e;
        }
        const envelope = parse(line);
        Logger.default.info(`envelope received from stdio: ${JSON.stringify(envelope)}`);
        if (isKernelEventEnvelope(envelope)) {
            switch (envelope.eventType) {
                case CommandFailedType:
                case CommandSucceededType:
                    if (this.pingTimer) {
                        clearInterval(this.pingTimer);
                        this.pingTimer = null;
                    }
                    break;
            }
            this._receiverSubject.next(envelope);

        } else if (isKernelCommandEnvelope(envelope)) {
            // TODO: pass in context with shortcut methods for publish, etc.
            // TODO: wrap and return succeed/failed
            // TODO: publish succeeded
            // TODO: publish failed
            this._receiverSubject.next(envelope);
        }
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        let sub = this._receiverSubject.subscribe({
            next: (envelope: KernelCommandOrEventEnvelope) => {
                if (isKernelEventEnvelope(envelope)) {
                    observer(envelope);
                }
            }
        });
        return {
            dispose: () => {
                sub.unsubscribe();
            }
        };
    }

    private writeToProcessStdin(content: KernelCommandOrEventEnvelope) {
        if (isNotNull(this.childProcess)) {
            let str = stringify(content);
            // send the command or event
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
            // To work around this, only when starting a `SubmitCode` command and only on Windows we periodically
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

        }
        else {
            throw new Error('Kernel process is `null`.');
        }
    }

    waitForReady(): Promise<KernelReady> {
        return this.readyPromise;
    }

    dispose() {
        this.notifyOnExit = false;
        if (this.pingTimer) {
            clearInterval(this.pingTimer);
            this.pingTimer = null;
        }

        this.childProcess = null;
    }
}
