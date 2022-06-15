// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import { Kernel } from './kernel';
import { ProxyKernel } from './proxyKernel';
import { Logger } from './logger';
import { KernelScheduler } from './kernelScheduler';

export class KernelHost {
    private readonly _remoteUriToKernel = new Map<string, Kernel>();
    private readonly _uriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, contracts.KernelInfo>();
    private readonly _uri: string;
    private readonly _scheduler: KernelScheduler<contracts.KernelCommandEnvelope>;

    constructor(private readonly _kernel: CompositeKernel, private readonly _channel: contracts.KernelCommandAndEventChannel, hostUri: string) {
        this._uri = hostUri || "kernel://vscode";
        this._kernel.host = this;
        this._scheduler = new KernelScheduler<contracts.KernelCommandEnvelope>();
    }

    public tryGetKernelByRemoteUri(remoteUri: string): Kernel | undefined {
        return this._remoteUriToKernel.get(remoteUri);
    }

    public trygetKernelByOriginUri(originUri: string): Kernel | undefined {
        return this._uriToKernel.get(originUri);
    }

    public tryGetKernelInfo(kernel: Kernel): contracts.KernelInfo | undefined {
        return this._kernelToKernelInfo.get(kernel);
    }

    public addKernelInfo(kernel: Kernel, kernelInfo: contracts.KernelInfo) {

        kernelInfo.uri = `${this._uri}/${kernel.name}`;
        this._kernelToKernelInfo.set(kernel, kernelInfo);
        this._uriToKernel.set(kernelInfo.uri, kernel);
    }

    public getKernel(kernelCommandEnvelope: contracts.KernelCommandEnvelope): Kernel {

        if (kernelCommandEnvelope.command.destinationUri) {
            let fromDestinationUri = this._uriToKernel.get(kernelCommandEnvelope.command.destinationUri.toLowerCase());
            if (fromDestinationUri) {
                Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.command.destinationUri}`);
                return fromDestinationUri;
            }

            fromDestinationUri = this._remoteUriToKernel.get(kernelCommandEnvelope.command.destinationUri.toLowerCase());
            if (fromDestinationUri) {
                Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.command.destinationUri}`);
                return fromDestinationUri;
            }
        }

        if (kernelCommandEnvelope.command.originUri) {
            let fromOriginUri = this._uriToKernel.get(kernelCommandEnvelope.command.originUri.toLowerCase());
            if (fromOriginUri) {
                Logger.default.info(`Kernel ${fromOriginUri.name} found for origin uri ${kernelCommandEnvelope.command.originUri}`);
                return fromOriginUri;
            }
        }

        Logger.default.info(`Using Kernel ${this._kernel.name}`);
        return this._kernel;
    }

    public registerRemoteUriForProxy(proxyLocalKernelName: string, remoteUri: string) {
        const kernel = this._kernel.findKernelByName(proxyLocalKernelName);
        if (!(kernel as ProxyKernel)) {
            throw new Error(`Kernel ${proxyLocalKernelName} is not a proxy kernel`);
        }

        const kernelinfo = this._kernelToKernelInfo.get(kernel!);

        if (!kernelinfo) {
            throw new Error("kernelinfo not found");
        }
        if (kernelinfo?.remoteUri) {
            Logger.default.info(`Removing remote uri ${kernelinfo.remoteUri} for proxy kernel ${kernelinfo.localName}`);
            this._remoteUriToKernel.delete(kernelinfo.remoteUri.toLowerCase());
        }
        kernelinfo.remoteUri = remoteUri;

        if (kernel) {
            Logger.default.info(`Registering remote uri ${remoteUri} for proxy kernel ${kernelinfo.localName}`);
            this._remoteUriToKernel.set(remoteUri.toLowerCase(), kernel);
        }
    }

    public createProxyKernelOnDefaultConnector(kernelInfo: contracts.KernelInfo): ProxyKernel {
        const proxyKernel = new ProxyKernel(kernelInfo.localName, this._channel);
        this._kernel.add(proxyKernel, kernelInfo.aliases);
        if (kernelInfo.remoteUri) {
            this.registerRemoteUriForProxy(proxyKernel.name, kernelInfo.remoteUri);
        }
        return proxyKernel;
    }

    public connect() {
        this._channel.setCommandHandler((kernelCommandEnvelope: contracts.KernelCommandEnvelope) => {
            // fire and forget this one
            this._scheduler.runAsync(kernelCommandEnvelope, commandEnvelope => {
                const kernel = this.getKernel(commandEnvelope);
                return kernel.send(commandEnvelope);
            });
            return Promise.resolve();
        });

        this._kernel.subscribeToKernelEvents(e => {
            this._channel.publishKernelEvent(e);
        });
    }
}