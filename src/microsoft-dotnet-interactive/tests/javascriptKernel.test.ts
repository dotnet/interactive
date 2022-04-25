// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as contracts from "../src/contracts";
import { JavascriptKernel } from "../src/javascriptKernel";
import { Logger } from "../src/logger";

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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "theAnswer = 42;" } });
        await kernel.send({ commandType: contracts.RequestValueInfosType, command: <contracts.RequestValueInfos>{} });
        expect((<contracts.ValueInfosProduced>events.find(e => e.eventType === contracts.ValueInfosProducedType)!.event).valueInfos).to.deep.equal([{ name: 'theAnswer' }]);
    });

    it("returns values from RequestValue", async () => {
        const events: contracts.KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => events.push(e));

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "theAnswer = 42;" } });
        await kernel.send({ commandType: contracts.RequestValueType, command: <contracts.RequestValue>{ name: "theAnswer" } });
        expect((<contracts.ValueProduced>events.find(e => e.eventType === contracts.ValueProducedType)!.event).formattedValue).to.deep.equal({ mimeType: 'application/json', value: '42' });
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
});
