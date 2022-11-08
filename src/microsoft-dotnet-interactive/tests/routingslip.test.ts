// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import { ProxyKernel } from "../src/proxyKernel";
import { Logger } from "../src/logger";
import * as rxjs from "rxjs";
import * as connection from "../src/connection";

describe("kernel event routingSlip", () => {
    it("cannot stamp twice", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {}
        };

        connection.stampEventRoutingSlip(envelope, "kernel://1");

        expect(() => connection.stampEventRoutingSlip(envelope, "kernel://1/")).to.throw("The uri kernel://1/ is already in the routing slip");
    });

    it("can append a routing slip to another", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://1/", "kernel://2/"]
        };

        let other = ["kernel://3/", "kernel://4/"];

        connection.appendToEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://1/', 'kernel://2/', 'kernel://3/', 'kernel://4/']);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://1/", "kernel://2/"]
        };

        let other = ["kernel://1/", "kernel://2/", "kernel://3/", "kernel://4/"];

        connection.appendToEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://1/', 'kernel://2/', 'kernel://3/', 'kernel://4/']);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://1/", "kernel://2/"]
        };

        let other = ["kernel://1/", "kernel://3/", "kernel://4/", "kernel://5/"];
        expect(() => connection.appendToEventRoutingSlip(envelope, other)).to.throw("The uri kernel://1/ is already in the routing slip");
    });
});