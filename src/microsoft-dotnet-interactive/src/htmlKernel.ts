// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { PromiseCompletionSource } from "./genericChannel";
import * as kernel from "./kernel";

export class HtmlKernel extends kernel.Kernel {
    constructor(kernelName?: string, private readonly htmlFragmentProcessor?: (htmlFragment: string) => Promise<void>, languageName?: string, languageVersion?: string) {
        super(kernelName ?? "html", languageName ?? "HTML");
        if (!this.htmlFragmentProcessor) {
            this.htmlFragmentProcessor = domHtmlFragmentProcessor;
        }
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
    }

    private async handleSubmitCode(invocation: kernel.IKernelCommandInvocation): Promise<void> {
        const submitCode = <contracts.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        invocation.context.publish({ eventType: contracts.CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });

        if (!this.htmlFragmentProcessor) {
            throw new Error("No HTML fragment processor registered");
        }

        try {
            await this.htmlFragmentProcessor(code);
        } catch (e) {
            throw e;//?
        }
    }
}

export function domHtmlFragmentProcessor(htmlFragment: string, configuration?: {
    containerFactory?: () => HTMLDivElement,
    elementToObserve?: () => HTMLElement,
    addToDom?: (element: HTMLElement) => void,
    mutationObserverFactory?: (callback: MutationCallback) => MutationObserver
}): Promise<void> {

    const factory: () => HTMLDivElement = configuration?.containerFactory ?? (() => document.createElement("div"));
    const elementToObserve: () => HTMLElement = configuration?.elementToObserve ?? (() => document.body);
    const addToDom: (element: HTMLElement) => void = configuration?.addToDom ?? ((element) => document.body.appendChild(element));
    const mutationObserverFactory = configuration?.mutationObserverFactory ?? (callback => new MutationObserver(callback));

    let container = factory();

    if (!container.id) {
        container.id = "html_kernel_container" + Math.floor(Math.random() * 1000000);
    }

    container.innerHTML = htmlFragment;
    const completionPromise = new PromiseCompletionSource<void>();
    const mutationObserver = mutationObserverFactory((mutations: MutationRecord[], observer: MutationObserver) => {

        for (const mutation of mutations) {
            if (mutation.type === "childList") {

                const nodes = Array.from(mutation.addedNodes);
                for (const addedNode of nodes) {
                    const element = addedNode as HTMLDivElement;
                    element.id;//?
                    container.id;//?
                    if (element?.id === container.id) {//?
                        completionPromise.resolve();
                        mutationObserver.disconnect();

                        return;
                    }
                }

            }
        }
    });

    mutationObserver.observe(elementToObserve(), { childList: true, subtree: true });
    addToDom(container);
    return completionPromise.promise;

}