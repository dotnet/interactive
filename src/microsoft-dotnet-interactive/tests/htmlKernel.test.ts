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
            getOrCreateContainer: () => {
                return dom.window.document.createElement("div");
            },
            mutateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = htmlFragment;
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0">a</div>' } });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
    });

    it("can reuse container", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.createElement("div");
        let htmlFragmentProcessorConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            mutateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="2">b</div>' } });

        expect(container.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can use body as container", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.body;
        let htmlFragmentProcessorConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            mutateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="2">b</div>' } });

        expect(container.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can submit html fragment with multiple elements", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.body;
        let htmlFragmentProcessorConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            mutateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' } });

        expect(container.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can replace container content with html fragment", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>
        <html lang="en">
          <head>
            <meta charset="utf-8" />
            <title>Replace Inner HTML of Element</title>
          </head>
          <body>
            <div id="main">
              <p>Replace me!!</p>
            </div>
          </body>
        </html>`);
        const container = dom.window.document.body;

        let htmlFragmentProcessorConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            mutateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = htmlFragment;
            },
            mutationObserverFactory: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = new HtmlKernel("html", (fragment) => domHtmlFragmentProcessor(fragment, htmlFragmentProcessorConfiguration));
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' } });

        expect(container.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });
});