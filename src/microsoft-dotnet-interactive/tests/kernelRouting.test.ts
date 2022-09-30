// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from "../src/compositeKernel";
import { JavascriptKernel } from "../src/javascriptKernel";
import * as contracts from "../src/contracts";
import { createInMemoryChannels } from "./testSupport";
import { KernelHost } from "../src/kernelHost";

describe("kernelRouting", () => {
    it("commands routing slip contains kernels that have been traversed", async () => {
        let composite = new CompositeKernel("vscode");
        composite.add(new JavascriptKernel("javascript"));
        composite.add(new JavascriptKernel("typescript"));

        composite.defaultKernelName = "typescript";

        let command: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: <contracts.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        let events: contracts.KernelEventEnvelope[] = [];

        composite.subscribeToKernelEvents(e => events.push(e));

        await composite.send(command);

        expect(command.routingSlip).not.to.be.undefined;

        expect(Array.from(command.routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/javascript',
                'kernel://local/vscode'
            ]);
    });

    it("event routing slip contains kernels that have been traversed", async () => {
        let composite = new CompositeKernel("vscode");
        composite.add(new JavascriptKernel("javascript"));
        composite.add(new JavascriptKernel("typescript"));

        composite.defaultKernelName = "typescript";

        let command: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: <contracts.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        let events: contracts.KernelEventEnvelope[] = [];

        composite.subscribeToKernelEvents(e => events.push(e));

        await composite.send(command);

        expect(events[0].routingSlip).not.to.be.undefined;

        expect(Array.from(events[0].routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/javascript',
                'kernel://local/vscode'
            ]);
    });

    it("commands routing slip contains proxy kernels that have been traversed", async () => {
        let remoteCompositeKernel = new CompositeKernel("remote-kernel");
        remoteCompositeKernel.add(new JavascriptKernel("javascript"));
        remoteCompositeKernel.add(new JavascriptKernel("typescript"));

        remoteCompositeKernel.defaultKernelName = "typescript";

        let command: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: <contracts.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        const inMemory = createInMemoryChannels();

        const remoteHost = new KernelHost(remoteCompositeKernel, inMemory.remote.sender, inMemory.remote.receiver, "kernel://remote");

        let localCompositeKernel = new CompositeKernel("local-kernel");
        const localHost = new KernelHost(localCompositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://local");

        localHost.connectProxyKernelOnDefaultConnector("javascript", "kernel://remote/javascript");
        localHost.connectProxyKernelOnDefaultConnector("typescript", "kernel://remote/typescript");

        remoteHost.connect();
        localHost.connect();

        let events: contracts.KernelEventEnvelope[] = [];

        localCompositeKernel.kernelEvents.subscribe(e => events.push(e));

        await localCompositeKernel.send(command);

        expect(command.routingSlip).not.to.be.undefined;

        expect(Array.from(command.routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/javascript',
                'kernel://remote/javascript',
                'kernel://remote',
                'kernel://local'
            ]);
    });

    // it.only("when hosts have bidirectional proxies RequestKernelInfo is not forwarded back to the host that initiated the request", async () => {
    //     const inMemory = createInMemoryChannels();

    //     let remoteCompositeKernel = new CompositeKernel("remote-kernel");
    //     remoteCompositeKernel.add(new JavascriptKernel("javascript"));
    //     remoteCompositeKernel.defaultKernelName = "javascript";


    //     const remoteHost = new KernelHost(remoteCompositeKernel, inMemory.remote.sender, inMemory.remote.receiver, "kernel://remote");

    //     let localCompositeKernel = new CompositeKernel("local-kernel");
    //     localCompositeKernel.add(new JavascriptKernel("typescript"));
    //     localCompositeKernel.defaultKernelName = "typescript";

    //     const localHost = new KernelHost(localCompositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://local");

    //     localHost.connectProxyKernelOnDefaultConnector("remote-kernel-proxy", "kernel://remote");
    //     remoteHost.connectProxyKernelOnDefaultConnector("local-kernel-proxy", "kernel://local");

    //     remoteHost.connect();
    //     localHost.connect();

    //     let events: contracts.KernelEventEnvelope[] = [];
    //     let command: contracts.KernelCommandEnvelope = {
    //         commandType: contracts.RequestKernelInfoType,
    //         command: <contracts.RequestKernelInfo>{
    //             targetKernelName: localCompositeKernel.name
    //         }
    //     };

    //     localCompositeKernel.kernelEvents.subscribe(e => events.push(e));

    //     await localCompositeKernel.send(command);

    //     expect(command.routingSlip).not.to.be.undefined;

    //     expect(Array.from(command.routingSlip!.values())).to.deep.equal(
    //         ['kernel://local']);
    // });

    it.skip("event routing slip contains proxy kernels that have been traversed", async () => {
        let remoteCompositeKernel = new CompositeKernel("remote-kernel");
        remoteCompositeKernel.add(new JavascriptKernel("javascript"));
        remoteCompositeKernel.add(new JavascriptKernel("typescript"));

        remoteCompositeKernel.defaultKernelName = "typescript";

        let command: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: <contracts.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        const inMemory = createInMemoryChannels();

        const remoteHost = new KernelHost(remoteCompositeKernel, inMemory.remote.sender, inMemory.remote.receiver, "kernel://remote");

        let localCompositeKernel = new CompositeKernel("local-kernel");
        const localHost = new KernelHost(localCompositeKernel, inMemory.local.sender, inMemory.local.receiver, "kernel://local");

        localHost.connectProxyKernelOnDefaultConnector("javascript", "kernel://remote/javascript");
        localHost.connectProxyKernelOnDefaultConnector("typescript", "kernel://remote/typescript");

        remoteHost.connect();
        localHost.connect();

        let events: contracts.KernelEventEnvelope[] = [];

        localCompositeKernel.kernelEvents.subscribe(e => events.push(e));

        await localCompositeKernel.send(command);

        expect(command.routingSlip).not.to.be.undefined;

        expect(Array.from(events[0].routingSlip!.values())).to.deep.equal(
            [
                'kernel://remote/javascript',
                'kernel://remote/',
                'kernel://local/',
                'kernel://local/javascript'
            ]);
    });
});