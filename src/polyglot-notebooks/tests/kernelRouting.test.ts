// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from "../src/compositeKernel";
import { JavascriptKernel } from "../src/javascriptKernel";
import * as commandsAndEvents from "../src/commandsAndEvents";
import { createInMemoryChannels } from "./testSupport";
import { KernelHost } from "../src/kernelHost";

describe("kernelRouting", () => {
    it("commands routing slip contains kernels that have been traversed", async () => {
        let composite = new CompositeKernel("vscode");
        composite.add(new JavascriptKernel("javascript"));
        composite.add(new JavascriptKernel("typescript"));

        composite.defaultKernelName = "typescript";

        let command: commandsAndEvents.KernelCommandEnvelope = {
            commandType: commandsAndEvents.SubmitCodeType,
            command: <commandsAndEvents.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        composite.subscribeToKernelEvents(e => events.push(e));

        await composite.send(command);
        events;//?
        const succededEvent = events.find(e => e.eventType === commandsAndEvents.CommandSucceededType);

        expect(succededEvent).not.to.be.undefined;

        succededEvent;//?
        expect(Array.from(succededEvent!.command!.routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/vscode?tag=arrived',
                'kernel://local/vscode/javascript?tag=arrived',
                'kernel://local/vscode/javascript',
                'kernel://local/vscode'
            ]);
    });

    it("event routing slip contains kernels that have been traversed", async () => {
        let composite = new CompositeKernel("vscode");
        composite.add(new JavascriptKernel("javascript"));
        composite.add(new JavascriptKernel("typescript"));

        composite.defaultKernelName = "typescript";

        let command: commandsAndEvents.KernelCommandEnvelope = {
            commandType: commandsAndEvents.SubmitCodeType,
            command: <commandsAndEvents.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        composite.subscribeToKernelEvents(e => events.push(e));

        await composite.send(command);

        expect(events[0].routingSlip).not.to.be.undefined;

        expect(Array.from(events[0].routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/vscode/javascript',
                'kernel://local/vscode'
            ]);
    });

    it("commands routing slip contains proxy kernels that have been traversed", async () => {
        let remoteCompositeKernel = new CompositeKernel("remote-kernel");
        remoteCompositeKernel.add(new JavascriptKernel("javascript"));
        remoteCompositeKernel.add(new JavascriptKernel("typescript"));

        remoteCompositeKernel.defaultKernelName = "typescript";

        let command: commandsAndEvents.KernelCommandEnvelope = {
            commandType: commandsAndEvents.SubmitCodeType,
            command: <commandsAndEvents.SubmitCode>{
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

        let events: commandsAndEvents.KernelEventEnvelope[] = [];

        localCompositeKernel.kernelEvents.subscribe(e => events.push(e));

        await localCompositeKernel.send(command);

        expect(command.routingSlip).not.to.be.undefined;

        expect(Array.from(command.routingSlip!.values())).to.deep.equal(
            [
                'kernel://local/?tag=arrived',
                'kernel://local/javascript?tag=arrived',
                'kernel://remote/?tag=arrived',
                'kernel://remote/javascript?tag=arrived',
                'kernel://local/javascript',
                'kernel://local/'
            ]);
    });

    it.skip("event routing slip contains proxy kernels that have been traversed", async () => {
        let remoteCompositeKernel = new CompositeKernel("remote-kernel");
        remoteCompositeKernel.add(new JavascriptKernel("javascript"));
        remoteCompositeKernel.add(new JavascriptKernel("typescript"));

        remoteCompositeKernel.defaultKernelName = "typescript";

        let command: commandsAndEvents.KernelCommandEnvelope = {
            commandType: commandsAndEvents.SubmitCodeType,
            command: <commandsAndEvents.SubmitCode>{
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

        let events: commandsAndEvents.KernelEventEnvelope[] = [];

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