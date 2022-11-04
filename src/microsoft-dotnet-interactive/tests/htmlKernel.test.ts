// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import * as contracts from "../src/contracts";
import { createHtmlKernelForBrowser, htmlDomFragmentInserter, HtmlDomFragmentInserterConfiguration, HtmlKernel } from "../src/htmlKernel";
import * as jd from "jsdom";

describe("htmlKernel", () => {
    it("can create a container for each code submission", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0">b</div>' } });

        expect(events.find(e => e.eventType === contracts.CommandSucceededType)).to.not.be.undefined;
        expect(dom.window.document.body.innerHTML).to.be.eql('<div class="html_kernel_container"><div id="0">a</div></div><div class="html_kernel_container"><div id="0">b</div></div>');
    });

    it("produces DisplayedValueProduced event with the content of the container", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0">b</div>' } });

        const lastDisplayedValueProducedEvent = events.filter(e => e.eventType === contracts.DisplayedValueProducedType).at(-1)?.event as contracts.DisplayedValueProduced;
        expect(lastDisplayedValueProducedEvent).to.not.be.undefined;

        expect(lastDisplayedValueProducedEvent).to.deep.equal({ displayedValueProduced: { formattedValues: [{ mimeType: 'text/html', value: '<div id="0">a</div><div id="0">b</div>' }] } });
    });

    it("evaluates script elements", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="0"><script>foo = 122;</script></div>' } });

        const lastDisplayedValueProducedEvent = events.filter(e => e.eventType === contracts.DisplayedValueProducedType).at(-1)?.event as contracts.DisplayedValueProduced;
        expect(lastDisplayedValueProducedEvent).to.not.be.undefined;

        expect(lastDisplayedValueProducedEvent).to.deep.equal({
            displayedValueProduced:
            {
                formattedValues:
                    [{
                        mimeType: 'text/html',
                        value: '<div id="0"><script>foo = 122;</script></div>'
                    }]
            }
        });

        expect(dom.window.globalThis["foo"]).to.be.equal(122);
    });

    it("can reuse container", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="2">b</div>' } });

        expect(dom.window.document.body.innerHTML).to.be.eql('<div><div id="1">a</div><div id="2">b</div></div>');
    });

    it("can use body as container", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div>' } });
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="2">b</div>' } });

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });

    it("can submit html fragment with multiple elements", async () => {
        let events: contracts.KernelEventEnvelope[] = [];
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

        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' } });

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
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
        await kernel.send({ commandType: contracts.SubmitCodeType, command: <contracts.SubmitCode>{ code: '<div id="1">a</div><div id="2">b</div>' } });

        expect(dom.window.document.body.innerHTML).to.be.eql('<div id="1">a</div><div id="2">b</div>');
    });
});