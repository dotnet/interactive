// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Kernel, IKernelCommandInvocation } from "./kernel";
import { PromiseCompletionSource } from "./promiseCompletionSource";

export class HtmlKernel extends Kernel {
    constructor(kernelName?: string, private readonly htmlFragmentInserter?: (htmlFragment: string) => Promise<string>, languageName?: string, languageVersion?: string) {
        super(kernelName ?? "html", languageName ?? "HTML");
        if (!this.htmlFragmentInserter) {
            this.htmlFragmentInserter = htmlDomFragmentInserter;
        }
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
    }

    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
        const submitCode = <contracts.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        invocation.context.publish({ eventType: contracts.CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });

        if (!this.htmlFragmentInserter) {
            throw new Error("No HTML fragment processor registered");
        }

        try {
            const formattedValue = await this.htmlFragmentInserter(code);
            const displayedValueProduced: contracts.DisplayedValueProduced = {
                formattedValues: [{
                    mimeType: "text/html",
                    value: formattedValue
                }]
            };

            invocation.context.publish({ eventType: contracts.DisplayedValueProducedType, event: { displayedValueProduced }, command: invocation.commandEnvelope });

        } catch (e) {
            throw e;//?
        }
    }
}

export type HtmlDomFragmentInserterConfiguration = {
    getOrCreateContainer?: () => HTMLElement,
    updateContainerContent?: (container: HTMLElement, htmlFragment: string) => void,
    createMutationObserver?: (callback: MutationCallback) => MutationObserver,
    nomarliseHtmlFragment?: (htmlFragment: string) => string
};

export function htmlDomFragmentInserter(htmlFragment: string, configuration?: HtmlDomFragmentInserterConfiguration): Promise<string> {

    const getOrCreateContainer = configuration?.getOrCreateContainer ?? (() => {
        const container = document.createElement("div");
        document.body.appendChild(container);
        return container;
    });
    const nomarliseFragment = configuration?.nomarliseHtmlFragment ?? ((htmlFragment: string) => {
        const container = document.createElement("div");
        container.innerHTML = htmlFragment;
        return container.innerHTML;
    });
    const updateContainerContent = configuration?.updateContainerContent ?? ((container, htmlFragment) => container.innerHTML = htmlFragment);
    const createMutationObserver = configuration?.createMutationObserver ?? (callback => new MutationObserver(callback));

    let container = getOrCreateContainer();

    const normalisedHtmlFragment = nomarliseFragment(htmlFragment);
    const completionPromise = new PromiseCompletionSource<void>();
    const mutationObserver = createMutationObserver((mutations: MutationRecord[], observer: MutationObserver) => {

        for (const mutation of mutations) {
            if (mutation.type === "childList") {
                const done = container.innerHTML.includes(normalisedHtmlFragment);
                done;//?
                if (done) {
                    completionPromise.resolve();
                    mutationObserver.disconnect();
                    return;
                }
            }
        }
    });

    mutationObserver.observe(container, { childList: true, subtree: true });
    updateContainerContent(container, normalisedHtmlFragment);
    return completionPromise.promise.then(() => container.innerHTML);
}

export function createHtmlKernelThatWorksWithPageDomInBrowser(config: { kernelName: string, container: HTMLElement, contentBehaviour: "append" | "replace" }): Kernel {

    const kernel = new HtmlKernel(
        config.kernelName,
        (htmlFragment: string) => htmlDomFragmentInserter(htmlFragment, {
            getOrCreateContainer: () => config.container,
            updateContainerContent: (container, htmlFragment) => {
                if (config.contentBehaviour === "append") {
                    container.innerHTML += htmlFragment;
                } else {
                    container.innerHTML = htmlFragment;
                }
            }
        }));

    return kernel;
}