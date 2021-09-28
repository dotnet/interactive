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
            eventType: 'CommandSucceeded',
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
            receiver.publish(event);
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