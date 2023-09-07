// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as commandsAndEvents from "../src/commandsAndEvents";
import * as routingSlip from "../src/routingslip";

describe("kernel event routingSlip", () => {
    it("cannot stamp twice", () => {
        let eventRoutingSlip = new routingSlip.EventRoutingSlip();
        eventRoutingSlip.stamp("kernel://a");

        expect(() => eventRoutingSlip.stamp("kernel://a")).to.throw("The uri kernel://a/ is already in the routing slip [kernel://a/]");
    });

    it("can append a routing slip to another", () => {
        let eventRoutingSlip = new routingSlip.EventRoutingSlip();
        eventRoutingSlip.stamp("kernel://a");
        eventRoutingSlip.stamp("kernel://b");

        let otherEventRoutingSlip = new routingSlip.EventRoutingSlip();
        otherEventRoutingSlip.stamp("kernel://c");
        otherEventRoutingSlip.stamp("kernel://d");

        eventRoutingSlip.continueWith(otherEventRoutingSlip);

        expect(eventRoutingSlip.toArray()).to.deep.equal(['kernel://a/', 'kernel://b/', 'kernel://c/', 'kernel://d/']);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {
        let eventRoutingSlip = new routingSlip.EventRoutingSlip();
        eventRoutingSlip.stamp("kernel://a");
        eventRoutingSlip.stamp("kernel://b");

        let otherEventRoutingSlip = new routingSlip.EventRoutingSlip();
        otherEventRoutingSlip.stamp("kernel://a");
        otherEventRoutingSlip.stamp("kernel://b");
        otherEventRoutingSlip.stamp("kernel://c");
        otherEventRoutingSlip.stamp("kernel://d");

        eventRoutingSlip.continueWith(otherEventRoutingSlip);

        expect(eventRoutingSlip.toArray()).to.deep.equal(['kernel://a/', 'kernel://b/', 'kernel://c/', 'kernel://d/']);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {

        let eventRoutingSlip = new routingSlip.EventRoutingSlip();
        eventRoutingSlip.stamp("kernel://a");
        eventRoutingSlip.stamp("kernel://b");

        let otherEventRoutingSlip = new routingSlip.EventRoutingSlip();
        otherEventRoutingSlip.stamp("kernel://a");
        otherEventRoutingSlip.stamp("kernel://c");
        otherEventRoutingSlip.stamp("kernel://d");
        otherEventRoutingSlip.stamp("kernel://e");

        expect(() => eventRoutingSlip.continueWith(otherEventRoutingSlip)).to.throw("The uri kernel://a/ is already in the routing slip [kernel://a/,kernel://b/], cannot continue with routing slip [kernel://a/,kernel://c/,kernel://d/,kernel://e/]");
    });
});


describe("kernel command routingSlip", () => {
    it("cannot stamp twice as arrived", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");

        expect(() => commandRoutingSlip.stampAsArrived("kernel://a")).to.throw("The uri kernel://a/?tag=arrived is already in the routing slip [kernel://a/?tag=arrived]");
    });

    it("can be stamped on arrival", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");

        expect(commandRoutingSlip.toArray()).to.deep.equal(['kernel://a/?tag=arrived']);
    });

    it("can be stamped", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");
        commandRoutingSlip.stamp("kernel://a");

        expect(commandRoutingSlip.toArray()).to.deep.equal(['kernel://a/?tag=arrived', 'kernel://a/']);
    });

    it("cannot be stamped if the uri is not stamped as arrived before", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();

        expect(() => commandRoutingSlip.stamp("kernel://a")).to.throw("The uri kernel://a/?tag=arrived is not in the routing slip []");
    });

    it("can append a routing slip to another", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");
        commandRoutingSlip.stampAsArrived("kernel://b");

        let otherCommandRoutingSlip = new routingSlip.CommandRoutingSlip();
        otherCommandRoutingSlip.stampAsArrived("kernel://c");
        otherCommandRoutingSlip.stampAsArrived("kernel://d");

        commandRoutingSlip.continueWith(otherCommandRoutingSlip);

        expect(commandRoutingSlip.toArray()).to.deep.equal([
            'kernel://a/?tag=arrived',
            'kernel://b/?tag=arrived',
            'kernel://c/?tag=arrived',
            'kernel://d/?tag=arrived'
        ]);
    });

    it("can append a routing slip to another if the other starts with the same list of uris", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");
        commandRoutingSlip.stampAsArrived("kernel://b");

        let otherCommandRoutingSlip = new routingSlip.CommandRoutingSlip();
        otherCommandRoutingSlip.stampAsArrived("kernel://a");
        otherCommandRoutingSlip.stampAsArrived("kernel://b");
        otherCommandRoutingSlip.stampAsArrived("kernel://c");
        otherCommandRoutingSlip.stampAsArrived("kernel://d");

        commandRoutingSlip.continueWith(otherCommandRoutingSlip);

        expect(commandRoutingSlip.toArray()).to.deep.equal([
            'kernel://a/?tag=arrived',
            'kernel://b/?tag=arrived',
            'kernel://c/?tag=arrived',
            'kernel://d/?tag=arrived'
        ]);
    });

    it("cannot append a routing slip to another if the other adds in the same kernel", () => {
        let commandRoutingSlip = new routingSlip.CommandRoutingSlip();
        commandRoutingSlip.stampAsArrived("kernel://a");
        commandRoutingSlip.stampAsArrived("kernel://b");

        let otherCommandRoutingSlip = new routingSlip.CommandRoutingSlip();
        otherCommandRoutingSlip.stampAsArrived("kernel://a");
        otherCommandRoutingSlip.stampAsArrived("kernel://c");
        otherCommandRoutingSlip.stampAsArrived("kernel://d");


        expect(() => commandRoutingSlip.continueWith(otherCommandRoutingSlip)).to
            .throw("The uri kernel://a/?tag=arrived is already in the routing slip [kernel://a/?tag=arrived,kernel://b/?tag=arrived], cannot continue with routing slip [kernel://a/?tag=arrived,kernel://c/?tag=arrived,kernel://d/?tag=arrived]");
    });
});