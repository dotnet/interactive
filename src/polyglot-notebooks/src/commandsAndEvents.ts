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

function toBase64String(value: Uint8Array): string {
    const wnd = <any>(globalThis.window);
    if (wnd) {
        return wnd.btoa(String.fromCharCode(...value));
    } else {
        return Buffer.from(value).toString('base64');
    }
}
export class KernelCommandEnvelope {

    private _childCommandCounter: number = 0;
    private _routingSlip: CommandRoutingSlip = new CommandRoutingSlip();
    private _token?: string;
    private _parentCommand?: KernelCommandEnvelope;

    constructor(
        public commandType: contracts.KernelCommandType,
        public command: contracts.KernelCommand) {
    }

    public get routingSlip(): CommandRoutingSlip {
        return this._routingSlip;
    }

    public get parentCommand(): KernelCommandEnvelope | undefined {
        return this._parentCommand;
    }

    public static isKernelCommandEnvelopeModel(arg: KernelCommandEnvelope | KernelCommandEnvelopeModel): arg is KernelCommandEnvelopeModel {
        return !(<any>arg).getOrCreateToken;
    }

    public setParent(parentCommand: KernelCommandEnvelope | undefined) {
        if (this._parentCommand && this._parentCommand !== parentCommand) {
            throw new Error("Parent cannot be changed.");
        }
        if (this._parentCommand === null || this._parentCommand === undefined) {
            {
                // todo: do we need to override the token? Should this throw if parenting happens after token is set?
                if (this._token) {
                    this._token = undefined;
                }
                this._parentCommand = parentCommand;
                this.getOrCreateToken();
            }
        }

    }

    public static areCommandsTheSame(envelope1: KernelCommandEnvelope, envelope2: KernelCommandEnvelope): boolean {
        envelope1;//?
        envelope2;//?
        envelope1 === envelope2;//?

        // reference equality
        if (envelope1 === envelope2) {
            return true;
        }

        // commandType equality
        const sameCommandType = envelope1?.commandType === envelope2?.commandType; //?
        if (!sameCommandType) {
            return false;
        }

        // both must have tokens
        if ((!envelope1?._token) || (!envelope2?._token)) {
            return false;
        }

        // token equality
        const sameToken = envelope1?._token === envelope2?._token; //?
        if (!sameToken) {
            return false;
        }
        return true;
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
        this._token = toBase64String(data);

        return this._token;
    }

    public getToken(): string {
        if (this._token) {
            return this._token;
        }
        throw new Error('token not set');
    }

    public isSelforDescendantOf(otherCommand: KernelCommandEnvelope) {
        const otherToken = otherCommand._token;
        const thisToken = this._token;
        if (thisToken && otherToken) {
            return thisToken.startsWith(otherToken!);
        }

        throw new Error('both commands must have tokens');
    }

    public hasSameRootCommandAs(otherCommand: KernelCommandEnvelope) {
        const otherToken = otherCommand._token;
        const thisToken = this._token;
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
            token: this.getOrCreateToken()
        };

        return model;
    }

    public static fromJson(model: KernelCommandEnvelopeModel): KernelCommandEnvelope {
        const command = new KernelCommandEnvelope(model.commandType, model.command);
        command._routingSlip = CommandRoutingSlip.fromUris(model.routingSlip || []);
        command._token = model.token;
        return command;
    }

    public clone(): KernelCommandEnvelope {
        return KernelCommandEnvelope.fromJson(this.toJson());
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

    public clone(): KernelEventEnvelope {
        return KernelEventEnvelope.fromJson(this.toJson());
    }
}
