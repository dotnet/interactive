// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { CommandRoutingSlip, EventRoutingSlip } from "./routingslip";
export * from "./contracts";
import * as uuid from "uuid";

export interface DocumentKernelInfoCollection {
    defaultKernelName: string;
    items: contracts.DocumentKernelInfo[];
}

export interface KernelEventEnvelope {
    eventType: contracts.KernelEventType;
    event: contracts.KernelEvent;
    command?: KernelCommandEnvelope;
    routingSlip?: string[];
}

export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
    commandType: contracts.KernelCommandType;
    command: contracts.KernelCommand;
    routingSlip?: string[];
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}

export class KernelCommandEnvelope2 {
    private _childCommandCounter: number = 0;
    private _routingSlip: CommandRoutingSlip = new CommandRoutingSlip();
    private _id?: string;
    private _token?: string;
    private _parentCommand?: KernelCommandEnvelope2;

    constructor(
        public commandType: contracts.KernelCommandType,
        public command: contracts.KernelCommand) {

        const guidBytes = uuid.parse(uuid.v4());
        const data = new Uint8Array(guidBytes);
        const buffer = Buffer.from(data.buffer);
        this._id = buffer.toString('base64');
    }

    public get routingSlip(): CommandRoutingSlip {
        return this._routingSlip;
    }

    public get parentCommand(): KernelCommandEnvelope2 | undefined {
        return this._parentCommand;
    }

    public set parent(parentCommand: KernelCommandEnvelope2 | undefined) {
        if (this._parentCommand && this._parentCommand !== parentCommand) {
            throw new Error("Parent cannot be changed.");
        }
        this._parentCommand = parentCommand;
    }

    public getOrCreateToken(): string {
        if (this._token) {
            return this._token;
        }

        if (this._parentCommand) {
            this._token = `${this._parentCommand.getOrCreateToken()}.${this._parentCommand.getNextChildToken()}`;
            return this._token
        }
        const guidBytes = uuid.parse(uuid.v4());
        const data = new Uint8Array(guidBytes);
        const buffer = Buffer.from(data.buffer);
        this._token = buffer.toString('base64');

        return this._token;
    }

    public toJson(): KernelCommandEnvelope {
        const model: KernelCommandEnvelope = {
            commandType: this.commandType,
            command: this.command,
            routingSlip: this._routingSlip.toArray(),
            id: this._id,
            token: this.getOrCreateToken()
        };

        return model;
    }

    public static fromJson(model: KernelCommandEnvelope): KernelCommandEnvelope2 {
        const command = new KernelCommandEnvelope2(model.commandType, model.command);
        command._routingSlip = CommandRoutingSlip.fromUris(model.routingSlip || []);
        command._id = model.id;
        command._token = model.token;
        return command;
    }

    private getNextChildToken(): number {
        return this._childCommandCounter++;
    }
}

export class KernelEventEnvelope2 {
    private _routingSlip: EventRoutingSlip = new EventRoutingSlip();
    constructor(
        public eventType: contracts.KernelEventType,
        public event: contracts.KernelEvent,
        public command?: KernelCommandEnvelope2) {
    }

    public get routingSlip(): EventRoutingSlip {
        return this._routingSlip;
    }

    public toJson(): KernelEventEnvelope {
        const model: KernelEventEnvelope = {
            eventType: this.eventType,
            event: this.event,
            command: this.command?.toJson(),
            routingSlip: this._routingSlip.toArray()
        };

        return model;
    }

    public static fromJson(model: KernelEventEnvelope): KernelEventEnvelope2 {
        const event = new KernelEventEnvelope2(
            model.eventType,
            model.event,
            model.command ? KernelCommandEnvelope2.fromJson(model.command) : undefined);
        event._routingSlip = EventRoutingSlip.fromUris(model.routingSlip || []
        );
        return event;
    }
}