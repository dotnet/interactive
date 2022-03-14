// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/kernel-client-impl";
import * as fetchMock from "fetch-mock";
import { configureFetchForKernelDiscovery, createMockChannel, MockKernelCommandAndEventChannel, asKernelClientContainer } from "./testSupport";
import * as contracts from "../src/dotnet-interactive/contracts";
import { IKernelCommandInvocation, Kernel } from "../src/dotnet-interactive/kernel";
import { attachKernelToChannel } from "../src/kernel-factory";
import { KernelInvocationContext } from "../src/dotnet-interactive/kernelInvocationContext";
import { Logger } from "../src/dotnet-interactive/logger";


interface CustomCommand extends contracts.KernelCommand {
    data: string
}

interface CustomCommand2 extends contracts.KernelCommand {
    moreData: string
}

describe("dotnet-interactive", () => {


    describe("langauge kernel", () => {
        afterEach(() => fetchMock.restore());

        it("can submit code", async () => {
            const rootUrl = "https://dotnet.interactive.com:999";
            configureFetchForKernelDiscovery(rootUrl);

            let transport: MockKernelCommandAndEventChannel | undefined;

            let client = await createDotnetInteractiveClient({
                address: rootUrl,
                channelFactory: async (url: string) => {
                    let mock = await createMockChannel(url);
                    transport = <MockKernelCommandAndEventChannel>mock;
                    return mock;
                }
            });

            let csharpKernel = asKernelClientContainer(client).csharp;

            await csharpKernel.submitCode("var a = 12");
            expect(transport!.codeSubmissions[0].command.targetKernelName).to.be.equal("csharp");
        });


    });
    describe("kernel client", () => {
        describe("submitCode", () => {
            afterEach(() => fetchMock.restore());


            it("returns resource url", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    channelFactory: createMockChannel
                });
                let resource = client.getResourceUrl("image.png");
                expect(resource).to.be.equal("https://dotnet.interactive.com:999/resources/image.png");
            });

            it("returns extensions resource url", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    channelFactory: createMockChannel
                });
                let resource = client.getExtensionResourceUrl("customExtension", "image.png");
                expect(resource).to.be.equal("https://dotnet.interactive.com:999/extensions/customExtension/resources/image.png");
            });

            it("returns token for correlation", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let transport: MockKernelCommandAndEventChannel | undefined;

                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    channelFactory: async (url: string) => {
                        let mock = await createMockChannel(url);
                        transport = <MockKernelCommandAndEventChannel>mock;
                        return mock;
                    }
                });
                let token = await client.submitCode("var a = 12");
                expect(token).to.be.equal(transport!.codeSubmissions[0].token);
            });

            it("sends SubmitCode command", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let transport: MockKernelCommandAndEventChannel | undefined;

                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    channelFactory: async (url: string) => {
                        let mock = await createMockChannel(url);
                        transport = <MockKernelCommandAndEventChannel>mock;
                        return mock;
                    }
                });

                await client.submitCode("var a = 12");
                expect(transport!.codeSubmissions[0].commandType).to.be.equal(contracts.SubmitCodeType);
            });
        });
    });

    describe("client-side commands", () => {
        afterEach(() => fetchMock.restore());

        // Set up with fake client-side kernel
        const rootUrl = "https://dotnet.interactive.com:999";
        let transport: MockKernelCommandAndEventChannel | undefined;
        let kernel: Kernel | undefined;
        let commandsSentToKernel: contracts.KernelCommandEnvelope[] | undefined;
        let kernelEventHandlers: contracts.KernelEventEnvelopeObserver[] | undefined;
        let registeredCommandHandlers: { [commandType: string]: ((kernelCommandInvocation: { command: contracts.KernelCommand, context: KernelInvocationContext }) => Promise<void>) } | undefined;

        let makeClient = () => {
            configureFetchForKernelDiscovery(rootUrl);
            return createDotnetInteractiveClient({
                address: rootUrl,
                channelFactory: async (url: string) => {
                    let mock = await createMockChannel(url);
                    transport = <MockKernelCommandAndEventChannel>mock;
                    return mock;
                },
                clientSideKernelFactory: async (kernelTransport) => {
                    commandsSentToKernel = [];
                    kernelEventHandlers = [];
                    registeredCommandHandlers = {};
                    kernel = new Kernel("client-side-kernel");
                    kernel.registerCommandHandler({
                        commandType: "CustomCommand",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel!.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });

                    kernel.registerCommandHandler({
                        commandType: "CustomCommand1",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel!.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });

                    kernel.registerCommandHandler({
                        commandType: "CustomCommand2",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel!.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });
                    attachKernelToChannel(kernel, kernelTransport);

                    return kernel;
                }
            });
        };

        // Deliver command from transport
        it("delivers inbound commands to client-side kernel", async () => {
            await makeClient();
            let commandIn: CustomCommand = {
                data: "Test"
            };
            let commandType: contracts.KernelCommandType = <contracts.KernelCommandType>"CustomCommand";
            let commandEnvelopeIn: contracts.KernelCommandEnvelope = {
                commandType,
                command: commandIn
            };
            transport!.fakeIncomingSubmitCommand(commandEnvelopeIn);

            expect(commandsSentToKernel!.length).to.equal(1);
            expect(commandsSentToKernel![0].commandType).to.equal("CustomCommand");
            let commandReceived = <CustomCommand>commandsSentToKernel![0].command;
            expect(commandReceived.data).to.equal(commandIn.data);

            // TODO: what are the semantics around completion? With client-to-server submitCommand, at
            // what point does the Promise<void> returned by the asynchronous KernelTransport.submitCommand
            // complete? Does that happen as soon as the request to send the message has been queued up with
            // the OS? Or does completion imply that the server has recieved it? Or more, does completion
            // imply that the command has been processed?
        });

        // Raise events from kernel.
        // Verify that they are sent back to the transport.
        // Token?
        it("sends client-side kernel events to the kernel transport", async () => {
            let client = await makeClient();
            client.registerCommandHandler({
                commandType: contracts.SubmitCodeType, handle: (invocation) => {
                    let submitCode = <contracts.SubmitCode>invocation.commandEnvelope.command;
                    let event: contracts.KernelEventEnvelope = {
                        eventType: contracts.CodeSubmissionReceivedType,
                        event: {
                            code: submitCode.code
                        },
                        command: invocation.commandEnvelope
                    };

                    invocation.context.publish(event);
                    return Promise.resolve();
                }
            });


            transport!.fakeIncomingSubmitCommand({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "39 + 3" } });

            let eventIn: contracts.CodeSubmissionReceived = {
                code: "39 + 3"
            };

            let eventEnvelopeIn: contracts.KernelEventEnvelope = {
                eventType: contracts.CodeSubmissionReceivedType,
                event: eventIn
            };

            expect(transport!.publishedEvents.length).to.be.equal(1);
            expect(transport!.publishedEvents[0].eventType).to.be.equal(eventEnvelopeIn.eventType);
            let eventPublished = <contracts.CodeSubmissionReceived>transport!.publishedEvents[0].event;
            expect(eventPublished.code).to.be.equal(eventIn.code);
        });

        // Do we need to handle get variable(s) requests?
    });
});
