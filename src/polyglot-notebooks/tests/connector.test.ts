// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as connection from "../src/connection";
import * as rxjs from 'rxjs';
import * as commandsAndEvents from "../src/commandsAndEvents";


describe("connector", () => {
    it("tracks remote hosts from incoming events", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const connector = new connection.Connector({
            receiver: connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal),
            sender: connection.KernelCommandAndEventSender.FromObserver(localToRemote),
            remoteUris: ["kernerl://remote1"]
        });

        const event1 = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, {});
        event1.routingSlip.stamp("kernerl://remote2/kernel1");
        const event2 = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CommandSucceededType, {});
        event2.routingSlip.stamp("kernerl://remote2/kernel2");

        remoteToLocal.next(event1);
        remoteToLocal.next(event2);

        expect(connector.remoteHostUris).to.deep.equal(['kernerl://remote1', 'kernerl://remote2']);
    });

    it("can tell is a temote can be reached", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const connector = new connection.Connector({
            receiver: connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal),
            sender: connection.KernelCommandAndEventSender.FromObserver(localToRemote),
            remoteUris: ["kernerl://remote1"]
        });

        expect(connector.canReach("kernerl://remote1")).to.be.true;
    });
});