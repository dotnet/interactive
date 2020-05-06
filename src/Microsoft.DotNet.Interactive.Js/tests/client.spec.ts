// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as interactive from "../src/dotnet-interactive"
import * as fetchMock from "fetch-mock";

describe("dotnet-interactive", () => {

    afterEach(fetchMock.restore);
    describe("initialisation", () => {
        it("injects function to create scope for dotnet interactive", () => {
            let global: any = {};
            interactive.init(global);

            expect(typeof (global.getDotnetInteractiveScope))
                .to
                .equal('function');
        });

        it("injects function to create dotnet interactive client", () => {
            let global: any = {};
            interactive.init(global);

            expect(typeof (global.createDotnetInteractiveClient))
                .to
                .equal('function');
        });
    });

    describe("scopes", () => {
        it("can be retrieved", () => {
            let global: any = {};
            interactive.init(global);

            let scope = global.getDotnetInteractiveScope("scopeid");
            expect(scope).not.to.be.null;
            expect(scope).not.to.be.undefined;

        })
    });
});