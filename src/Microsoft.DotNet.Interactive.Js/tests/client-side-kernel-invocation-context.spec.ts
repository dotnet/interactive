// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { IKernelEventObserver } from "../src/dotnet-interactive";
import { ClientSideKernelInvocationContext } from "../src/dotnet-interactive/client-side-kernel-invocation-context";
import { CommandFailedType, CommandSucceededType, DisplayedValueProduced, DisplayedValueProducedType, Disposable, ErrorProduced, ErrorProducedType, KernelCommand, KernelCommandEnvelope, KernelCommandType, KernelEvent, KernelEventEnvelope, KernelEventEnvelopeObserver, SubmitCode, SubmitCodeType } from "../src/dotnet-interactive/contracts";

describe("dotnet-interactive", () => {
    describe("client-side kernel invocation context", () => {

        let makeEventWatcher: () => { watcher: IKernelEventObserver, events: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }[] } =
            () => {
                let events: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }[] = [];
                return {
                    events,
                    watcher: (argument: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }) => events.push(argument)
                };
            };
        function makeSubmitCode(code: string): { commandType: string, command: KernelCommand } {
            let command: SubmitCode = {
                code: code
            };
            return {
                commandType: SubmitCodeType,
                command: command
            };
        }
        let commandAndType = makeSubmitCode("123");

        let toDispose: Disposable[] = [];
        function use<T extends Disposable>(disposable: T): T {
            toDispose.push(disposable);
            return disposable;
        }
        afterEach(() => toDispose.forEach(d => d.dispose()));

        it("publishes CommandHandled when Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandAndType.command);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandSucceededType);
        });

        it("does not publish CommandFailed when Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandAndType.command);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandFailedType);
            });
        });

        it("completes only when child context introduced by spawned command completes", async () => {
            // TODO
            // Not clear that we can test this right now, because the C# test depends on adding
            // middleware to the composite kernel, and getting it to invoke the C# kernel. I'm not
            // even sure where the spawned child command gets created - the test only appears to
            // create one command explicitly.
        });

        it("does not publish further events after Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandAndType.command);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: ErrorProducedType,
                ...commandAndType
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("publishes CommandFailed when Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandFailedType);
        });

        it("does not publish CommandHandled when Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandSucceededType);
            });
        });

        it("does not publish further events after Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandAndType));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            let ev: DisplayedValueProduced = {
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: DisplayedValueProducedType,
                ...commandAndType
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(DisplayedValueProducedType);
            });
        });

        it("completes only when child all commands complete if multiple commands are active", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            inner.complete(innerSubmitCode.command);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandSucceededType);
            });
        });

        it("publishes events from inner context if both inner and outer are in progress", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: ErrorProducedType,
                ...innerSubmitCode
            };
            inner.publish(evEnv);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(ErrorProducedType);
        });

        it("does not publish further events from inner context after outer context is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            outer.complete(outerSubmitCode.command);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: ErrorProducedType,
                ...innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("does not publish further events from inner context after it is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            inner.complete(outerSubmitCode.command);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: ErrorProducedType,
                ...innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("set current to null when disposed", async () => {
            let context = ClientSideKernelInvocationContext.establish(commandAndType);

            // TODO
            // The C# test registers a completion callback with OnComplete, but it's not
            // entirely clear why. It doesn't verify that the completion callback is
            // invoked, so I'm not sure what the relevance is to this test.

            context.dispose();

            expect(ClientSideKernelInvocationContext.current).is.null;
        });

        it("publishes CommandFailed when inner context fails", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            inner.fail();

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandFailedType);
            expect(ew.events[0].command).to.eql(outerSubmitCode.command);
        });

        it("does not publish further events after inner context fails", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            inner.fail();

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv = {
                event: ev,
                eventType: ErrorProducedType,
                ...innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });
    });
});