// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSucceeded, CommandSucceededType, CommandFailed, CommandFailedType, KernelCommand, KernelEvent } from "./contracts";
import { IKernelEventObserver, KernelInvocationContext } from "./dotnet-interactive-interfaces";
import { TokenGenerator } from "./tokenGenerator";


export class ClientSideKernelInvocationContext implements KernelInvocationContext {
    private static _current: ClientSideKernelInvocationContext = null;
    private readonly _command: KernelCommand;
    private readonly _commandType: string;
    private readonly _childCommands: KernelCommand[] = [];
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    private readonly _eventObservers: { [token: string]: IKernelEventObserver} = {};
    private _isComplete = false;

    static establish(kernelCommandInvocation: { command: KernelCommand, commandType: string }): KernelInvocationContext {
        let current = ClientSideKernelInvocationContext._current;
        if (current === null || current._isComplete) {
            ClientSideKernelInvocationContext._current = new ClientSideKernelInvocationContext(kernelCommandInvocation);
        } else {
            current._childCommands.push(kernelCommandInvocation.command);
        }

        return ClientSideKernelInvocationContext._current;
    }

    static get current(): KernelInvocationContext { return this._current; }
    get command(): KernelCommand { return this._command; }

    constructor(kernelCommandInvocation: { command: KernelCommand, commandType: string }) {
        this._command = kernelCommandInvocation.command;
        this._commandType  = kernelCommandInvocation.commandType;
    }

    subscribeToKernelEvents(observer: IKernelEventObserver) {
        let subToken = this._tokenGenerator.GetNewToken();
        this._eventObservers[subToken] = observer;
        return {
            dispose: () => { delete this._eventObservers[subToken]; }
        };
    }
    complete(command: KernelCommand) {
        if (command === this._command) {
            let succeeded: CommandSucceeded = {};
            let succeededDetail = {
                command: this._command,
                commandType: this._commandType,
                eventType: CommandSucceededType,
                event: succeeded
            };
            this.publish(succeededDetail);

            // TODO: C# version has completion callbacks - do we need these?
            // if (!_events.IsDisposed)
            // {
            //     _events.OnCompleted();
            // }
            this._isComplete = true;
        }
        else
        {
            let pos = this._childCommands.indexOf(command);
            delete this._childCommands[pos];
        }
    }

    fail(message?: string) {
        // TODO:
        // The C# code accepts a message and/or an exception. Do we need to add support
        // for exceptions? (The TS CommandFailed interface doesn't have a place for it right now.)
        let failed: CommandFailed = { message };
        let failedDetail = {
            command: this._command,
            commandType: this._commandType,
            eventType: CommandFailedType,
            event: failed
        };
        this.publish(failedDetail);

        this._isComplete = true;
    }

    publish(kernelEvent: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }) {
        if (!this._isComplete) {
            let command = kernelEvent.command;
            if (command === null ||
                command === this._command ||
                this._childCommands.includes(command)) {
                let keys = Object.keys(this._eventObservers);
                for (let subToken of keys) {
                    let observer = this._eventObservers[subToken];
                    observer(kernelEvent);
                }
            }
        }
    }

    dispose() {
        ClientSideKernelInvocationContext._current = null;
    }
}