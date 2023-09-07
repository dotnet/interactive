// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as commandsAndEvents from "../src/commandsAndEvents";
import { createHtmlKernelForBrowser, HtmlDomFragmentInserterConfiguration } from "../src/htmlKernel";
import * as jd from "jsdom";

describe("htmlKernel", () => {
    it("can create a container for each code submission", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                const container = dom.window.document.createElement("div");
                container.className = "html_kernel_container";
                dom.window.document.body.appendChild(container);
                return container;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="0">a</div>' });
        const command2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="0">b</div>' });
        await kernel.send(command1);
        await kernel.send(command2);

        expect(events.find(e => e.eventType === commandsAndEvents.CommandSucceededType)).to.not.be.undefined;
        expect(dom.window.document.body.innerHTML).to.be.eql('<div class="html_kernel_container"><div id="0">a</div></div><div class="html_kernel_container"><div id="0">b</div></div>');
    });

    it("produces DisplayedValueProduced event with the content of the container", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.createElement("div");
        dom.window.document.body.appendChild(container);
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            updateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="0">a</div>' });
        const command2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="0">b</div>' });

        await kernel.send(command1);
        await kernel.send(command2);

        const lastDisplayedValueProducedEvent = events.filter(e => e.eventType === commandsAndEvents.DisplayedValueProducedType).at(-1)?.event as commandsAndEvents.DisplayedValueProduced;
        expect(lastDisplayedValueProducedEvent).to.not.be.undefined;

        expect(lastDisplayedValueProducedEvent).to.deep.equal({
            formattedValues:
                [{
                    mimeType: 'text/html',
                    suppressDisplay: false,
                    value: '<div id="0">a</div><div id="0">b</div>'
                }]
        });
    });

    it("evaluates script elements", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`, { runScripts: "dangerously" });
        const container = dom.window.document.createElement("div");
        dom.window.document.body.appendChild(container);
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            updateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            },
            jsEvaluator: (code: string) => {
                dom.window.eval(code);
                return Promise.resolve();
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="0"><script type="module">foo = 122;</script><div id="1"><script type="module">bar = 211;</script></div></div>' });
        await kernel.send(command);

        const lastDisplayedValueProducedEvent = events.filter(e => e.eventType === commandsAndEvents.DisplayedValueProducedType).at(-1)?.event as commandsAndEvents.DisplayedValueProduced;
        expect(lastDisplayedValueProducedEvent).to.not.be.undefined;

        expect(lastDisplayedValueProducedEvent).to.deep.equal(
            {
                formattedValues:
                    [{
                        mimeType: 'text/html',
                        suppressDisplay: false,
                        value: '<div id="0"><script type="module">foo = 122;</script><div id="1"><script type="module">bar = 211;</script></div></div>'
                    }]
            }
        );

        expect(dom.window.globalThis["foo"]).to.be.equal(122);
        expect(dom.window.globalThis["bar"]).to.be.equal(211);
    });

    it("can reuse container", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.createElement("div");
        dom.window.document.body.appendChild(container);
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            updateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="1">a</div>' });
        const command2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="2">b</div>' });

        await kernel.send(command1);
        await kernel.send(command2);

        expect(dom.window.document.body.innerHTML).to.be.eql('<div><div id="1">a</div><div id="2">b</div></div>');
    });

    it("can use body as container", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.body;
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            updateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command1 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="1">a</div>' });
        const command2 = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="2">b</div>' });

        await kernel.send(command1);
        await kernel.send(command2);

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can submit html fragment with multiple elements", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
        const dom = new jd.JSDOM(`<!DOCTYPE html>`);
        const container = dom.window.document.body;
        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            updateContainerContent: (container: HTMLElement, htmlFragment: string) => {
                container.innerHTML = container.innerHTML + htmlFragment;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };
        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' });
        await kernel.send(command);

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can replace container content with html fragment", async () => {
        let events: commandsAndEvents.KernelEventEnvelope[] = [];
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

        let htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration = {
            getOrCreateContainer: () => {
                return container;
            },
            normalizeHtmlFragment: (htmlFragment: string) => {
                const container = dom.window.document.createElement("div");
                container.innerHTML = htmlFragment;
                return container.innerHTML;
            },
            createMutationObserver: (callback: MutationCallback) => {
                return new dom.window.MutationObserver(callback);
            }
        };

        const kernel = createHtmlKernelForBrowser({ kernelName: "html", htmlDomFragmentInserterConfiguration });
        kernel.subscribeToKernelEvents((e) => {
            events.push(e);
        });

        const command = new commandsAndEvents.KernelCommandEnvelope(commandsAndEvents.SubmitCodeType, <commandsAndEvents.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' });

        await kernel.send(command);

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });
});