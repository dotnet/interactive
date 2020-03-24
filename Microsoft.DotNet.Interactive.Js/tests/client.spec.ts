// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as interactive from "../src/dotnet-interactive"

describe("dotnet-interactive", () => {
    describe("initialisation", () => {
        it("injects function to create scope for dotnet", () => {
            let global : any = {};
            interactive.init(global);

            expect(typeof(global.getDotnetInteractiveScope))
            .to
            .equal('function');
        });

        it("injects function to create client", () => {
            let global : any = {};
            interactive.init(global);

            expect(typeof(global.createDotnetInteractiveClient))
            .to
            .equal('function');
        });
       
    });
});