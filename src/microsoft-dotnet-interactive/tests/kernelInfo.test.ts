// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { JavascriptKernel } from "../src/javascriptKernel";
import * as contracts from "../src/contracts";
import { CompositeKernel } from "../src/compositeKernel";
import { KernelHost } from "../src/kernelHost";
import { createInMemoryChannel } from "./testSupport";

describe("kernelInfo", () => {
    describe("for composite kernel", () => {
        it("returns kernel info for all children", async () => {
            const kernel = new CompositeKernel("root");
            kernel.add(new JavascriptKernel("child1"), ["child1Js"]);
            kernel.add(new JavascriptKernel("child2"), ["child2Js"]);
            const events: contracts.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send({ commandType: contracts.RequestKernelInfoType, command: {} });

            sub.dispose();
            const kernelInfos = events.filter(e => e.eventType === contracts.KernelInfoProducedType).map(e => (<contracts.KernelInfoProduced>(e.event)).kernelInfo);

            expect(kernelInfos.length).to.equal(2);
            expect(kernelInfos).to.deep.equal([{
                aliases: ['child1Js'],
                languageName: 'Javascript',
                languageVersion: undefined,
                localName: 'child1',
                supportedDirectives: [],
                supportedKernelCommands:
                    [{ name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' }]
            },
            {
                aliases: ['child2Js'],
                languageName: 'Javascript',
                languageVersion: undefined,
                localName: 'child2',
                supportedDirectives: [],
                supportedKernelCommands:
                    [{ name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' }]
            }]);
        });

        it("unproxied kernels have a URI", async () => {
            const kernel = new CompositeKernel("root");
            let inMemory = createInMemoryChannel();
            const host = new KernelHost(kernel, inMemory.channel, "kernel://local");
            kernel.add(new JavascriptKernel("child1"), ["child1Js"]);
            kernel.add(new JavascriptKernel("child2"), ["child2Js"]);
            const events: contracts.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send({ commandType: contracts.RequestKernelInfoType, command: {} });

            sub.dispose();
            const kernelInfos = events.filter(e => e.eventType === contracts.KernelInfoProducedType).map(e => (<contracts.KernelInfoProduced>(e.event)).kernelInfo.uri);

            expect(kernelInfos.length).to.equal(2);
            expect(kernelInfos).to.deep.equal(['kernel://local/child1', 'kernel://local/child2']);
        });

    });

    describe("for unparented kernel", () => {
        it("returns the list of instrinsict kernel commands", async () => {
            const kernel = new JavascriptKernel();
            const events: contracts.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send({ commandType: contracts.RequestKernelInfoType, command: {} });

            sub.dispose();
            const kernelInfoProduced = <contracts.KernelInfoProduced>events.find(e => e.eventType === contracts.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.supportedKernelCommands).to.deep.equal(
                [
                    { name: 'RequestKernelInfo' },
                    { name: 'SubmitCode' },
                    { name: 'RequestValueInfos' },
                    { name: 'RequestValue' }
                ]);
        });

        it("returns the language info for javascript", async () => {
            const kernel = new JavascriptKernel();
            const events: contracts.KernelEventEnvelope[] = [];
            const sub = kernel.subscribeToKernelEvents((event) => events.push(event));

            await kernel.send({ commandType: contracts.RequestKernelInfoType, command: {} });
            sub.dispose();
            const kernelInfoProduced = <contracts.KernelInfoProduced>events.find(e => e.eventType === contracts.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.languageName).to.equal("Javascript");

        });

        it("returns the list of dynamic kernel commands", async () => {
            const kernel = new JavascriptKernel();
            const events: contracts.KernelEventEnvelope[] = [];
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

            await kernel.send({ commandType: contracts.RequestKernelInfoType, command: {} });
            sub.dispose();
            const kernelInfoProduced = <contracts.KernelInfoProduced>events.find(e => e.eventType === contracts.KernelInfoProducedType)?.event;
            expect(kernelInfoProduced?.kernelInfo.supportedKernelCommands).to.deep.equal(
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' },
                { name: 'TestCommand1' },
                { name: 'TestCommand2' },
                { name: 'TestCommand3' }]);
        });
    });
});