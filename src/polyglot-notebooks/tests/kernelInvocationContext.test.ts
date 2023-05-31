// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { IKernelEventObserver } from "../src/kernel";
import { KernelInvocationContext } from "../src/kernelInvocationContext";
import * as commandsAndEvents from "../src/commandsAndEvents";
import * as disposables from "../src/disposables";
import * as uuid from "uuid";

describe("dotnet-interactive", () => {

    let toDispose: disposables.Disposable[] = [];
    function use<T extends disposables.Disposable>(disposable: T): T {
        toDispose.push(disposable);
        return disposable;
    }

    afterEach(() => toDispose.forEach(d => d.dispose()));
    describe("client-side kernel invocation context", () => {

        let makeEventWatcher: () => { watcher: IKernelEventObserver, events: commandsAndEvents.KernelEventEnvelope[] } =
            () => {
                let events: commandsAndEvents.KernelEventEnvelope[] = [];
                return {
                    events,
                    watcher: (kernelEvent: commandsAndEvents.KernelEventEnvelope) => events.push(kernelEvent)
                };
            };
        function makeSubmitCode(code: string): commandsAndEvents.KernelCommandEnvelope {
            let command: commandsAndEvents.SubmitCode = {
                code: code
            };
            return {
                commandType: commandsAndEvents.SubmitCodeType,
                command: command,
                token: uuid.v4()
            };
        }
        let commadnEnvelope = makeSubmitCode("123");

        it("publishes CommandHandled when Complete is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.kernelEvents.subscribe(ew.watcher);

            context.complete(commadnEnvelope);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(commandsAndEvents.CommandSucceededType);
        });

        it("is established only once for same command", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));
            let secondContext = use(KernelInvocationContext.establish(commadnEnvelope));

            expect(context).to.equal(secondContext);
            expect(context.command).to.equal(secondContext.command);
        });

        it("is appends child comamnds", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));
            let secondContext = use(KernelInvocationContext.establish(makeSubmitCode("456")));

            expect(context).to.equal(secondContext);
            expect(context.command).to.equal(secondContext.command);
        });

        it("does not publish CommandFailed when Complete is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.kernelEvents.subscribe(ew.watcher);

            context.complete(commadnEnvelope);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.CommandFailedType);
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
            context.kernelEvents.subscribe(ew.watcher);

            context.complete(commadnEnvelope);

            let ev: commandsAndEvents.ErrorProduced = {
                message: "oops",
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.ErrorProducedType,
                command: commadnEnvelope
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.ErrorProducedType);
            });
        });

        it("publishes CommandFailed when Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.kernelEvents.subscribe(ew.watcher);

            context.fail("oops!");

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(commandsAndEvents.CommandFailedType);
        });

        it("does not publish CommandHandled when Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.kernelEvents.subscribe(ew.watcher);

            context.fail("oops!");

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.CommandSucceededType);
            });
        });

        it("does not publish further events after Fail is called", async () => {
            let context = use(KernelInvocationContext.establish(commadnEnvelope));

            let ew = makeEventWatcher();
            context.kernelEvents.subscribe(ew.watcher);

            context.fail("oops!");

            let ev: commandsAndEvents.DisplayedValueProduced = {
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.DisplayedValueProducedType,
                command: commadnEnvelope
            };
            context.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.DisplayedValueProducedType);
            });
        });

        it("completes only when child all commands complete if multiple commands are active", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.complete(innerSubmitCode);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.CommandSucceededType);
            });
        });

        it("publishes events from inner context if both inner and outer are in progress", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            let ev: commandsAndEvents.ErrorProduced = {
                message: "oops",
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(commandsAndEvents.ErrorProducedType);
        });

        it("does not publish further events from inner context after outer context is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            outer.complete(outerSubmitCode);

            let ev: commandsAndEvents.ErrorProduced = {
                message: "oops",
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.ErrorProducedType);
            });
        });

        it("does not publish further events from inner context after it is completed", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.complete(outerSubmitCode);

            let ev: commandsAndEvents.ErrorProduced = {
                message: "oops",
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.ErrorProducedType);
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
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.fail();

            expect(ew.events.length).to.eql(1);
            expect(ew.events[0].eventType).to.eql(commandsAndEvents.CommandFailedType);
            expect(ew.events[0].command).to.eql(outerSubmitCode);
        });

        it("does not publish further events after inner context fails", async () => {
            let outerSubmitCode = makeSubmitCode("abc");
            let outer = use(KernelInvocationContext.establish(outerSubmitCode));

            let ew = makeEventWatcher();
            outer.kernelEvents.subscribe(ew.watcher);

            let innerSubmitCode = makeSubmitCode("def");
            let inner = use(KernelInvocationContext.establish(innerSubmitCode));

            inner.fail();

            let ev: commandsAndEvents.ErrorProduced = {
                message: "oops",
                formattedValues: [],
                valueId: undefined
            };
            let evEnv: commandsAndEvents.KernelEventEnvelope = {
                event: ev,
                eventType: commandsAndEvents.ErrorProducedType,
                command: innerSubmitCode
            };
            inner.publish(evEnv);

            ew.events.forEach(event => {
                expect(event.eventType).is.not.eq(commandsAndEvents.ErrorProducedType);
            });
        });
    });
});