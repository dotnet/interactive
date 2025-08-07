// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as commandsAndEvents from "../src/commandsAndEvents";
import { JavascriptKernel } from "../src/javascriptKernel";
import { Logger } from "../src/logger";
import * as uuid from "uuid";
import { ErrorProduced } from "../src/commandsAndEvents";

describe("javascriptKernel", () => {

    before(() => {
        Logger.configure("test", () => { });
    });

    it("can handle SubmitCode", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "1+1" } as commandsAndEvents.SubmitCode);
        await kernel.send(command);

        expect(events.find(e => e.eventType === commandsAndEvents.CommandSucceededType)).to.not.be.undefined;
    });

    it("SubmitCode can access polyglotNotebooks api", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, {
            code: `
let command = new polyglotNotebooks.KernelCommandEnvelope(polyglotNotebooks.SubmitCodeType, { code: "return 1+2;"});
return command.toJson();`
        } as commandsAndEvents.SubmitCode);
        await kernel.send(command);

        expect(events.find(e => e.eventType === commandsAndEvents.CommandSucceededType)).to.not.be.undefined;

        const returnValueProduced = events.find(e => e.eventType === commandsAndEvents.ReturnValueProducedType)!.event as commandsAndEvents.ReturnValueProduced;

        var parsed = JSON.parse(returnValueProduced.formattedValues[0].value);
        delete parsed.token;
        delete parsed.id;
        expect(parsed).to.deep.equal({
            command: { code: "return 1+2;" },
            commandType: 'SubmitCode',
            routingSlip: [],
        });
    });

    it("can handle SendValue with application/json", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(
            commandsAndEvents.SendValueType,
            {
                formattedValue: {
                    value: JSON.stringify({ a: 1 }),
                    mimeType: "application/json",
                },
                name: "x0"
            } as commandsAndEvents.SendValue);

        await kernel.send(command);

        expect(events.find(e => e.eventType === commandsAndEvents.CommandSucceededType)).to.not.be.undefined;
        let value: any = kernel.getLocalVariable("x0");
        expect(value).to.deep.equal({ a: 1 });
    });

    it("does not return built-in values from RequestValueInfos", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestValueInfosType, {} as commandsAndEvents.RequestValueInfos);
        await kernel.send(command);

        expect((events.find(e => e.eventType === commandsAndEvents.ValueInfosProducedType)!.event as commandsAndEvents.ValueInfosProduced).valueInfos).to.be.empty;
    });

    it("reports values defined in SubmitCode", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: `${valueName} = 42;` } as commandsAndEvents.SubmitCode);
        const requestValueInfos = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestValueInfosType, {} as commandsAndEvents.RequestValueInfos);

        await kernel.send(submitCode);
        await kernel.send(requestValueInfos);

        expect((events.find(e => e.eventType === commandsAndEvents.ValueInfosProducedType)!.event as commandsAndEvents.ValueInfosProduced).valueInfos)
            .to.deep.equal(
                [{
                    formattedValue: {
                        mimeType: 'text/plain',
                        suppressDisplay: false,
                        value: '42'
                    },
                    name: valueName,
                    preferredMimeTypes: [],
                    typeName: 'number'
                }]);
    });

    it("valueinfo report variable type", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: `${valueName} = [42,43];` } as commandsAndEvents.SubmitCode);
        const requestValueInfos = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestValueInfosType, {} as commandsAndEvents.RequestValueInfos);

        await kernel.send(submitCode);
        await kernel.send(requestValueInfos);

        expect((events.find(e => e.eventType === commandsAndEvents.ValueInfosProducedType)!.event as commandsAndEvents.ValueInfosProduced).valueInfos)
            .to.deep.equal(
                [{
                    formattedValue: {
                        mimeType: 'text/plain',
                        suppressDisplay: false,
                        value: '[42,43]'
                    },
                    name: valueName,
                    preferredMimeTypes: [],
                    typeName: 'number[]'
                }]);
    });

    it("valueinfo report variable type for NaN, Infinity and -Infinity", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: `${valueName}1 = NaN; ${valueName}2 = Infinity; ${valueName}3 = -Infinity;` } as commandsAndEvents.SubmitCode);
        const requestValueInfos = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestValueInfosType, {} as commandsAndEvents.RequestValueInfos);

        await kernel.send(submitCode);
        await kernel.send(requestValueInfos);

        expect((events.find(e => e.eventType === commandsAndEvents.ValueInfosProducedType)!.event as commandsAndEvents.ValueInfosProduced).valueInfos)
            .to.deep.equal(
                [{
                    formattedValue: {
                        mimeType: 'text/plain',
                        suppressDisplay: false,
                        value: 'NaN'
                    },
                    name: `${valueName}1`,
                    preferredMimeTypes: [],
                    typeName: 'number'
                },
                {
                    formattedValue: {
                        mimeType: 'text/plain',
                        suppressDisplay: false,
                        value: 'Infinity'
                    },
                    name: `${valueName}2`,
                    preferredMimeTypes: [],
                    typeName: 'number'
                },
                {
                    formattedValue: {
                        mimeType: 'text/plain',
                        suppressDisplay: false,
                        value: '-Infinity'
                    },
                    name: `${valueName}3`,
                    preferredMimeTypes: [],
                    typeName: 'number'
                }]);
    });


    it("returns values from RequestValue", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: `${valueName} = 42;` } as commandsAndEvents.SubmitCode);
        const requestValue = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.RequestValueType, { name: `${valueName}` } as commandsAndEvents.RequestValue);

        await kernel.send(submitCode);
        await kernel.send(requestValue);

        expect((events.find(e => e.eventType === commandsAndEvents.ValueProducedType)!.event as commandsAndEvents.ValueProduced).formattedValue)
            .to.deep.equal({
                mimeType: 'application/json',
                suppressDisplay: false,
                value: '42'
            });
    });

    it("publishes CodeSumbissionReceived", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "1+1" } as commandsAndEvents.SubmitCode);

        await kernel.send(submitCode);

        expect(events.find(e => e.eventType === commandsAndEvents.CodeSubmissionReceivedType)).to.not.be.undefined;
    });

    it("publishes ReturnValueProduced when evaluation returns a value", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "return 1+1;" } as commandsAndEvents.SubmitCode);

        await kernel.send(submitCode);

        expect(events.find(e => e.eventType === commandsAndEvents.ReturnValueProducedType)).to.not.be.undefined;
    });

    it("publishes ErrorProduced when evaluation throws", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, {
            code: `
                f = function DoSomethingThatCausesAnError() {
                    const n = 1;
                    n = 2;
                };

                f();
            ` } as commandsAndEvents.SubmitCode);

        await kernel.send(submitCode);

        const errorProduced: ErrorProduced = events.find(e => e.eventType === commandsAndEvents.ErrorProducedType)?.event as ErrorProduced;
        expect(errorProduced).to.not.be.undefined;
        expect(errorProduced.message).to.contain(`Error: Assignment to constant variable.`);
        expect(errorProduced.message).to.contain(`TypeError: Assignment to constant variable.`);
        expect(errorProduced.message).to.contain(`at DoSomethingThatCausesAnError`);
    });

    it("handles async code", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: `await Promise.resolve(20);` } as commandsAndEvents.SubmitCode);

        await kernel.send(submitCode);

        expect(events.find(e => e.eventType === commandsAndEvents.CommandSucceededType)).to.not.be.undefined;
    });

    it("emits ReturnValueProduced when evaluation returns a value in async calls", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(
            commandsAndEvents.SubmitCodeType,
            {
                code: `await Promise.resolve(20);
            return 1+1;`
            } as commandsAndEvents.SubmitCode
        );

        await kernel.send(submitCode);

        expect(events.find(e => e.eventType === commandsAndEvents.ReturnValueProducedType)).to.not.be.undefined;
    });


    it("redirects console.log", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCode = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "console.log(12);" } as commandsAndEvents.SubmitCode);
        await kernel.send(submitCode);
        let event = events.find(e => e.eventType === commandsAndEvents.DisplayedValueProducedType)?.event as commandsAndEvents.DisplayedValueProduced;
        expect(event).to.not.be.undefined;

        expect(event.formattedValues[0].value).to.equal("12");
        expect(event.formattedValues[0].mimeType).to.equal("text/plain");
    });

    it("reuses redirected console in subsequent submissions", async () => {
        const events: commandsAndEvents.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const submitCodeToDeclareFunction = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "hello = function () { console.log('hello'); };" } as commandsAndEvents.SubmitCode);
        const submitCodeInvocation1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "hello();" } as commandsAndEvents.SubmitCode);
        const submitCodeInvocation2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, { code: "hello();" } as commandsAndEvents.SubmitCode);

        await kernel.send(submitCodeToDeclareFunction);
        await kernel.send(submitCodeInvocation1);
        await kernel.send(submitCodeInvocation2);

        const writtenLines = events.filter(e => e.eventType === commandsAndEvents.DisplayedValueProducedType).map(e => e.event as commandsAndEvents.DisplayedValueProduced).map(e => e.formattedValues[0].value);
        expect(writtenLines).to.deep.equal([
            "hello",
            "hello",
        ]);
    });
});
