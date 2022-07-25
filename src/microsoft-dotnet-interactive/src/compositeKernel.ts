// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { tryAddUriToRoutingSlip } from "./connection";
import * as contracts from "./contracts";
import { getKernelUri, IKernelCommandInvocation, Kernel, KernelType } from "./kernel";
import { KernelHost } from "./kernelHost";
import { KernelInvocationContext } from "./kernelInvocationContext";

export class CompositeKernel extends Kernel {
    private _host: KernelHost | null = null;
    private readonly _defaultKernelNamesByCommandType: Map<contracts.KernelCommandType, string> = new Map();

    defaultKernelName: string | undefined;
    private _childKernels: KernelCollection;

    constructor(name: string) {
        super(name);
        this.kernelType = KernelType.composite;
        this._childKernels = new KernelCollection(this);
    }

    get childKernels() {
        return Array.from(this._childKernels);
    }

    get host(): KernelHost | null {
        return this._host;
    }

    set host(host: KernelHost | null) {
        this._host = host;
        if (this._host) {
            this.kernelInfo.uri = this._host.uri;
            this._childKernels.notifyThatHostWasSet();
        }
    }

    protected override async handleRequestKernelInfo(invocation: IKernelCommandInvocation): Promise<void> {
        for (let kernel of this._childKernels) {
            if (kernel.supportsCommand(invocation.commandEnvelope.commandType)) {
                await kernel.handleCommand({ command: {}, commandType: contracts.RequestKernelInfoType });
            }
        }
    }

    add(kernel: Kernel, aliases?: string[]) {
        if (!kernel) {
            throw new Error("kernel cannot be null or undefined");
        }

        if (!this.defaultKernelName) {
            // default to first kernel
            this.defaultKernelName = kernel.name;
        }

        kernel.parentKernel = this;
        kernel.rootKernel = this.rootKernel;
        kernel.kernelEvents.subscribe({
            next: (event) => {
                event;//?
                tryAddUriToRoutingSlip(event, getKernelUri(this));
                event;//?
                this.publishEvent(event);
            }
        });

        if (aliases) {
            let set = new Set(aliases);

            if (kernel.kernelInfo.aliases) {
                for (let alias in kernel.kernelInfo.aliases) {
                    set.add(alias);
                }
            }

            kernel.kernelInfo.aliases = Array.from(set);
        }

        this._childKernels.add(kernel, aliases);
    }

    setDefaultTargetKernelNameForCommand(commandType: contracts.KernelCommandType, kernelName: string) {
        this._defaultKernelNamesByCommandType.set(commandType, kernelName);
    }
    override handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        const invocationContext = KernelInvocationContext.current;

        let kernel = commandEnvelope.command.targetKernelName === this.name
            ? this
            : this.getHandlingKernel(commandEnvelope, invocationContext);


        const previusoHandlingKernel = invocationContext?.handlingKernel ?? null;

        if (kernel === this) {
            if (invocationContext !== null) {
                invocationContext.handlingKernel = kernel;
            }
            return super.handleCommand(commandEnvelope).finally(() => {
                if (invocationContext !== null) {
                    invocationContext.handlingKernel = previusoHandlingKernel;
                }
            });
        } else if (kernel) {
            if (invocationContext !== null) {
                invocationContext.handlingKernel = kernel;
            }
            tryAddUriToRoutingSlip(commandEnvelope, getKernelUri(kernel));
            return kernel.handleCommand(commandEnvelope).finally(() => {
                if (invocationContext !== null) {
                    invocationContext.handlingKernel = previusoHandlingKernel;
                }
            });
        }

