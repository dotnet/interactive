// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import * as connection from "../src/connection";

describe("kernel event routingSlip", () => {
    it("cannot stamp twice", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {}
        };

        connection.stampEventRoutingSlip(envelope, "kernel://a");

        expect(() => connection.stampEventRoutingSlip(envelope, "kernel://a")).to.throw("The uri kernel://a/ is already in the routing slip");
    });

    it("can append a routing slip to another", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://a", "kernel://b"]
        };

        let other = ["kernel://c", "kernel://d"];

        connection.continueEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://a', 'kernel://b', 'kernel://c/', 'kernel://d/']);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://a", "kernel://b"]
        };

        let other = ["kernel://a", "kernel://b", "kernel://c", "kernel://d"];

        connection.continueEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://a', 'kernel://b', 'kernel://c/', 'kernel://d/']);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: ["kernel://a", "kernel://b"]
        };

        let other = ["kernel://a", "kernel://c", "kernel://d", "kernel://e"];
        expect(() => connection.continueEventRoutingSlip(envelope, other)).to.throw("The uri kernel://a/ is already in the routing slip");
    });
});