// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelEventEnvelopeObserver, KernelCommandEnvelope, CommandSucceeded, CommandSucceededType, KernelEventEnvelope, CommandFailed, CommandFailedType } from "./contracts";
import { KernelInvocationContext } from "./dotnet-interactive-interfaces";
import { TokenGenerator } from "./tokenGenerator";


export class ClientSideKernelInvocationContext implements KernelInvocationContext {
    private static _current: ClientSideKernelInvocationContext = null;
    private readonly _command: KernelCommandEnvelope;
    private readonly _childCommands: KernelCommandEnvelope[] = [];
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    private readonly _eventSubscribers: { [token: string]: KernelEventEnvelopeObserver} = {};
    private _isComplete = false;

    static establish(commandEnvelope: KernelCommandEnvelope): KernelInvocationContext {
        let current = ClientSideKernelInvocationContext._current;
        if (current === null || current._isComplete) {
            ClientSideKernelInvocationContext._current = new ClientSideKernelInvocationContext(commandEnvelope);
        } else {
            current._childCommands.push(commandEnvelope);
        }

        return ClientSideKernelInvocationContext._current;
    }

    static get current(): KernelInvocationContext { return this._current; }
    get command(): KernelCommandEnvelope { return this._command; }

    constructor(commandEnvelope: KernelCommandEnvelope) {
        this._command = commandEnvelope;
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver) {
        let subToken = this._tokenGenerator.GetNewToken();
        this._eventSubscribers[subToken] = observer;
        return {
            dispose: () => { delete this._eventSubscribers[subToken]; }
        };
    }
    complete(commandEnvelope: KernelCommandEnvelope) {
        if (commandEnvelope === this._command) {
            let succeeded: CommandSucceeded = {};
            let succeededEnvelope: KernelEventEnvelope = {
                command: this._command,
                eventType: CommandSucceededType,
                event: succeeded
            };
            this.publish(succeededEnvelope);

            // TODO: C# version has completion callbacks - do we need these?
            // if (!_events.IsDisposed)
            // {
            //     _events.OnCompleted();
            // }
            this._isComplete = true;
        }
        else
        {
            let pos = this._childCommands.indexOf(commandEnvelope);
            delete this._childCommands[pos];
        }
    }

    fail(message?: string) {
        // TODO: exception?
        let failed: CommandFailed = { message };
        let failedEnvelope: KernelEventEnvelope = {
            command: this._command,
            eventType: CommandFailedType,
            event: failed
        };
        this.publish(failedEnvelope);

        this._isComplete = true;
    }

    publish(eventEnvelope: KernelEventEnvelope) {
        if (!this._isComplete) {
            let command = eventEnvelope.command;
            if (command === null ||
                command === this._command ||
                this._childCommands.includes(command)) {
                let keys = Object.keys(this._eventSubscribers);
                for (let subToken of keys) {
                    let observer = this._eventSubscribers[subToken];
                    observer(eventEnvelope);
                }
            }
        }
    }

    dispose() {
        ClientSideKernelInvocationContext._current = null;
    }
}