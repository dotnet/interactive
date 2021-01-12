// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { ClientSideKernel } from "../src/dotnet-interactive/client-side-kernel";
import { KernelCommand, KernelCommandEnvelope, KernelCommandType } from "../src/dotnet-interactive/contracts";
import { createMockKernelTransport } from "./testSupport";

interface CustomCommand1 extends KernelCommand {
    data: string
}

interface CustomCommand2 extends KernelCommand {
    moreData: string
}

describe("dotnet-interactive", () => {
    let makeKernel = async () => {
        let kernel = new ClientSideKernel();
        return kernel;
    };
    describe("client-side kernel", () => {
        it("invokes command handler when type matches", async () => {
            var kernel = await makeKernel();

            let commandType1: KernelCommandType = <KernelCommandType>"CustomCommand1";
            let commandType2: KernelCommandType = <KernelCommandType>"CustomCommand2";
            let command1In: CustomCommand1 = {
                data: "Test"
            };
            let command2In: CustomCommand2 = {
                moreData: "Test 2"
            };

            let command1EnvelopesSentToKernel: KernelCommandEnvelope[] = [];
            let command2EnvelopesSentToKernel: KernelCommandEnvelope[] = [];
            kernel.registerCommandHandler(commandType1, async env => { command1EnvelopesSentToKernel.push(env); })
            kernel.registerCommandHandler(commandType2, async env => { command2EnvelopesSentToKernel.push(env); })

            await kernel.send({
                commandType: commandType1,
                command: command1In
            });
            await kernel.send({
                commandType: commandType2,
                command: command2In
            });

            expect(command1EnvelopesSentToKernel.length).to.be.equal(1);
            expect(command1EnvelopesSentToKernel[0].commandType).to.be.equal(commandType1);
            let command1SentToKernel = <CustomCommand1>command1EnvelopesSentToKernel[0].command;
            expect(command1SentToKernel.data).to.be.equal(command1In.data);

            expect(command2EnvelopesSentToKernel.length).to.be.equal(1);
            expect(command2EnvelopesSentToKernel[0].commandType).to.be.equal(commandType2);
            let command2SentToKernel = <CustomCommand2>command2EnvelopesSentToKernel[0].command;
            expect(command2SentToKernel.moreData).to.be.equal(command2In.moreData);
        });

        it("invokes only most recently registered command handler", () => {
            // TODO

            // Multiple registrations for the same command: latest should replace previous handlers, to
            // avoid the problem of running every version of the handler ever registered.
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

            await kernel.send({
                commandType: commandType2,
                command: command2In
            });

            expect(command1EnvelopesSentToKernel.length).to.be.equal(0);
        });

        it("raises suitable kernel event when command type matches no handlers", async () => {
            // TODO
        });

        it("does not invoke command handler immediately when handling already in progress", async () => {
            // TODO
        });

        it("invokes command handler after previous command in progress completes", async () => {
            // TODO
        });
    });
});