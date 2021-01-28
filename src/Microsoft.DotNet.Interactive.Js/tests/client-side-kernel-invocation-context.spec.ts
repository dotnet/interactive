// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { ClientSideKernelInvocationContext } from "../src/dotnet-interactive/client-side-kernel-invocation-context";
import { CommandFailedType, CommandSucceededType, DisplayedValueProduced, DisplayedValueProducedType, Disposable, ErrorProduced, ErrorProducedType, KernelCommand, KernelCommandEnvelope, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver, SubmitCode, SubmitCodeType } from "../src/dotnet-interactive/contracts";

describe("dotnet-interactive", () => {
    describe("client-side kernel invocation context", () => {

        let makeEventWatcher: () => { watcher: KernelEventEnvelopeObserver, events: KernelEventEnvelope[] } =
            () => {
                let events: KernelEventEnvelope[] = [];
                return {
                    events,
                watcher: (ke: KernelEventEnvelope) => events.push(ke)
                };
            };
        function makeSubmitCode(code: string): KernelCommandEnvelope {
            let command: SubmitCode = {
                code: code
            };
            return {
                commandType: SubmitCodeType,
                command: command
            };
        }
        let commandEnvelope: KernelCommandEnvelope = makeSubmitCode("123");

        let toDispose: Disposable[] = [];
        function use<T extends Disposable>(disposable: T): T {
            toDispose.push(disposable);
            return disposable;
        }
        afterEach(() => toDispose.forEach(d => d.dispose()));

        it("publishes CommandHandled when Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandEnvelope);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandSucceededType);
        });

        it("does not publish CommandFailed when Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandEnvelope);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandFailedType);
            });
        });

        it("completes only when child context completes when child command spawned", async () => {
            //TODO
        });

        it("does not publish further events after Complete is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commandEnvelope);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: commandEnvelope
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("publishes CommandFailed when Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandFailedType);
        });

        it("does not publish CommandHandled when Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandSucceededType);
            });
        });

        it("does not publish further events after Fail is called", async () => {
            let context = use(ClientSideKernelInvocationContext.establish(commandEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            let ev: DisplayedValueProduced = {
                formattedValues: null,
                valueId: null
            };
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: DisplayedValueProducedType,
                command: commandEnvelope
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

            inner.complete(innerSubmitCode);

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
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: innerSubmitCode
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

            outer.complete(outerSubmitCode);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });

            // TODO
            // Presumably the original version of this test was introduced because a child
            // context's events normally are published. But there are no tests for this,
            // so in our case they aren't yet... So this test passes right now only because
            // we never actually publish child context events from the parent one.
        });

        it("does not publish further events from inner context after it is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(ClientSideKernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(ClientSideKernelInvocationContext.establish(innerSubmitCode));

            inner.complete(outerSubmitCode);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("set current to null when disposed", async () => {
            let context = ClientSideKernelInvocationContext.establish(commandEnvelope);

            // TODO
            // The C# test registers a completion callback with OnComplete, but it's not
            // entirely clear why.

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
            expect(ew.events[0].command).to.eql(outerSubmitCode);
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
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });
    });
});