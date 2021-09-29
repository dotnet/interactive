// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/common/interfaces/contracts";
import { CompositeKernel } from "../src/common/interactive/compositeKernel";
import { Kernel } from "../src/common/interactive/kernel";
import { ProxyKernel } from "../src/common/interactive/proxyKernel";
import { findEventFromKernel } from "./testSupport";
import { CommandAndEventReceiver, GenericTransport, PromiseCompletionSource } from "../src/common/interactive/genericTransport";
import { Logger } from "../src/common/logger";

describe("proxyKernel", () => {
    before(() => {
        Logger.configure("test", () => { });
    });

    it("forwards commands over the transport", async () => {
        let inMemory = createInMemoryTransport();
        let kernel = new ProxyKernel("proxy", inMemory.transport);
        let events: contracts.KernelEventEnvelope[] = [];
        inMemory.transport.run();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } })

        expect(events[0]).to.include({
            eventType: contracts.CommandSucceededType,
            command: inMemory.sentItems[0]
        });

    });

    it("procudes commandFailed", async () => {
        let inMemory = createInMemoryTransport(ce => {
            return [{ eventType: contracts.CommandFailedType, event: <contracts.CommandFailed>{ message: "something is wrong" }, command: ce }];
        });
        let kernel = new ProxyKernel("proxy", inMemory.transport);
        let events: contracts.KernelEventEnvelope[] = [];
        inMemory.transport.run();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } })

        expect(events[0]).to.include({
            eventType: contracts.CommandFailedType,
            command: inMemory.sentItems[0]
        });
    });

    it("forwards events", async () => {
        let inMemory = createInMemoryTransport(ce => {
            return [
                { eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, command: ce },
                { eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, command: ce },
                { eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: ce }];
        });
        let kernel = new ProxyKernel("proxy", inMemory.transport);
        let events: contracts.KernelEventEnvelope[] = [];
        inMemory.transport.run();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } })

        expect(events[0]).to.be.deep.eq({
            eventType: contracts.ValueProducedType,
            event: {
                name: "a",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable a"
                }
            },
            command: inMemory.sentItems[0]
        });

        expect(events[1]).to.be.deep.eq({
            eventType: contracts.ValueProducedType,
            event: {
                name: "b",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable b"
                }
            },
            command: inMemory.sentItems[0]
        });

        expect(events[2]).to.include({
            eventType: contracts.CommandSucceededType,
            command: inMemory.sentItems[0]
        });
    });

    it("forwards events ofremotely split commands", async () => {
        let inMemory = createInMemoryTransport(ce => {
            return [
                { eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "a", formattedValue: { mimeType: "text/plain", value: "variable a" } }, command: { ...ce, ["command.id"]: "newId" } },
                { eventType: contracts.ValueProducedType, event: <contracts.ValueProduced>{ name: "b", formattedValue: { mimeType: "text/plain", value: "variable b" } }, command: { ...ce, ["command.id"]: "newId" } },
                { eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: ce }];
        });
        let kernel = new ProxyKernel("proxy", inMemory.transport);
        let events: contracts.KernelEventEnvelope[] = [];
        inMemory.transport.run();
        kernel.subscribeToKernelEvents((e) => events.push(e));

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+2" } })

        expect(events[0]).to.be.deep.eq({
            eventType: contracts.ValueProducedType,
            event: {
                name: "a",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable a"
                }
            },
            command: { ...inMemory.sentItems[0], ["command.id"]: "newId" }
        });

        expect(events[1]).to.be.deep.eq({
            eventType: contracts.ValueProducedType,
            event: {
                name: "b",
                formattedValue: {
                    mimeType: "text/plain",
                    value: "variable b"
                }
            },
            command: { ...inMemory.sentItems[0], ["command.id"]: "newId" }
        });

        expect(events[2]).to.include({
            eventType: contracts.CommandSucceededType,
            command: inMemory.sentItems[0]
        });
    });
});


function createInMemoryTransport(eventProducer?: (commandEnvelope: contracts.KernelCommandEnvelope) => contracts.KernelEventEnvelope[]): { transport: GenericTransport, sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] } {
    let sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    if (!eventProducer) {
        eventProducer = (ce) => {
            return [{ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: ce }];
        }
    }

    const receiver = new CommandAndEventReceiver();
    let sender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems.push(item);
        let events = eventProducer(<contracts.KernelCommandEnvelope>item)
        for (let event of events) {
            receiver.delegate(event);
        }
        return Promise.resolve();
    }
    let transport = new GenericTransport(
        sender,
        () => {
            return receiver.read();
        }
    );
    return {
        transport,
        sentItems
    };
}