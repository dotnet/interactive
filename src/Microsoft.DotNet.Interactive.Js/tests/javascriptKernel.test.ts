// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { CodeSubmissionReceivedType, CommandSucceededType, DisplayedValueProducedType, KernelEventEnvelope, ReturnValueProducedType, SubmitCode, SubmitCodeType } from "../src/common/interfaces/contracts";
import { JavascriptKernel } from "../src/common/interactive/javascriptKernel";
import { DisplayedValueProduced } from "../../dotnet-interactive-vscode/common/interfaces/contracts";

describe("javascriptKernel", () => {
    it("can handle SubmitCode", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: "1+1" } });

        expect(events.find(e => e.eventType === CommandSucceededType)).to.not.be.undefined;
    });

    it("notifies about CodeSumbission", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: "1+1" } });

        expect(events.find(e => e.eventType === CodeSubmissionReceivedType)).to.not.be.undefined;
    });

    it("emits ReturnValueProduced when evaluation return a value", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: "return 1+1;" } });

        expect(events.find(e => e.eventType === ReturnValueProducedType)).to.not.be.undefined;
    });

    it("handles async code", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: `await Promise.resolve(20);` } });

        expect(events.find(e => e.eventType === CommandSucceededType)).to.not.be.undefined;
    });

    it("emits ReturnValueProduced when evaluation return a value in async calls", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: `await Promise.resolve(20); 
        return 1+1;` } });
        expect(events.find(e => e.eventType === ReturnValueProducedType)).to.not.be.undefined;
    });


    it("redirect console.log", async () => {
        let events: KernelEventEnvelope[] = [];
        const kernel = new JavascriptKernel();
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: SubmitCodeType, command: <SubmitCode>{ code: "console.log(12);" } });
        let event = <DisplayedValueProduced><any>(events.find(e => e.eventType === DisplayedValueProducedType))?.event;
        expect(event).to.not.be.undefined;
        
        expect(event.formattedValues[0].value).to.equal("12");
        expect(event.formattedValues[0].mimeType).to.equal("text/plain");
    });
});
