// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "../interfaces/contracts";
import { Kernel } from "./kernel";

export class CompositeKernel extends Kernel {

    private readonly _kernelMap: Map<string, Kernel> = new Map();
    defaultKernelName: string | undefined;

    constructor(name: string) {
        super(name);
    }

    get childKernels() {
        return Array.from(this._kernelMap.values());
    }

    add(kernel: Kernel, aliases?: string[]) {
        if (!kernel) {
            throw new Error("kernel cannot be null or undefined");
        }

        kernel.parentKernel = this;
        kernel.rootKernel = this.rootKernel;

        this._kernelMap.set(kernel.name, kernel);
        if (aliases) {
            aliases.forEach(alias => {
                this._kernelMap.set(alias, kernel);
            });
        }
    }

    findKernelByName(kernelName: string): Kernel | undefined {
        return this._kernelMap.get(kernelName);
    }
    handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {

        let kernel = commandEnvelope.command.targetKernelName === this.name
            ? this
            : this.getTargetKernel(commandEnvelope.command);

        if (kernel) {

            return kernel.handleCommand(commandEnvelope);
        }

        return Promise.reject(new Error("Kernel not found: " + commandEnvelope.command.targetKernelName));
    }

    getTargetKernel(command: contracts.KernelCommand): Kernel | undefined {
        let targetKernelName = command.targetKernelName ?? this.defaultKernelName;

        let kernel = targetKernelName === undefined ? this : this.findKernelByName(targetKernelName);
        return kernel;
    }
}