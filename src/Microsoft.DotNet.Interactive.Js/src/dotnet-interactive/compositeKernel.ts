// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DisposableSubscription, KernelEventEnvelopeObserver, KernelCommandEnvelope } from "../common/interfaces/contracts";
import { IKernelCommandHandler, Kernel } from "../common/interfaces/kernel";

export class CompositeKernel implements Kernel {
    readonly name: string;
    readonly childKernels: Kernel[];
    private readonly _kernelMap: { [key: string]: Kernel } = {}
    constructor(name?: string) {
        this.name = name ?? "client-side-composite-kernel";
        this.childKernels = [];
    }

    add(kernel: Kernel, aliases?: string[]) {
        if (!kernel) {
            throw new Error("kernel cannot be null or undefined")
        }

        this.childKernels.push(kernel);
        this._kernelMap[kernel.name] = kernel;
        if (aliases) {
            aliases.forEach(alias => {
                this._kernelMap[alias] = kernel;
            });
        }
    }

    findKernelByName(kernelName:string) : Kernel| undefined{
        return this._kernelMap[kernelName];
    }

    send(commandEnvelope: KernelCommandEnvelope): Promise<void> {
        throw new Error("Method not implemented.");
    }
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        throw new Error("Method not implemented.");
    }
    registerCommandHandler(handler: IKernelCommandHandler): void {
        throw new Error("Method not implemented.");
    }
}