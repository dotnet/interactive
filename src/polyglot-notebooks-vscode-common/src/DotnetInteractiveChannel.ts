// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelReady } from "./polyglot-notebooks";
import { IKernelCommandAndEventReceiver, IKernelCommandAndEventSender } from "./polyglot-notebooks/connection";
import { Disposable } from "./polyglot-notebooks/disposables";

export interface KernelCommandAndEventChannel extends Disposable {
    sender: IKernelCommandAndEventSender,
    receiver: IKernelCommandAndEventReceiver
}
export interface DotnetInteractiveChannel extends KernelCommandAndEventChannel {
    waitForReady(): Promise<KernelReady>;
}
