// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as contracts from "../src/contracts";
import { CompositeKernel } from "../src/compositeKernel";
import { Kernel } from "../src/kernel";
import { createInMemoryChannels } from "./testSupport";
import { Logger } from "../src/logger";
import { KernelHost } from "../src/kernelHost";

describe("kernelHost",

    () => {
        before(() => {
            Logger.configure("test", () => { });
        });

        it("provides uri for kernels", () => {
            const inMemory = createInMemoryChannels();
            const compositeKernel = new CompositeKernel("vscode");
            const childKernel = new Kernel("test", "customLanguage");
            childKernel.registerCommandHandler({
                commandType: "customCommand",
                handle: (_commandInvocation) => { return Promise.resolve(); }
            });
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelHost = new KernelHost(compositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://vscode");
            kernelHost.connect();

            expect(inMemory.local.messagesSent).to.deep.equal([{
                event: {},
                eventType: 'KernelReady',
                routingSlip: ['kernel://vscode/']
            },
            {
                event:
                {
                    kernelInfo:
                    {
                        aliases: [],
                        localName: 'vscode',
                        supportedDirectives: [],
                        supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                        uri: 'kernel://vscode/'
                    }
                },
                eventType: 'KernelInfoProduced',
                routingSlip: ['kernel://vscode/']
            },
            {
                event:
                {
                    kernelInfo:
                    {
                        aliases: ['test1', 'test2'],
                        languageName: 'customLanguage',
                        localName: 'test',
                        supportedDirectives: [],
                        supportedKernelCommands: [{ name: 'RequestKernelInfo' }, { name: 'customCommand' }],
                        uri: 'kernel://vscode/test'
                    }
                },
                eventType: 'KernelInfoProduced',
                routingSlip: ['kernel://vscode/test']
            }]);
        });

        it("provides uri for kernels", () => {
            const inMemory = createInMemoryChannels();
            const compositeKernel = new CompositeKernel("vscode");
            const kernelHost = new KernelHost(compositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://vscode");

            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelInfo = childKernel.kernelInfo;

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo!.uri).to.not.be.undefined;
            expect(kernelInfo!.uri).to.equal("kernel://vscode/test");
            expect(kernelInfo!.aliases).to.be.deep.eq(["test1", "test2"]);

        });

        it("provides uri for kernels as it is attached to composite kernel", () => {
            const inMemory = createInMemoryChannels();
            const compositeKernel = new CompositeKernel("vscode");

            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);

            const kernelHost = new KernelHost(compositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://vscode");
            const kernelInfo = childKernel.kernelInfo;

            expect(kernelInfo).to.not.be.undefined;
            expect(kernelInfo!.uri).to.not.be.undefined;
            expect(kernelInfo!.uri).to.equal("kernel://vscode/test");
            expect(kernelInfo!.aliases).to.be.deep.eq(["test1", "test2"]);

        });

        it("routes commands to the appropriate kernels", async () => {
            const events: contracts.KernelEventEnvelope[] = [];
            const vscodeKernel = new CompositeKernel("composite-kernel");

            const inMemory = createInMemoryChannels();

            const vscodeHost = new KernelHost(vscodeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://vscode");

            vscodeHost.connectProxyKernelOnDefaultConnector("python", "kernel://remote/python");
            vscodeHost.connectProxyKernelOnDefaultConnector("go", "kernel://remote/go");

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

            const remoteHost = new KernelHost(remote, inMemory.remote.sender, inMemory.remote.receiver, "kernel://remote");

            vscodeHost.connect();
            remoteHost.connect();

            await vscodeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "pytonCode", targetKernelName: "python" } });
            await vscodeKernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "goCode", targetKernelName: "go" } });


            expect(events.find(e => e.command!.command.targetKernelName === "python")).not.to.be.undefined;
            expect(events.find(e => e.command!.command.targetKernelName === "go")).not.to.be.undefined;
        });
    }
);