        if (invocationContext !== null) {
            invocationContext.handlingKernel = previusoHandlingKernel;
        }
        return Promise.reject(new Error("Kernel not found: " + commandEnvelope.command.targetKernelName));
    }

    override getHandlingKernel(commandEnvelope: contracts.KernelCommandEnvelope, context?: KernelInvocationContext | null): Kernel | null {

        let kernel: Kernel | null = null;
        if (commandEnvelope.command.destinationUri) {
            kernel = this._childKernels.tryGetByUri(commandEnvelope.command.destinationUri) ?? null;
            if (kernel) {
                return kernel;
            }
        }
        let targetKernelName = commandEnvelope.command.targetKernelName;

        if (targetKernelName === undefined || targetKernelName === null) {
            if (this.canHandle(commandEnvelope)) {
                return this;
            }

            targetKernelName = this._defaultKernelNamesByCommandType.get(commandEnvelope.commandType) ?? this.defaultKernelName;
        }

        if (targetKernelName !== undefined && targetKernelName !== null) {
            kernel = this._childKernels.tryGetByAlias(targetKernelName) ?? null;
        }

        if (!kernel) {
            if (this._childKernels.count === 1) {
                kernel = this._childKernels.single() ?? null;
            }
        }

        if (!kernel) {
            kernel = context?.handlingKernel ?? null;
        }
        return kernel ?? this;

    }
}

class KernelCollection implements Iterable<Kernel> {

    private _compositeKernel: CompositeKernel;
    private _kernels: Kernel[] = [];
    private _nameAndAliasesByKernel: Map<Kernel, Set<string>> = new Map<Kernel, Set<string>>();
    private _kernelsByNameOrAlias: Map<string, Kernel> = new Map<string, Kernel>();
    private _kernelsByLocalUri: Map<string, Kernel> = new Map<string, Kernel>();
    private _kernelsByRemoteUri: Map<string, Kernel> = new Map<string, Kernel>();

    constructor(compositeKernel: CompositeKernel) {
        this._compositeKernel = compositeKernel;
    }

    [Symbol.iterator](): Iterator<Kernel> {
        let counter = 0;
        return {
            next: () => {
                return {
                    value: this._kernels[counter++],
                    done: counter > this._kernels.length //?
                };
            }
        };
    }

    single(): Kernel | undefined {
        return this._kernels.length === 1 ? this._kernels[0] : undefined;
    }


    public add(kernel: Kernel, aliases?: string[]): void {
        this.updateKernelInfoAndIndex(kernel, aliases);
        this._kernels.push(kernel);
    }


    get count(): number {
        return this._kernels.length;
    }

    updateKernelInfoAndIndex(kernel: Kernel, aliases?: string[]): void {
        if (!this._nameAndAliasesByKernel.has(kernel)) {

            let set = new Set<string>();

            for (let alias of kernel.kernelInfo.aliases) {
                set.add(alias);
            }

            kernel.kernelInfo.aliases = Array.from(set);

            set.add(kernel.kernelInfo.localName);

            this._nameAndAliasesByKernel.set(kernel, set);
        }
        if (aliases) {
            for (let alias of aliases) {
                this._nameAndAliasesByKernel.get(kernel)!.add(alias);
            }
        }

        this._nameAndAliasesByKernel.get(kernel)?.forEach(alias => {
            this._kernelsByNameOrAlias.set(alias, kernel);
        });

        if (this._compositeKernel.host) {
            kernel.kernelInfo.uri = `${this._compositeKernel.host.uri}/${kernel.name}`;//?
            this._kernelsByLocalUri.set(kernel.kernelInfo.uri, kernel);
        }

        if (kernel.kernelType === KernelType.proxy) {
            this._kernelsByRemoteUri.set(kernel.kernelInfo.remoteUri!, kernel);
        }
    }

    public tryGetByAlias(alias: string): Kernel | undefined {
        return this._kernelsByNameOrAlias.get(alias);
    }

    public tryGetByUri(uri: string): Kernel | undefined {
        let kernel = this._kernelsByLocalUri.get(uri) || this._kernelsByRemoteUri.get(uri);
        return kernel;
    }
    notifyThatHostWasSet() {
        for (let kernel of this._kernels) {
            this.updateKernelInfoAndIndex(kernel);
        }
    }
}
