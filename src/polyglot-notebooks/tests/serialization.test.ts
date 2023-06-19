// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as connection from "../src/connection";
import * as rxjs from 'rxjs';
import * as commandsAndEvents from "../src/commandsAndEvents";


describe("serialization", () => {

    it("can rountrip NaN", () => {
        const rountrip = connection.Deserialize(connection.Serialize(NaN));
        expect(rountrip).to.be.NaN;
    });

    it("can rountrip Infinity", () => {
        const rountrip = connection.Deserialize(connection.Serialize(Infinity));
        expect(rountrip).to.be.equal(Infinity);
    });

    it("can rountrip negative Infinity", () => {
        const rountrip = connection.Deserialize(connection.Serialize(-Infinity));
        expect(rountrip).to.be.equal(-Infinity);
    });
});