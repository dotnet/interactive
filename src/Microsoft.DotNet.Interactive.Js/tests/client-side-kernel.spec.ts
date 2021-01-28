// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { ClientSideKernel } from "../src/dotnet-interactive/client-side-kernel";
import { KernelCommand, KernelCommandEnvelope, KernelCommandType } from "../src/dotnet-interactive/contracts";
import { KernelInvocationContext } from "../src/dotnet-interactive/dotnet-interactive-interfaces";

interface CustomCommand1 extends KernelCommand {
    data: string
}

interface CustomCommand2 extends KernelCommand {
    moreData: string
}

describe("dotnet-interactive", () => {
    let commandType1: KernelCommandType = <KernelCommandType>"CustomCommand1";
    let commandType2: KernelCommandType = <KernelCommandType>"CustomCommand2";

    let makeKernel = async () => {
        let kernel = new ClientSideKernel();
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

            let handler1Invocations: { envelope: KernelCommandEnvelope, context: KernelInvocationContext }[] = [];
            let handler2Invocations: { envelope: KernelCommandEnvelope, context: KernelInvocationContext }[] = [];
            kernel.registerCommandHandler(commandType1, async (envelope, context) => { handler1Invocations.push({ envelope, context }); })
            kernel.registerCommandHandler(commandType2, async (envelope, context) => { handler2Invocations.push({ envelope, context }); })

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
            let handler1Envelope = handler1Invocation.envelope;
            expect(handler1Envelope.commandType).to.be.equal(commandType1);
            let commandSentToHandler1 = <CustomCommand1>handler1Envelope.command;
            expect(commandSentToHandler1).to.equal(command1In);
            expect(handler1Invocation.context).is.not.null;

            expect(handler2Invocations.length).to.be.equal(1);
            let handler2Invocation = handler2Invocations[0];
            let handler2Envelope = handler2Invocation.envelope;
            expect(handler2Envelope.commandType).to.be.equal(commandType2);
            let commandSentToHandler2 = <CustomCommand2>handler2Envelope.command;
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
            let handler1Invocations: { envelope: KernelCommandEnvelope, context: KernelInvocationContext }[] = [];
            let handler2Invocations: { envelope: KernelCommandEnvelope, context: KernelInvocationContext }[] = [];
            kernel.registerCommandHandler(commandType1, async (envelope, context) => { handler1Invocations.push({ envelope, context }); })
            kernel.registerCommandHandler(commandType2, async (envelope, context) => { handler2Invocations.push({ envelope, context }); })

            await kernel.send({
                commandType: commandType1,
                command: command1In
            });

            expect(handler1Invocations.length).to.be.equal(0);

            expect(handler2Invocations.length).to.be.equal(1);
            let handler2Invocation = handler2Invocations[0];
            let handler2Envelope = handler2Invocation.envelope;
            expect(handler2Envelope.commandType).to.be.equal(commandType1);
            let commandSentToHandler2 = <CustomCommand1>handler2Envelope.command;
            expect(commandSentToHandler2).to.equal(command1In);
            expect(handler2Invocation.context).is.not.null;
        });
        

        it("does not invoke command handler when type does not match", async () => {
            var kernel = await makeKernel();

            let commandType1: KernelCommandType = <KernelCommandType>"CustomCommand1";
            let commandType2: KernelCommandType = <KernelCommandType>"CustomCommand2";
            let command2In: CustomCommand2 = {
                moreData: "Test 2"
            };

            let command1EnvelopesSentToKernel: KernelCommandEnvelope[] = [];
            kernel.registerCommandHandler(commandType1, async env => { command1EnvelopesSentToKernel.push(env); })

            let errorFromSend = null;
            await kernel.send({
                commandType: commandType2,
                command: command2In
            })
            .catch(e => { errorFromSend = e; });

            expect(command1EnvelopesSentToKernel.length).to.be.equal(0);
            expect(errorFromSend).is.not.null;
        });

        it("raises suitable kernel event when command type matches no handlers", async () => {
            // TODO
            // What's the right event? We probably need to understand this in the broader
            // context of what sort of error we think this is. Should the dotnet-interactive side
            // only ever send a command to the client if it's confident the client will handle
            // it? (In which case, this is a "this should never happen" type error.) Or do we
            // let user code attempt to send whatever commands they like to the client? (In which
            // case this is a "we need to tell the user what they did wrong" type error.)
        });
    });
});