// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import { CompositeKernel } from "../src/compositeKernel";
import { Kernel } from "../src/kernel";
import { findEventFromKernel } from "./testSupport";
import { Logger } from "../src/logger";


describe("compositeKernel", () => {

    before(() => {
        Logger.configure("test", () => { });
    });

    it("the first kernel is set as the default", () => {
        const kernel = new CompositeKernel("composite-kernel");
        expect(kernel.defaultKernelName).to.be.undefined;

        kernel.add(new Kernel("javascript"));
        expect(kernel.defaultKernelName).to.equal("javascript");

        kernel.add(new Kernel("perl"));
        expect(kernel.defaultKernelName).to.equal("javascript");
    });

    it("can have child kernels", () => {
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"));

        expect(kernel.childKernels.length).to.be.eq(1);
    });

    it("can retrive kernel by its name", () => {
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"));

        expect(kernel.findKernelByName("javascript")).not.to.be.undefined;
    });

    it("can retrive kernel by alias", () => {
        const kernel = new CompositeKernel("composite-kernel");
        kernel.add(new Kernel("javascript"), ["js"]);

        expect(kernel.findKernelByName("js")).not.to.be.undefined;
    });

    it("routes commands to the appropriate kernels", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const compositeKernel = new CompositeKernel("composite-kernel");
        const python = new Kernel("python");
        const go = new Kernel("go");
        compositeKernel.add(python);
        compositeKernel.add(go);

        python.registerCommandHandler({
            commandType: contracts.SubmitCodeType, handle: (invocation) => {
                return Promise.resolve();
            }
        });

        go.registerCommandHandler({
            commandType: contracts.SubmitCodeType, handle: (invocation) => {
                return Promise.resolve();
            }
        });

        compositeKernel.subscribeToKernelEvents(e => {
            events.push(e);
        });

        await compositeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "pytonCode", targetKernelName: python.name } });
        await compositeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "goCode", targetKernelName: "go" } });

        expect(events.find(e => e.command!.command.targetKernelName === python.name)).not.to.be.undefined;
        expect(events.find(e => e.command!.command.targetKernelName === go.name)).not.to.be.undefined;
    });

    it("can have command handlers", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const compositeKernel = new CompositeKernel("composite-kernel");
        const python = new Kernel("python");
        const go = new Kernel("go");
        compositeKernel.add(python);
        compositeKernel.add(go);

        compositeKernel.registerCommandHandler({
            commandType: contracts.SubmitCodeType, handle: (invocation) => {
                return Promise.resolve();
            }
        });

        compositeKernel.subscribeToKernelEvents(e => {
            events.push(e);
        });

        await compositeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "goCode", targetKernelName: compositeKernel.name } });

        expect(events.find(e => e.command!.command.targetKernelName === compositeKernel.name)).not.to.be.undefined;
    });

    it("publishes events from subkernels kernels", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const compositeKernel = new CompositeKernel("composite-kernel");
        const python = new Kernel("python");
        const go = new Kernel("go");
        compositeKernel.add(python);
        compositeKernel.add(go);

        python.registerCommandHandler({
            commandType: contracts.SubmitCodeType, handle: (invocation) => {
                invocation.context.publish({
                    eventType: contracts.ReturnValueProducedType,
                    event: {
                        formattedValues: [
                            {
                                mimeType: "text/plain",
                                value: "12"
                            }
                        ]
                    },
                    command: invocation.commandEnvelope
                });
                return Promise.resolve();
            }
        });

        go.registerCommandHandler({
            commandType: contracts.SubmitCodeType, handle: (invocation) => {
                invocation.context.publish({
                    eventType: contracts.ReturnValueProducedType,
                    event: {
                        formattedValues: [
                            {
                                mimeType: "text/plain",
                                value: "21"
                            }
                        ]
                    },
                    command: invocation.commandEnvelope
                });
                return Promise.resolve();
            }
        });

        compositeKernel.subscribeToKernelEvents(e => {
            events.push(e);
        });

        await compositeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "pytonCode", targetKernelName: "python" } });
        await compositeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "goCode", targetKernelName: "go" } });

        const pythonReturnValueProduced = findEventFromKernel<contracts.ReturnValueProduced>(events, contracts.ReturnValueProducedType, "python")!;
        const goReturnValueProduced = findEventFromKernel<contracts.ReturnValueProduced>(events, contracts.ReturnValueProducedType, "go")!;
        expect(pythonReturnValueProduced).not.to.be.undefined;
        expect(pythonReturnValueProduced.formattedValues[0].value).to.be.eq("12");
        expect(goReturnValueProduced).not.to.be.undefined;
        expect(goReturnValueProduced.formattedValues[0].value).to.be.eq("21");
    });

    it("routes commands to the appropriate kernels based on command type", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const compositeKernel = new CompositeKernel("composite-kernel");
        const python = new Kernel("python");
        const go = new Kernel("go");
        compositeKernel.add(python);
        compositeKernel.add(go);

        const CustomCommandType = "CustomCommand";
        let handlingKernel: string | undefined = undefined;
        python.registerCommandHandler({
            commandType: CustomCommandType, handle: (invocation) => {
                handlingKernel = invocation.context.handlingKernel?.name;
                return Promise.resolve();
            }
        });

        go.registerCommandHandler({
            commandType: CustomCommandType, handle: (invocation) => {
                handlingKernel = invocation.context.handlingKernel?.name;
                return Promise.resolve();
            }
        });

        compositeKernel.registerCommandHandler({
            commandType: CustomCommandType, handle: (invocation) => {
                handlingKernel = invocation.context.handlingKernel?.name;
                return Promise.resolve();
            }
        });

        compositeKernel.subscribeToKernelEvents(e => {
            events.push(e);
        });

        compositeKernel.setDefaultTargetKernelNameForCommand(<contracts.KernelCommandType>CustomCommandType, compositeKernel.name);
        await compositeKernel.send({ commandType: <contracts.KernelCommandType>CustomCommandType, command: {} });

        expect(handlingKernel).to.be.eq(compositeKernel.name);
    });
});
