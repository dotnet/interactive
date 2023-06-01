// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as commandsAndEvents from './commandsAndEvents';
import * as connection from './connection';
import * as routingSlip from './routingslip';
import { Kernel } from './kernel';
import { ProxyKernel } from './proxyKernel';
import { Logger } from './logger';
import { KernelScheduler } from './kernelScheduler';

export class KernelHost {
    private readonly _remoteUriToKernel = new Map<string, Kernel>();
    private readonly _uriToKernel = new Map<string, Kernel>();
    private readonly _kernelToKernelInfo = new Map<Kernel, commandsAndEvents.KernelInfo>();
    private readonly _uri: string;
    private readonly _scheduler: KernelScheduler<commandsAndEvents.KernelCommandEnvelope>;
    private _kernel: CompositeKernel;
    private _defaultConnector: connection.Connector;
    private readonly _connectors: connection.Connector[] = [];

    constructor(kernel: CompositeKernel, sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver, hostUri: string) {
        this._kernel = kernel;
        this._uri = routingSlip.createKernelUri(hostUri || "kernel://vscode");

        this._kernel.host = this;
        this._scheduler = new KernelScheduler<commandsAndEvents.KernelCommandEnvelope>();

        this._scheduler.setMustTrampoline((c => {
            return (c.commandType === commandsAndEvents.RequestInputType) || (c.commandType === commandsAndEvents.SendEditableCodeType);
        }));

        this._defaultConnector = new connection.Connector({ sender, receiver });
        this._connectors.push(this._defaultConnector);
    }

    public get defaultConnector(): connection.Connector {
        return this._defaultConnector;
    }

    public get uri(): string {
        return this._uri;
    }

    public get kernel(): CompositeKernel {
        return this._kernel;
    }

    public tryGetKernelByRemoteUri(remoteUri: string): Kernel | undefined {
        return this._remoteUriToKernel.get(remoteUri);
    }

    public trygetKernelByOriginUri(originUri: string): Kernel | undefined {
        return this._uriToKernel.get(originUri);
    }

    public tryGetKernelInfo(kernel: Kernel): commandsAndEvents.KernelInfo | undefined {
        return this._kernelToKernelInfo.get(kernel);
    }

    public addKernelInfo(kernel: Kernel, kernelInfo: commandsAndEvents.KernelInfo) {
        kernelInfo.uri = routingSlip.createKernelUri(`${this._uri}${kernel.name}`);
        this._kernelToKernelInfo.set(kernel, kernelInfo);
        this._uriToKernel.set(kernelInfo.uri, kernel);
    }

    public getKernel(kernelCommandEnvelope: commandsAndEvents.KernelCommandEnvelope): Kernel {

        const uriToLookup = kernelCommandEnvelope.command.destinationUri ?? kernelCommandEnvelope.command.originUri;
        let kernel: Kernel | undefined = undefined;
        if (uriToLookup) {
            kernel = this._kernel.findKernelByUri(uriToLookup);
        }

        if (!kernel) {
            if (kernelCommandEnvelope.command.targetKernelName) {
                kernel = this._kernel.findKernelByName(kernelCommandEnvelope.command.targetKernelName);
            }
        }

        kernel ??= this._kernel;
        Logger.default.info(`Using Kernel ${kernel.name}`);
        return kernel;
    }

    public connectProxyKernelOnDefaultConnector(localName: string, remoteKernelUri?: string, aliases?: string[]): ProxyKernel {
        return this.connectProxyKernelOnConnector(localName, this._defaultConnector.sender, this._defaultConnector.receiver, remoteKernelUri, aliases);
    }

    public tryAddConnector(connector: { sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver, remoteUris?: string[] }) {
        if (!connector.remoteUris) {
            this._connectors.push(new connection.Connector(connector));
            return true;
        } else {
            const found = connector.remoteUris!.find(uri => this._connectors.find(c => c.canReach(uri)));
            if (!found) {
                this._connectors.push(new connection.Connector(connector));
                return true;
            }
            return false;
        }
    }

    public tryRemoveConnector(connector: { remoteUris?: string[] }) {
        if (!connector.remoteUris) {
            for (let uri of connector.remoteUris!) {
                const index = this._connectors.findIndex(c => c.canReach(uri));
                if (index >= 0) {
                    this._connectors.splice(index, 1);
                }
            }
            return true;
        } else {

            return false;
        }
    }

    public connectProxyKernel(localName: string, remoteKernelUri: string, aliases?: string[]): ProxyKernel {
        this._connectors;//?
        const connector = this._connectors.find(c => c.canReach(remoteKernelUri));
        if (!connector) {
            throw new Error(`Cannot find connector to reach ${remoteKernelUri}`);
        }
        let kernel = new ProxyKernel(localName, connector.sender, connector.receiver);
        kernel.kernelInfo.remoteUri = remoteKernelUri;
        this._kernel.add(kernel, aliases);
        return kernel;
    }

    private connectProxyKernelOnConnector(localName: string, sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver, remoteKernelUri?: string, aliases?: string[]): ProxyKernel {
        let kernel = new ProxyKernel(localName, sender, receiver);
        kernel.kernelInfo.remoteUri = remoteKernelUri;
        this._kernel.add(kernel, aliases);
        return kernel;
    }

    public tryGetConnector(remoteUri: string) {
        return this._connectors.find(c => c.canReach(remoteUri));
    }

    public async connect(): Promise<commandsAndEvents.KernelReady> {
        this._kernel.subscribeToKernelEvents(e => {
            Logger.default.info(`KernelHost forwarding event: ${JSON.stringify(e)}`);
            this._defaultConnector.sender.send(e);
        });

        this._defaultConnector.receiver.subscribe({
            next: (kernelCommandOrEventEnvelope: connection.KernelCommandOrEventEnvelope) => {

                if (connection.isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                    Logger.default.info(`KernelHost dispacthing command: ${JSON.stringify(kernelCommandOrEventEnvelope)}`);
                    this._scheduler.runAsync(kernelCommandOrEventEnvelope, commandEnvelope => {
                        const kernel = this._kernel;
                        return kernel.send(commandEnvelope);
                    });
                }
            }
        });

        const kernelInfos = [this._kernel.kernelInfo, ...Array.from(this._kernel.childKernels.map(k => k.kernelInfo).filter(ki => ki.isProxy === false))];

        const kernekReady: commandsAndEvents.KernelReady = {
            kernelInfos: kernelInfos
        };

        const event = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.KernelReadyType, kernekReady);
        event.routingSlip.stamp(this._kernel.kernelInfo.uri!);

        await this._defaultConnector.sender.send(event);

        return kernekReady;
    }

    public getKernelInfos(): commandsAndEvents.KernelInfo[] {
        let kernelInfos = [this._kernel.kernelInfo];
        for (let kernel of this._kernel.childKernels) {
            kernelInfos.push(kernel.kernelInfo);
        }
        return kernelInfos;
    }

    public getKernelInfoProduced(): commandsAndEvents.KernelEventEnvelope[] {
        let events: commandsAndEvents.KernelEventEnvelope[] = Array.from(this.getKernelInfos().map(kernelInfo => {
            const event = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.KernelInfoProducedType, <commandsAndEvents.KernelInfoProduced>{ kernelInfo: kernelInfo });
            event.routingSlip.stamp(kernelInfo.uri!);
            return event;
        }
        ));

        return events;
    }
}
