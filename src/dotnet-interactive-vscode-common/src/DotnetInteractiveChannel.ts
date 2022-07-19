// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IKernelCommandAndEventReceiver, IKernelCommandAndEventSender } from "./dotnet-interactive/connection";
import { Disposable } from "./dotnet-interactive/disposables";

export interface KernelCommandAndEventChannel extends Disposable {

    sender: IKernelCommandAndEventSender,
    receiver: IKernelCommandAndEventReceiver
}
export interface DotnetInteractiveChannel extends KernelCommandAndEventChannel {
    waitForReady(): Promise<void>;
}
