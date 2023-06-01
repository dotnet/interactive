// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import * as commandsAndEvents from "../src/commandsAndEvents";
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

        it("provides uri for kernels and produces events as it connects", () => {
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

            expect(inMemory.local.messagesSent).to.deep
                .equal([{
                    _routingSlip: { _uris: ['kernel://vscode/'] },
                    command: undefined,
                    event:
                    {
                        kernelInfos:
                            [{
                                aliases: [],
                                displayName: 'vscode',
                                isComposite: true,
                                isProxy: false,
                                localName: 'vscode',
                                supportedDirectives: [],
                                supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                                uri: 'kernel://vscode/'
                            },
                            {
                                aliases: ['test1', 'test2'],
                                displayName: 'test',
                                isComposite: false,
                                isProxy: false,
                                languageName: 'customLanguage',
                                localName: 'test',
                                supportedDirectives: [],
                                supportedKernelCommands: [{ name: 'RequestKernelInfo' }, { name: 'customCommand' }],
                                uri: 'kernel://vscode/test'
                            }]
                    },
                    eventType: 'KernelReady'
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


        it("provides uri for root kernel", () => {
            const inMemory = createInMemoryChannels();
            const compositeKernel = new CompositeKernel("vscode");
            const kernelHost = new KernelHost(compositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://vscode");

            const childKernel = new Kernel("test");
            compositeKernel.add(childKernel, ["test1", "test2"]);


            const vscodeKernel = compositeKernel.findKernelByUri("kernel://vscode/");//?
            expect(vscodeKernel).not.to.be.undefined;

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
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
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
                commandType: commandsAndEvents.SubmitCodeType, handle: (invocation) => {
                    const returnValueProduced = new commandsAndEvents.KernelEventEnvelope(
                        commandsAndEvents.ReturnValueProducedType,
                        {
                            formattedValues: [
                                {
                                    mimeType: "text/plain",
                                    value: "12"
                                }
                            ]
                        },
                        invocation.commandEnvelope
                    );

                    invocation.context.publish(returnValueProduced);
                    return Promise.resolve();
                }
            });

            go.registerCommandHandler({
                commandType: commandsAndEvents.SubmitCodeType, handle: (invocation) => {
                    const returnValueProduced = new commandsAndEvents.KernelEventEnvelope(
                        commandsAndEvents.ReturnValueProducedType,
                        {
                            formattedValues: [
                                {
                                    mimeType: "text/plain",
                                    value: "21"
                                }
                            ]
                        },
                        invocation.commandEnvelope
                    );

                    invocation.context.publish(returnValueProduced);
                    return Promise.resolve();
                }
            });

            const remoteHost = new KernelHost(remote, inMemory.remote.sender, inMemory.remote.receiver, "kernel://remote");

            vscodeHost.connect();
            remoteHost.connect();

            const command1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "pytonCode", targetKernelName: "python" });
            const command2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: "goCode", targetKernelName: "go" });
            await vscodeKernel.send(command1);
            await vscodeKernel.send(command2);


            expect(events.find(e => e.command!.command.targetKernelName === "python")).not.to.be.undefined;
            expect(events.find(e => e.command!.command.targetKernelName === "go")).not.to.be.undefined;
        });
    }
);