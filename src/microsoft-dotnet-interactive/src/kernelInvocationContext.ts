// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from "rxjs";
import { tryAddUriToRoutingSlip } from "./connection";
import * as contracts from "./contracts";
import { Disposable } from "./disposables";
import { getKernelUri, Kernel } from "./kernel";
import { PromiseCompletionSource } from "./promiseCompletionSource";


export class KernelInvocationContext implements Disposable {
    public get promise(): void | PromiseLike<void> {
        return this.completionSource.promise;
    }
    private static _current: KernelInvocationContext | null = null;
    private readonly _commandEnvelope: contracts.KernelCommandEnvelope;
    private readonly _childCommands: contracts.KernelCommandEnvelope[] = [];
    private readonly _eventSubject: rxjs.Subject<contracts.KernelEventEnvelope> = new rxjs.Subject<contracts.KernelEventEnvelope>();

    private _isComplete = false;
    private _handlingKernel: Kernel | null = null;

    public get handlingKernel() {
        return this._handlingKernel;
    };

    public get kernelEvents(): rxjs.Observable<contracts.KernelEventEnvelope> {
        return this._eventSubject.asObservable();
    };

    public set handlingKernel(value: Kernel | null) {
        this._handlingKernel = value;
    }

    private completionSource = new PromiseCompletionSource<void>();
    static establish(kernelCommandInvocation: contracts.KernelCommandEnvelope): KernelInvocationContext {
        let current = KernelInvocationContext._current;
        if (!current || current._isComplete) {
            KernelInvocationContext._current = new KernelInvocationContext(kernelCommandInvocation);
        } else {
            if (!areCommandsTheSame(kernelCommandInvocation, current._commandEnvelope)) {
                const found = current._childCommands.includes(kernelCommandInvocation);
                if (!found) {
                    current._childCommands.push(kernelCommandInvocation);

                    const oldSlip = kernelCommandInvocation.routingSlip ?? [];
                    kernelCommandInvocation.routingSlip = [...(current._commandEnvelope.routingSlip ?? [])];
                    for (const uri of oldSlip) {
                        tryAddUriToRoutingSlip(kernelCommandInvocation, uri);
                    }
                }
            }
        }

        return KernelInvocationContext._current!;
    }

    static get current(): KernelInvocationContext | null { return this._current; }
    get command(): contracts.KernelCommand { return this._commandEnvelope.command; }
    get commandEnvelope(): contracts.KernelCommandEnvelope { return this._commandEnvelope; }
    constructor(kernelCommandInvocation: contracts.KernelCommandEnvelope) {
        this._commandEnvelope = kernelCommandInvocation;
    }

    complete(command: contracts.KernelCommandEnvelope) {
        if (areCommandsTheSame(command, this._commandEnvelope)) {
            this._isComplete = true;
            let succeeded: contracts.CommandSucceeded = {};
            let eventEnvelope: contracts.KernelEventEnvelope = {
                command: this._commandEnvelope,
                eventType: contracts.CommandSucceededType,
                event: succeeded
            };
            this.internalPublish(eventEnvelope);
            this.completionSource.resolve();
            // TODO: C# version has completion callbacks - do we need these?
            // if (!_events.IsDisposed)
            // {
            //     _events.OnCompleted();
            // }

        }
        else {
            let pos = this._childCommands.indexOf(command);
            delete this._childCommands[pos];
        }
    }

    fail(message?: string) {
        // TODO:
        // The C# code accepts a message and/or an exception. Do we need to add support
        // for exceptions? (The TS CommandFailed interface doesn't have a place for it right now.)
        this._isComplete = true;
        let failed: contracts.CommandFailed = { message: message ?? "Command Failed" };
        let eventEnvelope: contracts.KernelEventEnvelope = {
            command: this._commandEnvelope,
            eventType: contracts.CommandFailedType,
            event: failed
        };

        this.internalPublish(eventEnvelope);
        this.completionSource.resolve();
    }

    publish(kernelEvent: contracts.KernelEventEnvelope) {
        if (!this._isComplete) {
            this.internalPublish(kernelEvent);
        }
    }

    private internalPublish(kernelEvent: contracts.KernelEventEnvelope) {
        if (!kernelEvent.command) {
            kernelEvent.command = this._commandEnvelope;
        }

        let command = kernelEvent.command;

        if (this.handlingKernel) {
            tryAddUriToRoutingSlip(kernelEvent, getKernelUri(this.handlingKernel));

        } else {
            kernelEvent;//?
        }
        if (command === null ||
            command === undefined ||
            areCommandsTheSame(command!, this._commandEnvelope) ||
            this._childCommands.includes(command!)) {
            this._eventSubject.next(kernelEvent);
        }
    }

    isParentOfCommand(commandEnvelope: contracts.KernelCommandEnvelope): boolean {
        const childFound = this._childCommands.includes(commandEnvelope);
        return childFound;
    }

    dispose() {
        if (!this._isComplete) {
            this.complete(this._commandEnvelope);
        }
        KernelInvocationContext._current = null;
    }
}

export function areCommandsTheSame(envelope1: contracts.KernelCommandEnvelope, envelope2: contracts.KernelCommandEnvelope): boolean {
    const sameReference = envelope1 === envelope2;//?
    const sameCommandType = envelope1?.commandType === envelope2?.commandType;//?
    return sameReference
        || (sameCommandType && envelope1?.token === envelope2?.token && envelope1?.id === envelope2?.id);
}
