// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Kernel, IKernelCommandInvocation } from "./kernel";
import { PromiseCompletionSource } from "./promiseCompletionSource";

export class HtmlKernel extends Kernel {
    constructor(kernelName?: string, private readonly htmlFragmentProcessor?: (htmlFragment: string) => Promise<void>, languageName?: string, languageVersion?: string) {
        super(kernelName ?? "html", languageName ?? "HTML");
        if (!this.htmlFragmentProcessor) {
            this.htmlFragmentProcessor = htmlDomFragmentProcessor;
        }
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
    }

    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
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

export type HtmlDomFragmentProcessor = {
    getOrCreateContainer?: () => HTMLElement,
    mutateContainerContent?: (container: HTMLElement, htmlFragment: string) => void,
    mutationObserverFactory?: (callback: MutationCallback) => MutationObserver
};

export function htmlDomFragmentProcessor(htmlFragment: string, configuration?: HtmlDomFragmentProcessor): Promise<void> {

    const getOrCreateContainer: () => HTMLElement = configuration?.getOrCreateContainer ?? (() => document.createElement("div"));
    const mutateContainerContent: (container: HTMLElement, htmlFragment: string) => void = configuration?.mutateContainerContent ?? ((container, htmlFragment) => container.append(htmlFragment));
    const mutationObserverFactory = configuration?.mutationObserverFactory ?? (callback => new MutationObserver(callback));

    let container = getOrCreateContainer();

    const completionPromise = new PromiseCompletionSource<void>();
    const mutationObserver = mutationObserverFactory((mutations: MutationRecord[], observer: MutationObserver) => {

        for (const mutation of mutations) {
            if (mutation.type === "childList") {

                const nodes = Array.from(mutation.addedNodes);
                for (const addedNode of nodes) {
                    const element = addedNode as HTMLDivElement;
                    element.parentElement?.id;//?
                    container.id;//?
                    if (element.parentElement === container) {
                        completionPromise.resolve();
                        mutationObserver.disconnect();

                        return;
                    }
                }

            }
        }
    });

    mutationObserver.observe(container, { childList: true, subtree: true });
    mutateContainerContent(container, htmlFragment);
    return completionPromise.promise;

}