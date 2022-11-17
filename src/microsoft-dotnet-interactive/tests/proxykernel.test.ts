// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import { ProxyKernel } from "../src/proxyKernel";
import { Logger } from "../src/logger";
import * as rxjs from "rxjs";
import * as connection from "../src/connection";

function removeCommandTokenAndId(envelope: connection.KernelCommandOrEventEnvelope) {
    if (connection.isKernelEventEnvelope(envelope)) {
        delete envelope.command?.id;
        delete envelope.command?.token;

    } else if (connection.isKernelCommandEnvelope(envelope)) {
        delete envelope.id;
        delete envelope.token;
    }

    return envelope;
}

describe("proxyKernel", () => {
    before(() => {
        Logger.configure("test", () => { });
    });

    it("forwards commands over the transport", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next({ eventType: contracts.CommandSucceededType, event: {}, command: e });
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: contracts.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command: contracts.KernelCommandEnvelope = { commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } };
        await kernel.send(command);

        expect(events[0]).to.include({
            eventType: contracts.CommandSucceededType,
            command: command
        });

    });

    it("procudes commandFailed", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next({ eventType: contracts.CommandFailedType, event: <contracts.CommandFailed>{ message: "something is wrong" }, command: e });
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: contracts.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command: contracts.KernelCommandEnvelope = { commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } };
        await kernel.send(command);

        expect(events[0]).to.include({
            eventType: contracts.CommandFailedType,
            command: command
        });
    });

    it("forwards events", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next({ eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, command: e });
                remoteToLocal.next({ eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, command: e });
                remoteToLocal.next({ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: e });
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: contracts.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command: contracts.KernelCommandEnvelope = { commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } };
        await kernel.send(command);

        events;//?
        expect(removeCommandTokenAndId(events[0])).to.deep.include({
            eventType: contracts.ValueProducedType,
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
                    originUri: 'kernel://local/proxy'
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(removeCommandTokenAndId(events[1])).to.deep.include({
            eventType: contracts.ValueProducedType,
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
                    originUri: 'kernel://local/proxy'
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(removeCommandTokenAndId(events[2])).to.deep.include({
            eventType: contracts.CommandSucceededType,
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
                remoteToLocal.next({ eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, command: { ...e, id: "newId" } });
                remoteToLocal.next({ eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, command: { ...e, id: "newId" } });
                remoteToLocal.next({ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: e });
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));
        let events: contracts.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command: contracts.KernelCommandEnvelope = { commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" }, id: "newId" };
        await kernel.send(command);

        expect(removeCommandTokenAndId(events[0])).to.be.deep.include({
            eventType: contracts.ValueProducedType,
            event: {
                name: "a",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable a"
                }
            },
            command: {
                command: {
                    code: '1+2', originUri:
                        'kernel://local/proxy'
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(removeCommandTokenAndId(events[1])).to.be.deep.include({
            eventType: contracts.ValueProducedType,
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
                    originUri: 'kernel://local/proxy'
                },
                commandType: 'SubmitCode',
                routingSlip: ['kernel://local/proxy?tag=arrived']
            }
        });

        expect(events[2]).to.include({
            eventType: contracts.CommandSucceededType,
            command: command
        });
    });

    it("updates kernelInfo when KernelInfoProduced is intercepted", async () => {
        let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        localToRemote.subscribe(e => {
            if (connection.isKernelCommandEnvelope(e)) {
                remoteToLocal.next({
                    eventType: contracts.KernelInfoProducedType,
                    event: <contracts.KernelInfoProduced>{
                        kernelInfo: {
                            localName: "remoteKernel",
                            aliases: [],
                            uri: 'kernel://local/remoteKernel',
                            languageName: "gsharp",
                            languageVersion: "1.2.3",
                            supportedKernelCommands: [{ name: "customCommand1" }, { name: "customCommand2" }],
                            supportedDirectives: []
                        }
                    }

                });

                remoteToLocal.next({ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: e });
            }
        });

        let kernel = new ProxyKernel("proxy", connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal));

        kernel.registerCommandHandler({
            commandType: "customCommand1",
            handle: (invocation) => {
                return Promise.resolve();
            }
        });

        let events: contracts.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents((e) => events.push(e));
        let command: contracts.KernelCommandEnvelope = { commandType: contracts.RequestKernelInfoType, command: <contracts.RequestKernelInfo>{ targetKernelName: "proxy" } };
        await kernel.send(command);

        expect(kernel.kernelInfo).to.deep.equal({
            aliases: [],
            languageName: 'gsharp',
            languageVersion: '1.2.3',
            localName: 'proxy',
            supportedDirectives: [],
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'customCommand1' },
                { name: 'customCommand2' }],
            uri: 'kernel://local/proxy'
        });
    });
});


