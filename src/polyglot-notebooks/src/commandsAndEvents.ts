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

export interface KernelEventEnvelopeModel {
    eventType: contracts.KernelEventType;
    event: contracts.KernelEvent;
    command?: KernelCommandEnvelopeModel;
    routingSlip?: string[];
}

export interface KernelCommandEnvelopeModel {
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

export class KernelCommandEnvelope {
    private _childCommandCounter: number = 0;
    private _routingSlip: CommandRoutingSlip = new CommandRoutingSlip();
    private _id: string;
    private _token?: string;
    private _parentCommand?: KernelCommandEnvelope;

    constructor(
        public commandType: contracts.KernelCommandType,
        public command: contracts.KernelCommand) {

        const guidBytes = uuid.parse(uuid.v4());
        const data = new Uint8Array(guidBytes);
        const buffer = Buffer.from(data.buffer);
        this._id = buffer.toString('base64');
    }

    public get id(): string {
        return this._id;
    }

    public get routingSlip(): CommandRoutingSlip {
        return this._routingSlip;
    }

    public get parentCommand(): KernelCommandEnvelope | undefined {
        return this._parentCommand;
    }

    public set parent(parentCommand: KernelCommandEnvelope | undefined) {
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
            return this._token;
        }
        const guidBytes = uuid.parse(uuid.v4());
        const data = new Uint8Array(guidBytes);
        const buffer = Buffer.from(data.buffer);
        this._token = buffer.toString('base64');

        return this._token;
    }

    public isSelforDescendantOf(otherCommand: KernelCommandEnvelope) {
        const otherToken = otherCommand.getOrCreateToken();
        const thisToken = this.getOrCreateToken();
        if (thisToken && otherToken) {
            return thisToken.startsWith(otherToken!);
        }

        throw new Error('both commands must have tokens');
    }

    public hasSameRootCommandAs(otherCommand: KernelCommandEnvelope) {
        const otherToken = otherCommand.getOrCreateToken();
        const thisToken = this.getOrCreateToken();
        if (thisToken && otherToken) {
            const otherRootToken = KernelCommandEnvelope.getRootToken(otherToken);
            const thisRootToken = KernelCommandEnvelope.getRootToken(thisToken);
            return thisRootToken === otherRootToken;
        }
        throw new Error('both commands must have tokens');
    }

    public static getRootToken(token: string): string {
        const parts = token.split('.');
        return parts[0];
    }

    public toJson(): KernelCommandEnvelopeModel {
        const model: KernelCommandEnvelopeModel = {
            commandType: this.commandType,
            command: this.command,
            routingSlip: this._routingSlip.toArray(),
            id: this._id,
            token: this.getOrCreateToken()
        };

        return model;
    }

    public static fromJson(model: KernelCommandEnvelopeModel): KernelCommandEnvelope {
        const command = new KernelCommandEnvelope(model.commandType, model.command);
        command._routingSlip = CommandRoutingSlip.fromUris(model.routingSlip || []);
        if (model.id) {
            command._id = model.id;
        }
        command._token = model.token;
        return command;
    }

    private getNextChildToken(): number {
        return this._childCommandCounter++;
    }
}

export class KernelEventEnvelope {
    private _routingSlip: EventRoutingSlip = new EventRoutingSlip();
    constructor(
        public eventType: contracts.KernelEventType,
        public event: contracts.KernelEvent,
        public command?: KernelCommandEnvelope) {
    }

    public get routingSlip(): EventRoutingSlip {
        return this._routingSlip;
    }

    public toJson(): KernelEventEnvelopeModel {
        const model: KernelEventEnvelopeModel = {
            eventType: this.eventType,
            event: this.event,
            command: this.command?.toJson(),
            routingSlip: this._routingSlip.toArray()
        };

        return model;
    }

    public static fromJson(model: KernelEventEnvelopeModel): KernelEventEnvelope {
        const event = new KernelEventEnvelope(
            model.eventType,
            model.event,
            model.command ? KernelCommandEnvelope.fromJson(model.command) : undefined);
        event._routingSlip = EventRoutingSlip.fromUris(model.routingSlip || []
        );
        return event;
    }
}