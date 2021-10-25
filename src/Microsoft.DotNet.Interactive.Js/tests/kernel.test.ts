// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as contracts from "../src/common/interfaces/contracts";
import { IKernelCommandInvocation, Kernel } from "../src/common/interactive/kernel";
import { Logger } from "../src/common/logger";

interface CustomCommand1 extends contracts.KernelCommand {
    data: string
}

interface CustomCommand2 extends contracts.KernelCommand {
    moreData: string
}

describe("dotnet-interactive", () => {

    before(() => {
        Logger.configure("test", () => { });
    });

    let commandType1: contracts.KernelCommandType = <contracts.KernelCommandType>"CustomCommand1";
    let commandType2: contracts.KernelCommandType = <contracts.KernelCommandType>"CustomCommand2";

    let makeKernel = async () => {
        let kernel = new Kernel("client-side kernel");
        return kernel;
    };
    describe("client-side kernel", () => {
        it("invokes command handler when type matches", async () => {
            var kernel = await makeKernel();

            let command1In: CustomCommand1 = {
                data: "Test"
            };
            let command2In: CustomCommand2 = {
                moreData: "Test 2"
            };

            let handler1Invocations: IKernelCommandInvocation[] = [];
            let handler2Invocations: IKernelCommandInvocation[] = [];
            kernel.registerCommandHandler({ commandType: commandType1, handle: async (a: IKernelCommandInvocation) => { handler1Invocations.push(a); } })
            kernel.registerCommandHandler({ commandType: commandType2, handle: async (a: IKernelCommandInvocation) => { handler2Invocations.push(a); } })

            await kernel.send({
                commandType: commandType1,
                command: command1In
            });
            await kernel.send({
                commandType: commandType2,
                command: command2In
            });

            expect(handler1Invocations.length).to.be.equal(1);
            let handler1Invocation = handler1Invocations[0];
            let commandSentToHandler1 = <CustomCommand1>handler1Invocation.commandEnvelope.command;
            expect(commandSentToHandler1).to.equal(command1In);
            expect(handler1Invocation.context).is.not.null;

            expect(handler2Invocations.length).to.be.equal(1);
            let handler2Invocation = handler2Invocations[0];
            let commandSentToHandler2 = <CustomCommand2>handler2Invocation.commandEnvelope.command;
            expect(commandSentToHandler2).to.equal(command2In);
            expect(handler2Invocation.context).is.not.null;
        });

        it("invokes only most recently registered command handler", async () => {
            // Multiple registrations for the same command: latest should replace previous handlers, to
            // avoid the problem of running every version of the handler ever registered.
            var kernel = await makeKernel();

            let command1In: CustomCommand1 = {
                data: "Test"
            };
            let handler1Invocations: IKernelCommandInvocation[] = [];
            let handler2Invocations: IKernelCommandInvocation[] = [];
            kernel.registerCommandHandler({ commandType: commandType1, handle: async (a: IKernelCommandInvocation) => { handler1Invocations.push(a); } })
            kernel.registerCommandHandler({ commandType: commandType1, handle: async (a: IKernelCommandInvocation) => { handler2Invocations.push(a); } })

            await kernel.send({
                commandType: commandType1,
                command: command1In
            });

            expect(handler1Invocations.length).to.be.equal(0);

            expect(handler2Invocations.length).to.be.equal(1);
            let handler2Invocation = handler2Invocations[0];
            let commandSentToHandler2 = <CustomCommand1>handler2Invocation.commandEnvelope.command;
            expect(commandSentToHandler2).to.equal(command1In);
            expect(handler2Invocation.context).is.not.null;
        });


        it("does not invoke command handler when type does not match", async () => {
            var kernel = await makeKernel();

            let commandType1: contracts.KernelCommandType = <contracts.KernelCommandType>"CustomCommand1";
            let commandType2: contracts.KernelCommandType = <contracts.KernelCommandType>"CustomCommand2";
            let command2In: CustomCommand2 = {
                moreData: "Test 2"
            };

            let events: contracts.KernelEventEnvelope[] = [];
            let handlerInvocations: IKernelCommandInvocation[] = [];
            kernel.registerCommandHandler({ commandType: commandType1, handle: async (a: IKernelCommandInvocation) => { handlerInvocations.push(a); } })

            kernel.subscribeToKernelEvents(e => events.push(e));
            let errorFromSend = null;
            await kernel.send({
                commandType: commandType2,
                command: command2In
            });

            expect(handlerInvocations.length).to.be.equal(0);

            expect(events.find(e => e.eventType === contracts.CommandFailedType)).is.not.null;
        });
    });
});