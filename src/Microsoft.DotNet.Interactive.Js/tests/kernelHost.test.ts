// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/common/interfaces/contracts";
import { CompositeKernel } from "../src/common/interactive/compositeKernel";
import { Kernel } from "../src/common/interactive/kernel";
import { createInMemoryTransport, findEventFromKernel } from "./testSupport";
import { Logger } from "../src/common/logger";
import { KernelHost } from "../src/common/interactive/kernelHost";

describe("kernelHost",

    () => {
        before(() => {
            Logger.configure("test", () => { });
        });

        it("provides uri for kernels", () => {
            const inMemory = createInMemoryTransport();
            const compositeKernel = new CompositeKernel("vscode");
            const kernelHost = new KernelHost(compositeKernel, inMemory.transport, "kernel://vscode");

            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelInfo = kernelHost.tryGetKernelInfo(childKernel);

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo.originUri).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.equal("kernel://vscode/test");
            expect(kernelInfo.aliases).to.be.deep.eq(["test1", "test2"]);

        });

        it("provides uri for kernels as it is attached to composite kernel", () => {
            const inMemory = createInMemoryTransport();
            const compositeKernel = new CompositeKernel("vscode");


            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelHost = new KernelHost(compositeKernel, inMemory.transport, "kernel://vscode");
            const kernelInfo = kernelHost.tryGetKernelInfo(childKernel);

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo.originUri).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.equal("kernel://vscode/test");
            expect(kernelInfo.aliases).to.be.deep.eq(["test1", "test2"]);

        });
    }
);