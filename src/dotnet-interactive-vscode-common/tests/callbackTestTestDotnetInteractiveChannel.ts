// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import { IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope, KernelCommandAndEventReceiver, isKernelCommandEnvelope } from '../../src/vscode-common/dotnet-interactive';
import { KernelEventEnvelope } from '../../src/vscode-common/dotnet-interactive/contracts';
import { DotnetInteractiveChannel } from '../../src/vscode-common/DotnetInteractiveChannel';
// executes the given callback for the specified commands
export class CallbackTestTestDotnetInteractiveChannel implements DotnetInteractiveChannel {
    private _senderSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;
    private _receiverSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;

    constructor(readonly fakedCommandCallbacks: { [key: string]: () => KernelEventEnvelope }) {
        this._senderSubject = new rxjs.Subject<KernelCommandOrEventEnvelope>();
        this._receiverSubject = new rxjs.Subject<KernelCommandOrEventEnvelope>();

        this.sender = KernelCommandAndEventSender.FromObserver(this._senderSubject);
        this.receiver = KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);

        this._senderSubject.subscribe({
            next: (envelope) => {
                if (isKernelCommandEnvelope(envelope)) {
                    const commandCallback = this.fakedCommandCallbacks[envelope.commandType];
                    if (!commandCallback) {
                        throw new Error(`No callback specified for command '${envelope.commandType}'`);
                    }

                    const eventEnvelope = commandCallback();
                    this._receiverSubject.next(eventEnvelope);
                }
            }
        });
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
