// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/dotnet-interactive/KernelClientImpl";
import * as fetchMock from "fetch-mock";
import { VariableResponse, VariableRequest, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";
import { asKernelClientContainer } from "./testSupprot";

describe("dotnet-interactive", () => {
    const rootUrl = "https://dotnet.interactive.com:999";
    beforeEach(() => {
        const expectedKernels = require("./Responses/kernlesResponse.json");
        fetchMock.get(`${rootUrl}/kernels`, expectedKernels);
    })
    afterEach(fetchMock.restore);
    describe("Variable api", () => {
        it("car load multiple variables in a single request", async () => {
            let expectedVariables: VariableResponse = {
                csharp:
                {
                    a: 1,
                    b: "2"
                },
                fsharp: {
                    c: 3,
                    d: false
                }
            }

            let varaibleRequest: VariableRequest = {
                csharp: ["a", "b"],
                fsharp: ["c", "d"]
            };

            fetchMock.post(`${rootUrl}/variables`, expectedVariables);

            let client = await createDotnetInteractiveClient(rootUrl);
            let variables = await client.getVariables(varaibleRequest);

            expect(variables).to.deep.eq(expectedVariables);
        });

        it("car load variable using kernel client", async () => {
            let expectedVariable = 123;
            let variableName = "data";
            let kernelName = "csharp";

            fetchMock.get(`${rootUrl}/variables/${kernelName}/${variableName}`,
                {
                    body: expectedVariable,
                    status: 200
                });

            let client = asKernelClientContainer( await createDotnetInteractiveClient(rootUrl) );
            let csharpClient = client[kernelName];
            let variable = await csharpClient.getVariable(variableName);

            expect(variable).to.deep.eq(expectedVariable);
        });
    });
});