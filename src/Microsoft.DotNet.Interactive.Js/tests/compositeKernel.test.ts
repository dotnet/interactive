// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { KernelEventEnvelope, SubmitCode, SubmitCodeType } from "../src/common/interfaces/contracts";
import { CompositeKernel } from "../src/common/interactive/compositeKernel";
import { Kernel } from "../src/common/interactive/kernel";


describe("compositeKernel", ()=>{
    it("can have child kernels",()=>{
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"));

        expect( kernel.childKernels.length).to.be.eq(1);
    });

    it("can retrive kernel by its name",()=>{
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"));

        expect(kernel.findKernelByName("javascript")).not.to.be.undefined;
    });

    it("can retrive kernel by alias",()=>{
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"), ["js"]);

        expect(kernel.findKernelByName("js")).not.to.be.undefined;
    });

    it("routes commands to the approprioate kernels",async ()=>{
        const events: KernelEventEnvelope[] = [];
        const compositeKernel = new CompositeKernel("composite-kernel");
        const python = new Kernel("python");       
        const go = new Kernel("go");
        compositeKernel.add(python);
        compositeKernel.add(go);

        python.registerCommandHandler({ commandType: SubmitCodeType, handle: (invocation )=>{
            return Promise.resolve();
        } });

        go.registerCommandHandler({ commandType: SubmitCodeType, handle: (invocation )=>{
            return Promise.resolve();
        } });

        compositeKernel.subscribeToKernelEvents(e =>{
            events.push(e);
        });

        await compositeKernel.send({ commandType: SubmitCodeType, command:<SubmitCode>{code:"pytonCode", targetKernelName: "python"}});
        await compositeKernel.send({ commandType: SubmitCodeType, command:<SubmitCode>{code:"goCode", targetKernelName: "go"}});

        expect(events.find(e => e.command.command.targetKernelName === "python")).not.to.be.undefined;
        expect(events.find(e => e.command.command.targetKernelName === "go")).not.to.be.undefined;
    });
});