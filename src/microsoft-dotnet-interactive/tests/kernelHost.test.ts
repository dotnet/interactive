// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import { CompositeKernel } from "../src/compositeKernel";
import { Kernel } from "../src/kernel";
import { createInMemoryChannels, createInMemoryChannel } from "./testSupport";
import { Logger } from "../src/logger";
import { KernelHost } from "../src/kernelHost";

describe("kernelHost",

    () => {
        before(() => {
            Logger.configure("test", () => { });
        });

        it("provides uri for kernels", () => {
            const inMemory = createInMemoryChannel();
            const compositeKernel = new CompositeKernel("vscode");
            const kernelHost = new KernelHost(compositeKernel, inMemory.channel, "kernel://vscode");

            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelInfo = kernelHost.tryGetKernelInfo(childKernel);

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.equal("kernel://vscode/test");
            expect(kernelInfo!.aliases).to.be.deep.eq(["test1", "test2"]);

        });

        it("provides uri for kernels as it is attached to composite kernel", () => {
            const inMemory = createInMemoryChannel();
            const compositeKernel = new CompositeKernel("vscode");


            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelHost = new KernelHost(compositeKernel, inMemory.channel, "kernel://vscode");
            const kernelInfo = kernelHost.tryGetKernelInfo(childKernel);

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.not.be.undefined;
            expect(kernelInfo!.originUri).to.equal("kernel://vscode/test");
            expect(kernelInfo!.aliases).to.be.deep.eq(["test1", "test2"]);

        });

        it("routes commands to the appropriate kernels", async () => {
            const events: contracts.KernelEventEnvelope[] = [];
            const vscodeKernel = new CompositeKernel("composite-kernel");

            const inMemory = createInMemoryChannels();

            const vscodeHost = new KernelHost(vscodeKernel, inMemory.channels[0].channel, "kernel://vscode");

            vscodeHost.createProxyKernelOnDefaultConnector({ localName: "python", destinationUri: "kernel://remote/python" });
            vscodeHost.createProxyKernelOnDefaultConnector({ localName: "go", destinationUri: "kernel://remote/go" });
            vscodeHost.connect();

            vscodeKernel.subscribeToKernelEvents(e => {
                events.push(e);
            });

            const remote = new CompositeKernel("remote-kernel");
            const python = new Kernel("python");
            const go = new Kernel("go");
            remote.add(python);
            remote.add(go);

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

            const remoteHost = new KernelHost(remote, inMemory.channels[1].channel, "kernel://remote");
            remoteHost.connect();

            inMemory.channels[0].channel.run();
            inMemory.channels[1].channel.run();

            await vscodeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "pytonCode", targetKernelName: "python" } });
            await vscodeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "goCode", targetKernelName: "go" } });

            expect(events.find(e => e.command!.command.targetKernelName === "python")).not.to.be.undefined;
            expect(events.find(e => e.command!.command.targetKernelName === "go")).not.to.be.undefined;
        });


    }
);