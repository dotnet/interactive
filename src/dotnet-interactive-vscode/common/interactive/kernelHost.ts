// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from '../interfaces/contracts';
import { Kernel } from './kernel';
import { KernelInfo } from './kernelInfo';
import { ProxyKernel } from './proxyKernel';
import { Logger } from '../logger';

export class KernelHost {
    private readonly _destinationUriToKernel = new Map<string, Kernel>();
    private readonly _originUriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, KernelInfo>();
    private readonly _uri: string;

    constructor(private readonly _kernel: CompositeKernel, private readonly _transport: contracts.Transport, hostUri: string) {
        this._uri = hostUri || "kernel://vscode";
        this._kernel.host = this;
    }

    public tryGetKernelByDestinationUri(destinationUri: string): Kernel | undefined {
        return this._destinationUriToKernel.get(destinationUri);
    }

    public trygetKernelByOriginUri(originUri: string): Kernel | undefined {
        return this._originUriToKernel.get(originUri);
    }

    public tryGetKernelInfo(kernel: Kernel): KernelInfo | undefined {
        return this._kernelToKernelInfo.get(kernel);
    }

    public addKernelInfo(kernel: Kernel, kernelInfo: KernelInfo) {

        kernelInfo.originUri = `${this._uri}/${kernel.name}`;
        this._kernelToKernelInfo.set(kernel, kernelInfo);
        this._originUriToKernel.set(kernelInfo.originUri, kernel);
    }

    private getKernel(kernelCommandEnvelope: contracts.KernelCommandEnvelope): Kernel {

        if (kernelCommandEnvelope.destinationUri) {
            let fromDestinationUri = this._destinationUriToKernel.get(kernelCommandEnvelope.destinationUri);
            if (fromDestinationUri) {
                Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.destinationUri}`);
                return fromDestinationUri;
            }
        }

        if (kernelCommandEnvelope.originUri) {
            let fromOriginUri = this._originUriToKernel.get(kernelCommandEnvelope.originUri);
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

        const kernelinfo = this._kernelToKernelInfo.get(kernel);
        if (!kernelinfo) {
            throw new Error("kernelinfo not found");
        }
        if (kernelinfo?.destinationUri) {
            Logger.default.info(`Removing destination uri ${kernelinfo.destinationUri} for proxy kernel ${kernel.name}`);
            this._destinationUriToKernel.delete(kernelinfo.destinationUri);
        }
        kernelinfo.destinationUri = destinationUri;

        if (kernel) {
            Logger.default.info(`Registering destination uri ${destinationUri} for proxy kernel ${kernel.name}`);
            this._destinationUriToKernel.set(destinationUri, kernel);
        }
    }

    public connect() {
        this._transport.setCommandHandler((kernelCommandEnvelope: contracts.KernelCommandEnvelope) => {
            const kernel = this.getKernel(kernelCommandEnvelope);
            return kernel.send(kernelCommandEnvelope);
        });
    }
}