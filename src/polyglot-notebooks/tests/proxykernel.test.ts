// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as commandsAndEvents from "../src/commandsAndEvents";
import { ProxyKernel } from "../src/proxyKernel";
import { Logger } from "../src/logger";
import * as rxjs from "rxjs";
import * as connection from "../src/connection";
import { clearTokenAndId } from "./testSupport";


describe("proxyKernel", () => {
    before(() => {
        Logger.configure("test", () => { });
    });

    it("forwards commands over the transport", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                const commandSuucceeded = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, <commandsAndEvents.CommandSucceeded>{}, e);
                remoteToLocal.next(commandSuucceeded);
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "1+2" });
        await kernel.send(command);

        expect(events[0]).to.include({
            eventType: commandsAndEvents.CommandSucceededType,
            command: command
        });

    });

    it("produces commandFailed", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                const commandFailedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandFailedType, <commandsAndEvents.CommandFailed>{ message: "something is wrong" }, e);
                remoteToLocal.next(commandFailedEvent);
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "1+2" });

        await kernel.send(command);

        expect(events[0]).to.include({
            eventType: commandsAndEvents.CommandFailedType,
            command: command
        });
    });

    it("forwards events", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueProducedType, <commandsAndEvents.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, e));
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueProducedType, <commandsAndEvents.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, e));
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, <commandsAndEvents.CommandSucceeded>{}, e));
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "1+2" });
        await kernel.send(command);

        events;//?
        expect(clearTokenAndId(events[0].toJson())).to.deep.include({
            eventType: commandsAndEvents.ValueProducedType,
            event: {
                name: "a",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable a"
                }
            },
            command: {
                command: {
                    code: '1+2',
                    originUri: 'kernel://local/proxy',
                    destinationUri: undefined
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(clearTokenAndId(events[1].toJson())).to.deep.include({
            eventType: commandsAndEvents.ValueProducedType,
            event: {
                name: "b",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable b"
                }
            },
            command: {
                command: {
                    code: '1+2',
                    originUri: 'kernel://local/proxy',
                    destinationUri: undefined
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(clearTokenAndId(events[2].toJson())).to.deep.include({
            eventType: commandsAndEvents.CommandSucceededType,
            command: {
                command:
                {
                    code: '1+2',
                    destinationUri: undefined,
                    originUri: 'kernel://local/proxy'
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived', 'kernel://local/proxy']
            }
        });
    });

    it("forwards events of remotely split commands", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueProducedType, <commandsAndEvents.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, e));
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueProducedType, <commandsAndEvents.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, e));
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, <commandsAndEvents.CommandSucceeded>{}, e));
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "1+2" });
        await kernel.send(command);

        expect(clearTokenAndId(events[0].toJson())).to.be.deep.include({
            eventType: commandsAndEvents.ValueProducedType,
            event: {
                name: "a",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable a"
                }
            },
            command: {
                command: {
                    code: '1+2',
                    originUri: 'kernel://local/proxy',
                    destinationUri: undefined
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(clearTokenAndId(events[1].toJson())).to.be.deep.include({
            eventType: commandsAndEvents.ValueProducedType,
            event: {
                name: "b",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable b"
                }
            },
            command: {
                command: {
                    code: '1+2',
                    originUri: 'kernel://local/proxy',
                    destinationUri: undefined
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(events[2]).to.include({
            eventType: commandsAndEvents.CommandSucceededType,
            command: command
        });
    });

    it("updates kernelInfo when KernelInfoProduced is intercepted", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                const kernelInfoProduced = new commandsAndEvents.KernelEventEnvelope(
                    commandsAndEvents.KernelInfoProducedType,
                    <commandsAndEvents.KernelInfoProduced>{
                        kernelInfo: {
                            isComposite: false,
                            isProxy: false,
                            description: `This kernel executes g# code.`,
                            localName: "remoteKernel",
                            aliases: [],
                            uri: 'kernel://local/remoteKernel',
                            languageName: "gsharp",
                            languageVersion: "1.2.3",
                            displayName: "G#",
                            supportedKernelCommands: [{ name: "customCommand1" }, { name: "customCommand2" }],
                            supportedDirectives: []
                        }
                    },
                    e
                );
                remoteToLocal.next(kernelInfoProduced);
                remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, <commandsAndEvents.CommandSucceeded>{}, e));
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        kernel.kernelInfo.remoteUri = 'kernel://local/remoteKernel';
        kernel.registerCommandHandler({
            commandType: "customCommand1",
            handle: (invocation) => {
                return Promise.resolve();
            }
        });

        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, <commandsAndEvents.RequestKernelInfo>{ targetKernelName: "proxy" });
        await kernel.send(command);

        expect(kernel.kernelInfo).to.deep.equal({
            aliases: [],
            displayName: 'G#',
            isComposite: false,
            isProxy: true,
            languageName: 'gsharp',
            languageVersion: '1.2.3',
            localName: 'proxy',
            remoteUri: 'kernel://local/remoteKernel',
            description: `This kernel executes g# code.`,
            supportedDirectives: [],
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'customCommand1' },
                { name: 'customCommand2' }],
            uri: 'kernel://local/proxy'
        });
    });
});
