// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from '../interfaces/contracts';
import { Kernel } from './kernel';
import { KernelInfo } from './kernelInfo';

export class KernelHost {
    private readonly _destinationUriToKernel = new Map<string, Kernel>();
    private readonly _originUriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, KernelInfo>();
    private readonly _uri: string;
    constructor(private readonly _kernel: CompositeKernel, private readonly _transport: contracts.Transport, hostUri: string) {
        this._uri = hostUri || "kernel://vscode";
        this._kernel.SetHost(this);
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
                return fromDestinationUri;
            }
        }

        if (kernelCommandEnvelope.originUri) {
            let fromOriginUri = this._originUriToKernel.get(kernelCommandEnvelope.originUri);
            if (fromOriginUri) {
                return fromOriginUri;
            }
        }

        return this._kernel
    }

}