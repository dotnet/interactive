// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as contracts from "../src/contracts";
import { JavascriptKernel } from "../src/javascriptKernel";
import { Logger } from "../src/logger";
import * as uuid from "uuid";

describe("javascriptKernel", () => {

    before(() => {
        Logger.configure("test", () => { });
    });

    it("can handle SubmitCode", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+1" } });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
    });

    it("can handle SendValue with application/json", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({
            commandType: contracts.SendValueType, command: <contracts.SendValue>{
                formattedValue: {
                    value: JSON.stringify({ a: 1 }),
                    mimeType: "application/json",
                },
                name: "x0"
            }
        });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
        let value: any = kernel.getLocalVariable("x0");
        expect(value).to.deep.equal({ a: 1 });
    });

    it("does not return built-in values from RequestValueInfos", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));

        await kernel.send({ commandType: contracts.RequestValueInfosType, command: <contracts.RequestValueInfos>{} });

        expect((<contracts.ValueInfosProduced>events.find(e => e.eventType === contracts.ValueInfosProducedType)!.event).valueInfos).to.be.empty;
    });

    it("reports values defined in SubmitCode", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: `${valueName} = 42;` } });
        await kernel.send({ commandType: contracts.RequestValueInfosType, command: <contracts.RequestValueInfos>{} });
        expect((<contracts.ValueInfosProduced>events.find(e => e.eventType === contracts.ValueInfosProducedType)!.event).valueInfos)
            .to.deep.equal(
                [{
                    formattedValue: { mimeType: 'text/plain', value: '42' },
                    name: valueName,
                    preferredMimeTypes: [],
                    typeName: 'number'
                }]);
    });

    it("valueinfo report variable type", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: `${valueName} = [42,43];` } });
        await kernel.send({ commandType: contracts.RequestValueInfosType, command: <contracts.RequestValueInfos>{} });
        expect((<contracts.ValueInfosProduced>events.find(e => e.eventType === contracts.ValueInfosProducedType)!.event).valueInfos)
            .to.deep.equal(
                [{
                    formattedValue: { mimeType: 'text/plain', value: '[42,43]' },
                    name: valueName,
                    preferredMimeTypes: [],
                    typeName: 'number[]'
                }]);
    });

    it("returns values from RequestValue", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));
        const valueName = `value_${uuid.v4().replace(/-/g, "_")}`; //?
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: `${valueName} = 42;` } });
        await kernel.send({ commandType: contracts.RequestValueType, command: <contracts.RequestValue>{ name: `${valueName}` } });
        expect((<contracts.ValueProduced>events.find(e => e.eventType === contracts.ValueProducedType)!.event).formattedValue)
            .to.deep.equal({
                mimeType: 'application/json',
                value: '42'
            });
    });

    it("notifies about CodeSumbission", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "1+1" } });

        expect(events.find(e => e.eventType === contracts.CodeSubmissionReceivedType)).to.not.be.undefined;
    });

    it("emits ReturnValueProduced when evaluation return a value", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "return 1+1;" } });

        expect(events.find(e => e.eventType === contracts.ReturnValueProducedType)).to.not.be.undefined;
    });

    it("handles async code", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: `await Promise.resolve(20);` } });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
    });

    it("emits ReturnValueProduced when evaluation return a value in async calls", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({
            commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{
                code: `await Promise.resolve(20);
        return 1+1;` }
        });
        expect(events.find(e => e.eventType === contracts.ReturnValueProducedType)).to.not.be.undefined;
    });


    it("redirect console.log", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "console.log(12);" } });
        let event = <contracts.DisplayedValueProduced><any>(events.find(e => e.eventType === contracts.DisplayedValueProducedType))?.event;
        expect(event).to.not.be.undefined;

        expect(event.formattedValues[0].value).to.equal("12");
        expect(event.formattedValues[0].mimeType).to.equal("text/plain");
    });

    it("redirected console is reused in subsequent submissions", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "hello = function () { console.log('hello'); };" } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "hello();" } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "hello();" } });
        const writtenLines = events.filter(e => e.eventType === contracts.DisplayedValueProducedType).map(e => <contracts.DisplayedValueProduced>e.event).map(e => e.formattedValues[0].value);
        expect(writtenLines).to.deep.equal([
            "hello",
            "hello",
        ]);
    });
});
