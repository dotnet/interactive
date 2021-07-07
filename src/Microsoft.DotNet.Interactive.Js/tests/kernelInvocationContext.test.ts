// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { IKernelEventObserver } from "../src/common/interactive/kernel";
import { KernelInvocationContext } from "../src/common/interactive/kernelInvocationContext";
import { CommandFailedType, CommandSucceededType, DisplayedValueProduced, DisplayedValueProducedType, Disposable, ErrorProduced, ErrorProducedType, KernelCommand, KernelCommandEnvelope, KernelCommandType, KernelEvent, KernelEventEnvelope, KernelEventEnvelopeObserver, SubmitCode, SubmitCodeType } from "../src/common/interfaces/contracts";

describe("dotnet-interactive", () => {

    let toDispose: Disposable[] = [];
    function use<T extends Disposable>(disposable: T): T {
        toDispose.push(disposable);
        return disposable;
    }

    afterEach(() => toDispose.forEach(d => d.dispose()));
    describe("client-side kernel invocation context", () => {

        let makeEventWatcher: () => { watcher: IKernelEventObserver, events: KernelEventEnvelope[] } =
            () => {
                let events: KernelEventEnvelope[] = [];
                return {
                    events,
                    watcher: (kernelEvent: KernelEventEnvelope) => events.push(kernelEvent)
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
        let commadnEnvelope = makeSubmitCode("123");

        it("publishes CommandHandled when Complete is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commadnEnvelope);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandSucceededType);
        });

        it("does not publish CommandFailed when Complete is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commadnEnvelope);

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
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.complete(commadnEnvelope);

            let ev: ErrorProduced = {
                message: "oops",
                formattedValues: null,
                valueId: null
            };
            let evEnv: KernelEventEnvelope = {
                event: ev,
                eventType: ErrorProducedType,
                command: commadnEnvelope
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(ErrorProducedType);
            });
        });

        it("publishes CommandFailed when Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandFailedType);
        });

        it("does not publish CommandHandled when Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.subscribeToKernelEvents(ew.watcher);

            context.fail("oops!");

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandSucceededType);
            });
        });

        it("does not publish further events after Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

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
                command: commadnEnvelope
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(DisplayedValueProducedType);
            });
        });

        it("completes only when child all commands complete if multiple commands are active", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.complete(innerSubmitCode);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(CommandSucceededType);
            });
        });

        it("publishes events from inner context if both inner and outer are in progress", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

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
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

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
        });

        it("does not publish further events from inner context after it is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

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
            let context = KernelInvocationContext.establish(commadnEnvelope);

            // TODO
            // The C# test registers a completion callback with OnComplete, but it's not
            // entirely clear why. It doesn't verify that the completion callback is
            // invoked, so I'm not sure what the relevance is to this test.

            context.dispose();

            expect(KernelInvocationContext.current).is.null;
        });

        it("publishes CommandFailed when inner context fails", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.fail();

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(CommandFailedType);
            expect(ew.events[0].command).to.eql(outerSubmitCode);
        });

        it("does not publish further events after inner context fails", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.subscribeToKernelEvents(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

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