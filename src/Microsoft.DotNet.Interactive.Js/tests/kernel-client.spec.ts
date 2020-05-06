// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/dotnet-interactive/kernel-client-impl";
import * as fetchMock from "fetch-mock";
import { configureFetchForKernelDiscovery, createMockKernelTransport, MockKernelTransport, asKernelClientContainer } from "./testSupport";
import { SubmitCodeType } from "../src/dotnet-interactive/contracts";

describe("dotnet-interactive", () => {
    describe("langauge kernel", () => {
        afterEach(fetchMock.restore);

        it("can submit code", async () => {
            const rootUrl = "https://dotnet.interactive.com:999";
            configureFetchForKernelDiscovery(rootUrl);

            let transport: MockKernelTransport = null;

            let client = await createDotnetInteractiveClient({
                address: rootUrl,
                kernelTransportFactory: async (url: string) => {
                    let mock = await createMockKernelTransport(url);
                    transport = <MockKernelTransport>mock;
                    return mock;
                }
            });

            let csharpKernel = asKernelClientContainer(client).csharp;

            await csharpKernel.submitCode("var a = 12");
            expect(transport.codeSubmissions[0].command.targetKernelName).to.be.equal("csharp");
        });


    });
    describe("kernel client", () => {
        describe("submitCode", () => {
            afterEach(fetchMock.restore);

            it("returns token for correlation", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let transport: MockKernelTransport = null;

                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    kernelTransportFactory: async (url: string) => {
                        let mock = await createMockKernelTransport(url);
                        transport = <MockKernelTransport>mock;
                        return mock;
                    }
                });
                let token = await client.submitCode("var a = 12");
                expect(token).to.be.equal(transport.codeSubmissions[0].token);
            });

            it("sends SubmitCode command", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let transport: MockKernelTransport = null;

                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    kernelTransportFactory: async (url: string) => {
                        let mock = await createMockKernelTransport(url);
                        transport = <MockKernelTransport>mock;
                        return mock;
                    }
                });

                await client.submitCode("var a = 12");
                expect(transport.codeSubmissions[0].commandType).to.be.equal(SubmitCodeType);
            });
        });
    });
});
