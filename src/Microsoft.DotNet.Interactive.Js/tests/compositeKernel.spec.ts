// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from "../src/dotnet-interactive/compositeKernel";
import { FakeKernel } from "./testSupport";

describe("compositeKernel", ()=>{
    it("can have child kernels",()=>{
        const kernel = new CompositeKernel();
        kernel.add(new FakeKernel("javascript"));

        expect( kernel.childKernels.length).to.be.eq(1);
    });

    it("can retrive kernel by its name",()=>{
        const kernel = new CompositeKernel();
        kernel.add(new FakeKernel("javascript"));

        expect(kernel.findKernelByName("javascript")).not.to.be.undefined;
    });

    it("can retrive kernel by alias",()=>{
        const kernel = new CompositeKernel();
        kernel.add(new FakeKernel("javascript"), ["js"]);

        expect(kernel.findKernelByName("js")).not.to.be.undefined;
    });
});