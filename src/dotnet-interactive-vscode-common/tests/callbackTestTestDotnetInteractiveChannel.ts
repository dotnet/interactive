// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import { IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope, KernelCommandAndEventReceiver, isKernelCommandEnvelope } from '../../src/vscode-common/dotnet-interactive';
import { CommandFailed, CommandFailedType, KernelEventEnvelope } from '../../src/vscode-common/dotnet-interactive/contracts';
import { DotnetInteractiveChannel } from '../../src/vscode-common/DotnetInteractiveChannel';
// executes the given callback for the specified commands
export class CallbackTestTestDotnetInteractiveChannel implements DotnetInteractiveChannel {

    private _receiverSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;

    constructor(readonly fakedCommandCallbacks: { [key: string]: () => KernelEventEnvelope }) {

        this._receiverSubject = new rxjs.Subject<KernelCommandOrEventEnvelope>();

        this.sender = KernelCommandAndEventSender.FromFunction((envelope: KernelCommandOrEventEnvelope) => {
            if (isKernelCommandEnvelope(envelope)) {
                const commandCallback = this.fakedCommandCallbacks[envelope.commandType];
                if (!commandCallback) {
                    throw new Error(`No callback specified for command '${envelope.commandType}'`);
                }
                const eventEnvelope = commandCallback();
                try {
                    this._receiverSubject.next(eventEnvelope);
                } catch (e) {
                    const eventEnvelope: KernelEventEnvelope = {
                        eventType: CommandFailedType,
                        event: <CommandFailed>{
                            message: e
                        },
                        command: envelope,
                    }
                    this._receiverSubject.next(eventEnvelope);
                    throw e;
                }
            }

        });
        this.receiver = KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);
    }

    sender: IKernelCommandAndEventSender;
    receiver: IKernelCommandAndEventReceiver;

    waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    dispose() {
        // noop
    }
}
