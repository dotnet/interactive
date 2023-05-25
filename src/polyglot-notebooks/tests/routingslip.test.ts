// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import * as routingSlip from "../src/routingslip";

describe("kernel event routingSlip", () => {
    it("cannot stamp twice", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {}
        };

        routingSlip.stampEventRoutingSlip(envelope, "kernel://a");

        expect(() => routingSlip.stampEventRoutingSlip(envelope, "kernel://a")).to.throw("The uri kernel://a/ is already in the routing slip [kernel://a/]");
    });

    it("can append a routing slip to another", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: routingSlip.createRoutingSlip(["kernel://a", "kernel://b"])
        };

        let other = ["kernel://c", "kernel://d"];

        routingSlip.continueEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://a/', 'kernel://b/', 'kernel://c/', 'kernel://d/']);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: routingSlip.createRoutingSlip(["kernel://a", "kernel://b"])
        };

        let other = ["kernel://a", "kernel://b", "kernel://c", "kernel://d"];

        routingSlip.continueEventRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://a/', 'kernel://b/', 'kernel://c/', 'kernel://d/']);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {

        let envelope: contracts.KernelEventEnvelope = {
            eventType: contracts.CommandSucceededType,
            event: {},
            routingSlip: routingSlip.createRoutingSlip(["kernel://a", "kernel://b"])
        };

        let other = ["kernel://a", "kernel://c", "kernel://d", "kernel://e"];
        expect(() => routingSlip.continueEventRoutingSlip(envelope, other)).to.throw("The uri kernel://a/ is already in the routing slip [kernel://a/,kernel://b/], cannot continue with routing slip [kernel://a/,kernel://c/,kernel://d/,kernel://e/]");
    });
});


describe("kernel command routingSlip", () => {
    it("cannot stamp twice as arrived", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {}
        };

        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a");

        expect(() => routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a")).to.throw("The uri kernel://a/?tag=arrived is already in the routing slip [kernel://a/?tag=arrived]");
    });

    it("can be stamped on arrival", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {}
        };

        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a");

        expect(envelope.routingSlip).to.deep.equal(['kernel://a/?tag=arrived']);
    });

    it("can be stamped", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {}
        };

        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a");
        routingSlip.stampCommandRoutingSlip(envelope, "kernel://a");

        expect(envelope.routingSlip).to.deep.equal(['kernel://a/?tag=arrived', 'kernel://a/']);
    });

    it("cannot be stamped if the uri is not stamped as arrived before", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {},
            routingSlip: []
        };
        expect(() => routingSlip.stampCommandRoutingSlip(envelope, "kernel://a")).to.throw("The uri kernel://a/ is not in the routing slip []");
    });

    it("can append a routing slip to another", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {},
            routingSlip: []
        };

        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a");
        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://b");

        let other = ["kernel://c/?tag=arrived", "kernel://d/?tag=arrived"];

        routingSlip.continueCommandRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal([
            'kernel://a/?tag=arrived',
            'kernel://b/?tag=arrived',
            'kernel://c/?tag=arrived',
            'kernel://d/?tag=arrived'
        ]);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {},
            routingSlip: []
        };
        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://a");
        routingSlip.stampCommandRoutingSlipAsArrived(envelope, "kernel://b");

        let other = ["kernel://a/?tag=arrived", "kernel://b/?tag=arrived", "kernel://c/?tag=arrived", "kernel://d/?tag=arrived", "kernel://d"];

        routingSlip.continueCommandRoutingSlip(envelope, other);

        expect(envelope.routingSlip).to.deep.equal(['kernel://a/?tag=arrived',
            'kernel://b/?tag=arrived',
            'kernel://c/?tag=arrived',
            'kernel://d/?tag=arrived',
            'kernel://d/']);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {

        let envelope: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: {},
            routingSlip: routingSlip.createRoutingSlip(["kernel://a", "kernel://b"])
        };

        let other = ["kernel://a", "kernel://c", "kernel://d", "kernel://e"];
        expect(() => routingSlip.continueCommandRoutingSlip(envelope, other)).to.throw("The uri kernel://a/ is already in the routing slip [kernel://a/,kernel://b/], cannot continue with routing slip [kernel://a/,kernel://c/,kernel://d/,kernel://e/]");
    });
});