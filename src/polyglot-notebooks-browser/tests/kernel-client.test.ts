// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/kernel-client-impl";
import * as fetchMock from "fetch-mock";
import { configureFetchForKernelDiscovery, createMockChannel, MockKernelCommandAndEventChannel, asKernelClientContainer, delay } from "./testSupport";
import * as commandsAndEvents from "../src/polyglot-notebooks/commandsAndEvents";
import { IKernelCommandInvocation, Kernel } from "../src/polyglot-notebooks/kernel";
import { attachKernelToChannel } from "../src/kernel-factory";
import { KernelInvocationContext } from "../src/polyglot-notebooks/kernelInvocationContext";


interface CustomCommand extends commandsAndEvents.KernelCommand {
    data: string
}

interface CustomCommand2 extends commandsAndEvents.KernelCommand {
    moreData: string
}

describe("polyglot-notebooks", () => {


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
                    transport = mock as MockKernelCommandAndEventChannel;
                    return mock;
                }
            });

            let csharpKernel = asKernelClientContainer(client).csharp;

            await csharpKernel.submitCode("var a = 12");
            expect(transport!.commandsSent[0].command.targetKernelName).to.be.equal("csharp");
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
                        transport = mock as MockKernelCommandAndEventChannel;
                        return mock;
                    }
                });
                let token = await client.submitCode("var a = 12");
                expect(token).to.be.equal(transport!.commandsSent[0].getOrCreateToken());
            });

            it("sends SubmitCode command", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let transport: MockKernelCommandAndEventChannel | undefined;

                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    channelFactory: async (url: string) => {
                        let mock = await createMockChannel(url);
                        transport = mock as MockKernelCommandAndEventChannel;
                        return mock;
                    }
                });

                await client.submitCode("var a = 12");
                expect(transport!.commandsSent[0].commandType).to.be.equal(commandsAndEvents.SubmitCodeType);
            });
        });
    });

    describe("client-side commands", () => {
        afterEach(() => fetchMock.restore());

        // Set up with fake client-side kernel
        const rootUrl = "https://dotnet.interactive.com:999";
        let transport: MockKernelCommandAndEventChannel | undefined;
        let kernel: Kernel | undefined;
        let commandsSentToKernel: commandsAndEvents.KernelCommandEnvelope[] = [];
        let kernelEventHandlers: commandsAndEvents.KernelEventEnvelopeObserver[] = [];
        let registeredCommandHandlers: { [commandType: string]: ((kernelCommandInvocation: { command: commandsAndEvents.KernelCommand, context: KernelInvocationContext }) => Promise<void>) } = {};

        let makeClient = () => {
            configureFetchForKernelDiscovery(rootUrl);
            return createDotnetInteractiveClient({
                address: rootUrl,
                channelFactory: async (url: string) => {
                    let mock = await createMockChannel(url);
                    transport = mock as MockKernelCommandAndEventChannel;
                    return mock;
                },
                clientSideKernelFactory: async (kernelTransport) => {

                    kernel = new Kernel("client-side-kernel");
                    kernel.registerCommandHandler({
                        commandType: "CustomCommand",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel!.push(commandInvocation.commandEnvelope);
                            commandsSentToKernel;//?
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
            let commandType: commandsAndEvents.KernelCommandType = "CustomCommand" as commandsAndEvents.KernelCommandType;
            let commandEnvelopeIn = new commandsAndEvents.KernelCommandEnvelope(
                commandType,
                commandIn
            );

            transport!.fakeIncomingSubmitCommand(commandEnvelopeIn);
            await delay(500);
            commandsSentToKernel;//?


            expect(commandsSentToKernel!.length).to.equal(1);
            expect(commandsSentToKernel![0].commandType).to.equal("CustomCommand");
            let commandReceived = commandsSentToKernel![0].command as CustomCommand;
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
                commandType: commandsAndEvents.SubmitCodeType, handle: (invocation) => {
                    let submitCode = invocation.commandEnvelope.command as commandsAndEvents.SubmitCode;
                    let event = new commandsAndEvents.KernelEventEnvelope(
                        commandsAndEvents.CodeSubmissionReceivedType,
                        {
                            code: submitCode.code
                        },
                        invocation.commandEnvelope
                    );

                    invocation.context.publish(event);
                    return Promise.resolve();
                }
            });

            const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "39 + 3" } as commandsAndEvents.SubmitCode);
            transport!.fakeIncomingSubmitCommand(command);

            let eventIn: commandsAndEvents.CodeSubmissionReceived = {
                code: "39 + 3"
            };

            let eventEnvelopeIn = new commandsAndEvents.KernelEventEnvelope(
                commandsAndEvents.CodeSubmissionReceivedType,
                eventIn
            );

            await delay(500);
            const publishedEvents = transport!.eventsPublished.filter(e => e.eventType === eventEnvelopeIn.eventType);
            expect(publishedEvents.length).to.equal(1);
            expect(publishedEvents[0].eventType).to.be.equal(eventEnvelopeIn.eventType);
            const eventPublished = publishedEvents[0].event as commandsAndEvents.CodeSubmissionReceived;
            expect(eventPublished.code).to.be.equal(eventIn.code);
        });

        // Do we need to handle get variable(s) requests?
    });
});
