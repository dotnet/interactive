// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from "rxjs";
import * as commandsAndEvents from "./commandsAndEvents";
import { Disposable } from "./disposables";
import { getKernelUri, Kernel } from "./kernel";
import { PromiseCompletionSource } from "./promiseCompletionSource";


export class KernelInvocationContext implements Disposable {
    public get promise(): void | PromiseLike<void> {
        return this.completionSource.promise;
    }
    private static _current: KernelInvocationContext | null = null;
    private readonly _commandEnvelope: commandsAndEvents.KernelCommandEnvelope;
    private readonly _childCommands: commandsAndEvents.KernelCommandEnvelope[] = [];
    private readonly _eventSubject: rxjs.Subject<commandsAndEvents.KernelEventEnvelope> = new rxjs.Subject<commandsAndEvents.KernelEventEnvelope>();

    private _isComplete = false;
    private _handlingKernel: Kernel | null = null;

    public get handlingKernel() {
        return this._handlingKernel;
    };

    public get kernelEvents(): rxjs.Observable<commandsAndEvents.KernelEventEnvelope> {
        return this._eventSubject.asObservable();
    };

    public set handlingKernel(value: Kernel | null) {
        this._handlingKernel = value;
    }

    private completionSource = new PromiseCompletionSource<void>();
    static getOrCreateAmbientContext(command: commandsAndEvents.KernelCommandEnvelope): KernelInvocationContext {
        let current = KernelInvocationContext._current;
        if (!current || current._isComplete) {
            command.getOrCreateToken();
            KernelInvocationContext._current = new KernelInvocationContext(command);
        } else {
            if (!commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(command, current._commandEnvelope)) {
                const found = current._childCommands.includes(command);
                if (!found) {
                    if (command.parentCommand === null || command.parentCommand === undefined) {
                        command.setParent(current._commandEnvelope);
                    }
                    current._childCommands.push(command);
                }
            }
        }

        return KernelInvocationContext._current!;
    }

    static get current(): KernelInvocationContext | null { return this._current; }
    get command(): commandsAndEvents.KernelCommand { return this._commandEnvelope.command; }
    get commandEnvelope(): commandsAndEvents.KernelCommandEnvelope { return this._commandEnvelope; }
    constructor(kernelCommandInvocation: commandsAndEvents.KernelCommandEnvelope) {
        this._commandEnvelope = kernelCommandInvocation;
    }

    complete(command: commandsAndEvents.KernelCommandEnvelope) {
        if (commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(command, this._commandEnvelope)) {
            this._isComplete = true;
            let succeeded: commandsAndEvents.CommandSucceeded = {};
            let eventEnvelope: commandsAndEvents.KernelEventEnvelope = new commandsAndEvents.KernelEventEnvelope(
                commandsAndEvents.CommandSucceededType,
                succeeded,
                this._commandEnvelope
            );

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
        let failed: commandsAndEvents.CommandFailed = { message: message ?? "Command Failed" };
        let eventEnvelope: commandsAndEvents.KernelEventEnvelope = new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.CommandFailedType,
            failed,
            this._commandEnvelope
        );

        this.internalPublish(eventEnvelope);
        this.completionSource.resolve();
    }

    publish(kernelEvent: commandsAndEvents.KernelEventEnvelope) {
        if (!this._isComplete) {
            this.internalPublish(kernelEvent);
        }
    }

    private internalPublish(kernelEvent: commandsAndEvents.KernelEventEnvelope) {
        if (!kernelEvent.command) {
            kernelEvent.command = this._commandEnvelope;
        }

        let command = kernelEvent.command;

        if (this.handlingKernel) {
            const kernelUri = getKernelUri(this.handlingKernel);
            if (!kernelEvent.routingSlip.contains(kernelUri)) {
                kernelEvent.routingSlip.stamp(kernelUri);
                kernelEvent.routingSlip;//?
            } else {
                "should not be here";//?
            }

        } else {
            kernelEvent;//?
        }
        this._commandEnvelope;//?
        if (command === null ||
            command === undefined ||
            commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(command!, this._commandEnvelope) ||
            this._childCommands.includes(command!)) {
            this._eventSubject.next(kernelEvent);
        } else if (command.isSelforDescendantOf(this._commandEnvelope)) {
            this._eventSubject.next(kernelEvent);
        } else if (command.hasSameRootCommandAs(this._commandEnvelope)) {
            this._eventSubject.next(kernelEvent);
        }
    }

    isParentOfCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope): boolean {
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
