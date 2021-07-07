// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { createDotnetInteractiveClient } from "../src/dotnet-interactive-browser/kernel-client-impl";
import * as fetchMock from "fetch-mock";
import { configureFetchForKernelDiscovery, createMockKernelTransport, MockKernelTransport, asKernelClientContainer } from "./testSupport";
import { CodeSubmissionReceived, CodeSubmissionReceivedType, KernelCommand, KernelCommandEnvelope, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver, SubmitCodeType } from "../src/common/interfaces/contracts";
import { IKernelCommandInvocation, IKernelInvocationContext, Kernel } from "../src/common/interactive/kernel";
import { attachKernelToTransport } from "../src/dotnet-interactive/kernel-factory";
import { SubmitCode } from "../../dotnet-interactive-vscode/common/interfaces/contracts";


interface CustomCommand extends KernelCommand {
    data: string
}

interface CustomCommand2 extends KernelCommand {
    moreData: string
}

describe("dotnet-interactive", () => {
    describe("langauge kernel", () => {
        afterEach(() => fetchMock.restore());

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
            afterEach(() => fetchMock.restore());


            it("returns resource url", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    kernelTransportFactory: createMockKernelTransport
                });
                let resource = client.getResourceUrl("image.png");
                expect(resource).to.be.equal("https://dotnet.interactive.com:999/resources/image.png");
            });

            it("returns extensions resource url", async () => {
                const rootUrl = "https://dotnet.interactive.com:999";
                configureFetchForKernelDiscovery(rootUrl);


                let client = await createDotnetInteractiveClient({
                    address: rootUrl,
                    kernelTransportFactory: createMockKernelTransport
                });
                let resource = client.getExtensionResourceUrl("customExtension", "image.png");
                expect(resource).to.be.equal("https://dotnet.interactive.com:999/extensions/customExtension/resources/image.png");
            });

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

    describe("client-side commands", () => {
        afterEach(() => fetchMock.restore());

        // Set up with fake client-side kernel
        const rootUrl = "https://dotnet.interactive.com:999";
        let transport: MockKernelTransport = null;
        let kernel: Kernel = null;
        let commandsSentToKernel: KernelCommandEnvelope[] = null;
        let kernelEventHandlers: KernelEventEnvelopeObserver[] = null
        let registeredCommandHandlers: { [commandType: string]: ((kernelCommandInvocation: { command: KernelCommand, context: IKernelInvocationContext }) => Promise<void>) } = null;

        let makeClient = () => {
            configureFetchForKernelDiscovery(rootUrl);
            return createDotnetInteractiveClient({
                address: rootUrl,
                kernelTransportFactory: async (url: string) => {
                    let mock = await createMockKernelTransport(url);
                    transport = <MockKernelTransport>mock;
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
                            commandsSentToKernel.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });

                    kernel.registerCommandHandler({
                        commandType: "CustomCommand1",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });

                    kernel.registerCommandHandler({
                        commandType: "CustomCommand2",
                        handle: (commandInvocation: IKernelCommandInvocation) => {
                            commandsSentToKernel.push(commandInvocation.commandEnvelope);
                            return Promise.resolve();
                        }
                    });
                    attachKernelToTransport(kernel, kernelTransport);

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
            let commandType: KernelCommandType = <KernelCommandType>"CustomCommand";
            let commandEnvelopeIn: KernelCommandEnvelope = {
                commandType,
                command: commandIn
            };
            transport.fakeIncomingSubmitCommand(commandEnvelopeIn);

            expect(commandsSentToKernel.length).to.equal(1);
            expect(commandsSentToKernel[0].commandType).to.equal("CustomCommand");
            let commandReceived = <CustomCommand>commandsSentToKernel[0].command;
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
                commandType: SubmitCodeType, handle: (invocation) => {
                    let submitCode = <SubmitCode>invocation.commandEnvelope.command;
                    let event: KernelEventEnvelope = {
                        eventType: CodeSubmissionReceivedType,
                        event: {
                            code: submitCode.code
                        },
                        command: invocation.commandEnvelope
                    };

                    invocation.context.publish(event);
                    return Promise.resolve();
                }
            });


            transport.fakeIncomingSubmitCommand({ commandType: SubmitCodeType, command: <SubmitCode>{ code: "39 + 3" } });

            let eventIn: CodeSubmissionReceived = {
                code: "39 + 3"
            };

            let eventEnvelopeIn: KernelEventEnvelope = {
                eventType: CodeSubmissionReceivedType,
                event: eventIn
            };

            expect(transport.publishedEvents.length).to.be.equal(1);
            expect(transport.publishedEvents[0].eventType).to.be.equal(eventEnvelopeIn.eventType);
            let eventPublished = <CodeSubmissionReceived>transport.publishedEvents[0].event;
            expect(eventPublished.code).to.be.equal(eventIn.code);
        });

        // Do we need to handle get variable(s) requests?
    });
});
