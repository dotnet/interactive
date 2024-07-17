// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { JavascriptKernel } from "../src/javascriptKernel";
import * as commandsAndEvents from "../src/commandsAndEvents";
import { CompositeKernel } from "../src/compositeKernel";
import { KernelHost } from "../src/kernelHost";
import { clearTokenAndId, createInMemoryChannels } from "./testSupport";
import { Kernel } from "../src/kernel";

describe("kernelInfo", () => {
    describe("for composite kernel", () => {
        it("returns kernel info for all children and its own", async () => {
            const compositeKernel = new CompositeKernel("root");
            compositeKernel.add(new JavascriptKernel("child1"), ["child1Js"]);
            compositeKernel.add(new JavascriptKernel("child2"), ["child2Js"]);
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const sub = compositeKernel.subscribeToKernelEvents((event) => events.push(event));
            const requestKernelInfo = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, {});
            await compositeKernel.send(requestKernelInfo);

            sub.dispose();
            const kernelInfos = events.filter(e => e.eventType === commandsAndEvents.KernelInfoProducedType).map(e => (<commandsAndEvents.KernelInfoProduced>(e.event)).kernelInfo);

            expect(kernelInfos.length).to.equal(3);
            expect(kernelInfos).to.deep.equal([{
                aliases: [],
                displayName: 'root',
                isComposite: true,
                isProxy: false,
                languageName: undefined,
                languageVersion: undefined,
                localName: 'root',
                supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                uri: 'kernel://local/root'
            },
            {
                aliases: ['child1Js'],
                description: 'This Kernel is for executing JavaScript code.',
                displayName: 'child1 - JavaScript',
                isComposite: false,
                isProxy: false,
                languageName: 'JavaScript',
                languageVersion: undefined,
                localName: 'child1',
                supportedKernelCommands:
                    [{ name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' },
                    { name: 'SendValue' }],
                uri: 'kernel://local/root/child1'
            },
            {
                aliases: ['child2Js'],
                description: 'This Kernel is for executing JavaScript code.',
                displayName: 'child2 - JavaScript',
                isComposite: false,
                isProxy: false,
                languageName: 'JavaScript',
                languageVersion: undefined,
                localName: 'child2',
                supportedKernelCommands:
                    [{ name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' },
                    { name: 'SendValue' }],
                uri: 'kernel://local/root/child2'
            }]);
        });

        it("unproxied kernels have a URI", async () => {
            const kernel = new CompositeKernel("root");
            let inMemory = createInMemoryChannels();
            const host = new KernelHost(kernel, inMemory.local.sender, inMemory.local.receiver, "kernel://local");
            kernel.add(new JavascriptKernel("child1"), ["child1Js"]);
            kernel.add(new JavascriptKernel("child2"), ["child2Js"]);
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            const requestKernelInfo = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, {});
            await kernel.send(requestKernelInfo);

            sub.dispose();
            const kernelInfos = events.filter(e => e.eventType === commandsAndEvents.KernelInfoProducedType).map(e => (<commandsAndEvents.KernelInfoProduced>(e.event)).kernelInfo.uri);

            expect(kernelInfos.length).to.equal(3);
            expect(kernelInfos).to.deep.equal([
                'kernel://local/',
                'kernel://local/child1',
                'kernel://local/child2']);
        });

        it("when kernels are added it produces KernelInfoProduced events", () => {
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const kernel = new CompositeKernel("root");
            kernel.kernelEvents.subscribe({
                next: (event) => {
                    events.push(event);
                }
            });

            kernel.add(new JavascriptKernel("child1"), ["child1Js"]);

            expect(events.map(e => e.toJson())).to.deep.equal([{
                command: undefined,
                event:
                {
                    kernelInfo:
                    {
                        aliases: ['child1Js'],
                        description: 'This Kernel is for executing JavaScript code.',
                        displayName: 'child1 - JavaScript',
                        isComposite: false,
                        isProxy: false,
                        languageName: 'JavaScript',
                        languageVersion: undefined,
                        localName: 'child1',
                        supportedKernelCommands:
                            [{ name: 'RequestKernelInfo' },
                            { name: 'SubmitCode' },
                            { name: 'RequestValueInfos' },
                            { name: 'RequestValue' },
                            { name: 'SendValue' }],
                        uri: 'kernel://local/root/child1'
                    }
                },
                eventType: 'KernelInfoProduced',
                routingSlip: []
            }]);
        });

        it("when a custom command is added during command execution it produces and updatedKernelInfoProduced event", async () => {
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const kernel = new CompositeKernel("root");
            const childKernel = new Kernel("child1", "customLanguage");
            childKernel.registerCommandHandler({
                commandType: commandsAndEvents.SubmitCodeType,
                handle: (_commandInvocation) => {
                    childKernel.registerCommandHandler({
                        commandType: "customCommand",
                        handle: (_commandInvocation) => {
                            return Promise.resolve();
                        }
                    });
                    return Promise.resolve();
                }
            });

            kernel.add(childKernel);

            kernel.kernelEvents.subscribe({
                next: (event) => {
                    events.push(event);
                }
            });

            await kernel.send(new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { targetKernelName: "child1" }));
            expect(events.filter(e => e.eventType === commandsAndEvents.KernelInfoProducedType).map(e => clearTokenAndId(e.toJson())))
                .to
                .deep
                .equal([{
                    command:
                    {
                        command: { targetKernelName: 'child1' },
                        commandType: 'SubmitCode',
                        routingSlip:
                            ['kernel://local/root?tag=arrived',
                                'kernel://local/root/child1?tag=arrived',
                                'kernel://local/root/child1',
                                'kernel://local/root']
                    },
                    event:
                    {
                        kernelInfo:
                        {
                            aliases: [],
                            displayName: 'child1',
                            isComposite: false,
                            isProxy: false,
                            languageName: 'customLanguage',
                            languageVersion: undefined,
                            localName: 'child1',
                            supportedKernelCommands:
                                [{ name: 'RequestKernelInfo' },
                                { name: 'SubmitCode' },
                                { name: 'customCommand' }],
                            uri: 'kernel://local/root/child1'
                        }
                    },
                    eventType: 'KernelInfoProduced',
                    routingSlip: ['kernel://local/root/child1', 'kernel://local/root']
                }]);

        });

        it("when a custom command is added it produces and updatedKernelInfoProduced event", async () => {
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const kernel = new CompositeKernel("root");
            const childKernel = new Kernel("child1", "customLanguage");

            kernel.add(childKernel);

            kernel.kernelEvents.subscribe({
                next: (event) => {
                    events.push(event);
                }
            });

            childKernel.registerCommandHandler({
                commandType: commandsAndEvents.SubmitCodeType,
                handle: (_commandInvocation) => {
                    return Promise.resolve();
                }
            });

            expect(events.filter(e => e.eventType === commandsAndEvents.KernelInfoProducedType).map(e => clearTokenAndId(e.toJson())))
                .to
                .deep
                .equal([{
                    command: undefined,
                    event:
                    {
                        kernelInfo:
                        {
                            aliases: [],
                            displayName: 'child1',
                            isComposite: false,
                            isProxy: false,
                            languageName: 'customLanguage',
                            languageVersion: undefined,
                            localName: 'child1',
                            supportedKernelCommands: [{ name: 'RequestKernelInfo' }, { name: 'SubmitCode' }],
                            uri: 'kernel://local/root/child1'
                        }
                    },
                    eventType: 'KernelInfoProduced',
                    routingSlip: ['kernel://local/root/child1', 'kernel://local/root']
                }]);
        });

        it("when commands adde a kernel it produces KernelInfoProduced events", async () => {
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const kernel = new CompositeKernel("root");
            const childKernel = new Kernel("child1", "customLanguage");

            childKernel.registerCommandHandler({
                commandType: commandsAndEvents.SubmitCodeType,
                handle: (commandInvocation) => {
                    commandInvocation.commandEnvelope;//?
                    commandInvocation.context.commandEnvelope;//?
                    commandInvocation.context.handlingKernel?.rootKernel;//?
                    const composite = <CompositeKernel>commandInvocation.context.handlingKernel?.rootKernel;
                    if (!composite) {
                        return Promise.reject(new Error("No composite kernel"));
                    };
                    try {
                        composite.add(new Kernel("child2", "customLanguage"), ["child2Js"]);
                        return Promise.resolve();
                    }
                    catch (error) {
                        return Promise.reject(error);
                    }
                }
            });

            kernel.add(childKernel);

            kernel.kernelEvents.subscribe({
                next: (event) => {
                    events.push(event);
                }
            });

            const code = `kernel.root.add(new Kernel("child2", "customLanguage"), ["child2Js"]);`;
            await kernel.send(new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: code, targetKernelName: "child1" }));

            expect(events.filter(e => e.eventType === commandsAndEvents.KernelInfoProducedType).map(e => clearTokenAndId(e.toJson())))
                .to
                .deep
                .equal([{
                    command:
                    {
                        command:
                        {
                            code: 'kernel.root.add(new Kernel("child2", "customLanguage"), ["child2Js"]);',
                            targetKernelName: 'child1'
                        },
                        commandType: 'SubmitCode',
                        routingSlip:
                            ['kernel://local/root?tag=arrived',
                                'kernel://local/root/child1?tag=arrived',
                                'kernel://local/root/child1',
                                'kernel://local/root']
                    },
                    event:
                    {
                        kernelInfo:
                        {
                            aliases: ['child2Js'],
                            displayName: 'child2',
                            isComposite: false,
                            isProxy: false,
                            languageName: 'customLanguage',
                            languageVersion: undefined,
                            localName: 'child2',
                            supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                            uri: 'kernel://local/root/child2'
                        }
                    },
                    eventType: 'KernelInfoProduced',
                    routingSlip: ['kernel://local/root/child1', 'kernel://local/root']
                }]);
        });

    });

    describe("for unparented kernel", () => {
        it("returns the list of instrinsict kernel commands", async () => {
            const kernel = new JavascriptKernel();
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send(new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, {}));

            sub.dispose();
            const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>events.find(e => e.eventType === commandsAndEvents.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.supportedKernelCommands).to.deep.equal(
                [
                    { name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' },
                    { name: 'SendValue' }
                ]);
        });

        it("returns the language info for javascript", async () => {
            const kernel = new JavascriptKernel();
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send(new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, {}));
            sub.dispose();
            const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>events.find(e => e.eventType === commandsAndEvents.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.languageName).to.equal("JavaScript");

        });

        it("returns the list of dynamic kernel commands", async () => {
            const kernel = new JavascriptKernel();
            const events: commandsAndEvents.KernelEventEnvelope[] = [];
            kernel.registerCommandHandler({
                commandType: "TestCommand1",
                handle: (_invocation) => Promise.resolve()
            });

            kernel.registerCommandHandler({
                commandType: "TestCommand2",
                handle: (_invocation) => Promise.resolve()
            });

            kernel.registerCommandHandler({
                commandType: "TestCommand3",
                handle: (_invocation) => Promise.resolve()
            });
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send(new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestKernelInfoType, {}));
            sub.dispose();
            const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>events.find(e => e.eventType === commandsAndEvents.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.supportedKernelCommands).to.deep.equal(
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' },
                { name: 'SendValue' },
                { name: 'TestCommand1' },
                { name: 'TestCommand2' },
                { name: 'TestCommand3' }]);
        });
    });
});