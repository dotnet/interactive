// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/dotnet-interactive/kernel-client-impl";
import * as fetchMock from "fetch-mock";
import { asKernelClientContainer, createMockKernelTransport } from "./testSupprot";

describe("dotnet-interactive", () => {
    describe("kernel discovery contract", () => {

        afterEach(fetchMock.restore);

        it("creates kernel clients for all discovered kernels", async () => {
            const rootUrl = "https://dotnet.interactive.com:999";
            const expectedKernels = [".NET", "csharp", "fsharp", "powershell", "javascript", "html"];
            fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
            let client = asKernelClientContainer(await createDotnetInteractiveClient({ 
                address: rootUrl, 
                kernelTransportFactory: createMockKernelTransport}));

            for (let kernelName of expectedKernels) {
                expect(client[kernelName]).not.to.be.undefined;
            }
        });
    });
});