// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSucceeded, CommandSucceededType, CommandFailed, CommandFailedType, KernelCommandEnvelope, KernelCommand, KernelEventEnvelope, Disposable } from "./contracts";
import { IKernelEventObserver, Kernel } from "./kernel";
import { TokenGenerator } from "./tokenGenerator";


export class KernelInvocationContext implements Disposable {
    private static _current: KernelInvocationContext | null = null;
    private readonly _commandEnvelope: KernelCommandEnvelope;
    private readonly _childCommands: KernelCommandEnvelope[] = [];
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    private readonly _eventObservers: Map<string, IKernelEventObserver> = new Map();
    private _isComplete = false;
    public handlingKernel: Kernel | null = null;
    static establish(kernelCommandInvocation: KernelCommandEnvelope): KernelInvocationContext {
        let current = KernelInvocationContext._current;
        if (!current || current._isComplete) {
            KernelInvocationContext._current = new KernelInvocationContext(kernelCommandInvocation);
        } else {
            current._childCommands.push(kernelCommandInvocation);
        }

        return KernelInvocationContext._current!;
    }

    static get current(): KernelInvocationContext | null { return this._current; }
    get command(): KernelCommand { return this._commandEnvelope.command; }
    get commandEnvelope(): KernelCommandEnvelope { return this._commandEnvelope; }
    constructor(kernelCommandInvocation: KernelCommandEnvelope) {
        this._commandEnvelope = kernelCommandInvocation;
    }

    subscribeToKernelEvents(observer: IKernelEventObserver) {
        let subToken = this._tokenGenerator.GetNewToken();
        this._eventObservers.set(subToken, observer);
        return {
            dispose: () => {
                this._eventObservers.delete(subToken);
            }
        };
    }
    complete(command: KernelCommandEnvelope) {
        if (command === this._commandEnvelope) {
            let succeeded: CommandSucceeded = {};
            let eventEnvelope: KernelEventEnvelope = {
                command: this._commandEnvelope,
                eventType: CommandSucceededType,
                event: succeeded
            };
            this.publish(eventEnvelope);

            // TODO: C# version has completion callbacks - do we need these?
            // if (!_events.IsDisposed)
            // {
            //     _events.OnCompleted();
            // }
            this._isComplete = true;
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
        let failed: CommandFailed = { message: message ?? "Command Failed" };
        let eventEnvelope: KernelEventEnvelope = {
            command: this._commandEnvelope,
            eventType: CommandFailedType,
            event: failed
        };

        this.publish(eventEnvelope);
        this._isComplete = true;
    }

    publish(kernelEvent: KernelEventEnvelope) {
        if (!this._isComplete) {
            let command = kernelEvent.command;
            if (command === null ||
                areCommandsTheSame(command!, this._commandEnvelope) ||
                this._childCommands.includes(command!)) {
                this._eventObservers.forEach((observer) => {
                    observer(kernelEvent);
                });
            }
        }
    }

    dispose() {
        KernelInvocationContext._current = null;
    }
}

export function areCommandsTheSame(envelope1: KernelCommandEnvelope, envelope2: KernelCommandEnvelope): boolean {
    return envelope1 === envelope2
        || (envelope1.commandType === envelope2.commandType && envelope1.token === envelope2.token);
}
