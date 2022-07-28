// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import * as connection from './connection';
import { Kernel, KernelType } from './kernel';
import { ProxyKernel } from './proxyKernel';
import { Logger } from './logger';
import { KernelScheduler } from './kernelScheduler';

export class KernelHost {
    private readonly _remoteUriToKernel = new Map<string, Kernel>();
    private readonly _uriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, contracts.KernelInfo>();
    private readonly _uri: string;
    private readonly _scheduler: KernelScheduler<contracts.KernelCommandEnvelope>;
    private _defaultSender: connection.IKernelCommandAndEventSender;
    private _defaultReceiver: connection.IKernelCommandAndEventReceiver;
    private _kernel: CompositeKernel;

    constructor(kernel: CompositeKernel, sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver, hostUri: string) {
        this._kernel = kernel;
        this._uri = hostUri || "kernel://vscode";
        this._kernel.host = this;
        this._scheduler = new KernelScheduler<contracts.KernelCommandEnvelope>();
        this._defaultSender = sender;
        this._defaultReceiver = receiver;
    }

    public get uri(): string {
        return this._uri;
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

        kernelInfo.uri = `${this._uri}/${kernel.name}`;//?
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

    public connectProxyKernelOnDefaultConnector(localName: string, remoteKernelUri?: string, aliases?: string[]): ProxyKernel {
        return this.connectProxyKernelOnConnector(localName, this._defaultSender, this._defaultReceiver, remoteKernelUri, aliases);
    }

    public connectProxyKernelOnConnector(localName: string, sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver, remoteKernelUri?: string, aliases?: string[]): ProxyKernel {
        let kernel = new ProxyKernel(localName, sender, receiver);
        kernel.kernelInfo.remoteUri = remoteKernelUri;
        this._kernel.add(kernel, aliases);
        return kernel;
    }

    public connect() {
        this._kernel.subscribeToKernelEvents(e => {
            this._defaultSender.send(e);
        });

        this._defaultReceiver.subscribe({
            next: (kernelCommandOrEventEnvelope: connection.KernelCommandOrEventEnvelope) => {
                if (connection.isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                    this._scheduler.runAsync(kernelCommandOrEventEnvelope, commandEnvelope => {
                        const kernel = this._kernel;;
                        return kernel.send(commandEnvelope);
                    });
                }
            }
        });

        this._defaultSender.send({ eventType: contracts.KernelReadyType, event: {} });

        this._defaultSender.send({ eventType: contracts.KernelInfoProducedType, event: <contracts.KernelInfoProduced>{ kernelInfo: this._kernel.kernelInfo }, routingSlip: [this._kernel.kernelInfo.uri!] });

        for (let kernel of this._kernel.childKernels) {
            this._defaultSender.send({ eventType: contracts.KernelInfoProducedType, event: <contracts.KernelInfoProduced>{ kernelInfo: kernel.kernelInfo }, routingSlip: [kernel.kernelInfo.uri!] });
        }

    }
}
