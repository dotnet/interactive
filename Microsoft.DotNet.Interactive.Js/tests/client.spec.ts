// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as interactive from "../src/dotnet-interactive"

describe("client", () => {
    describe("not sure", () => {
        it("runs", () => {
            let client = interactive.createClient();
            let value = client.GetVariable("code");            
            expect(value).to.eq(1);
        });

        it("runs again", () => {
            let client = interactive.createClient();
            let value = client.GetStuff();            
            expect(value).to.eq(1);
        });
    });
});