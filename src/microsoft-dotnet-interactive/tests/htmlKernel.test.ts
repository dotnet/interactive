// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as contracts from "../src/contracts";
import { domHtmlFragmentProcessor, HtmlKernel } from "../src/htmlKernel";
import * as jd from "jsdom";

describe("htmlKernel", () => {
    it("can handle SubmitCode", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        let htmlFragmentProcessorConfiguration = {
            containerFactory: () => {
                return dom.window.document.createElement("div");
            },
            elementToObserve: () => {
                return dom.window.document.body;
            },
            addToDom: (element: HTMLElement) => {
                dom.window.document.body.appendChild(element);
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: "<div>a</div>" } });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
    });
});