// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import { Kernel } from './kernel';
import { ProxyKernel } from './proxyKernel';
import { Logger } from './logger';
import { KernelCommandScheduler } from './kernelCommandScheduler';

export class KernelHost {
    private readonly _destinationUriToKernel = new Map<string, Kernel>();
    private readonly _originUriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, contracts.KernelInfo>();
    private readonly _uri: string;
    private readonly _scheduler: KernelCommandScheduler;

    constructor(private readonly _kernel: CompositeKernel, private readonly _channel: contracts.KernelCommandAndEventChannel, hostUri: string) {
        this._uri = hostUri || "kernel://vscode";
        this._kernel.host = this;
        this._scheduler = new KernelCommandScheduler(commandEnvelope => {
            const kernel = this.getKernel(commandEnvelope);
            return kernel.send(commandEnvelope);
        });
    }

    public tryGetKernelByDestinationUri(destinationUri: string): Kernel | undefined {
        return this._destinationUriToKernel.get(destinationUri);
    }

    public trygetKernelByOriginUri(originUri: string): Kernel | undefined {
        return this._originUriToKernel.get(originUri);
    }

    public tryGetKernelInfo(kernel: Kernel): contracts.KernelInfo | undefined {
        return this._kernelToKernelInfo.get(kernel);
    }

    public addKernelInfo(kernel: Kernel, kernelInfo: contracts.KernelInfo) {

        kernelInfo.originUri = `${this._uri}/${kernel.name}`;
        this._kernelToKernelInfo.set(kernel, kernelInfo);
        this._originUriToKernel.set(kernelInfo.originUri, kernel);
    }

    public getKernel(kernelCommandEnvelope: contracts.KernelCommandEnvelope): Kernel {

        if (kernelCommandEnvelope.destinationUri) {
            let fromDestinationUri = this._originUriToKernel.get(kernelCommandEnvelope.destinationUri.toLowerCase());
            if (fromDestinationUri) {
                Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.destinationUri}`);
                return fromDestinationUri;
            }

            fromDestinationUri = this._destinationUriToKernel.get(kernelCommandEnvelope.destinationUri.toLowerCase());
            if (fromDestinationUri) {
                Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.destinationUri}`);
                return fromDestinationUri;
            }
        }

        if (kernelCommandEnvelope.originUri) {
            let fromOriginUri = this._originUriToKernel.get(kernelCommandEnvelope.originUri.toLowerCase());
            if (fromOriginUri) {
                Logger.default.info(`Kernel ${fromOriginUri.name} found for origin uri ${kernelCommandEnvelope.originUri}`);
                return fromOriginUri;
            }
        }

        Logger.default.info(`Using Kernel ${this._kernel.name}`);
        return this._kernel;
    }

    public registerDestinationUriForProxy(proxyLocalKernelName: string, destinationUri: string) {
        const kernel = this._kernel.findKernelByName(proxyLocalKernelName);
        if (!(kernel as ProxyKernel)) {
            throw new Error(`Kernel ${proxyLocalKernelName} is not a proxy kernel`);
        }

        const kernelinfo = this._kernelToKernelInfo.get(kernel!);

        if (!kernelinfo) {
            throw new Error("kernelinfo not found");
        }
        if (kernelinfo?.destinationUri) {
            Logger.default.info(`Removing destination uri ${kernelinfo.destinationUri} for proxy kernel ${kernelinfo.localName}`);
            this._destinationUriToKernel.delete(kernelinfo.destinationUri.toLowerCase());
        }
        kernelinfo.destinationUri = destinationUri;

        if (kernel) {
            Logger.default.info(`Registering destination uri ${destinationUri} for proxy kernel ${kernelinfo.localName}`);
            this._destinationUriToKernel.set(destinationUri.toLowerCase(), kernel);
        }
    }

    public createProxyKernelOnDefaultConnector(kernelInfo: contracts.KernelInfo): ProxyKernel {
        const proxyKernel = new ProxyKernel(kernelInfo.localName, this._channel);
        this._kernel.add(proxyKernel, kernelInfo.aliases);
        if (kernelInfo.destinationUri) {
            this.registerDestinationUriForProxy(proxyKernel.name, kernelInfo.destinationUri);
        }
        return proxyKernel;
    }

    public connect() {
        this._channel.setCommandHandler((kernelCommandEnvelope: contracts.KernelCommandEnvelope) => {
            // fire and forget this one
            this._scheduler.schedule(kernelCommandEnvelope);
            return Promise.resolve();
        });

        this._kernel.subscribeToKernelEvents(e => {
            this._channel.publishKernelEvent(e);
        });
    }
}