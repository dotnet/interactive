// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./commandsAndEvents";
import { Kernel, IKernelCommandInvocation } from "./kernel";
import { Logger } from "./logger";
import { PromiseCompletionSource } from "./promiseCompletionSource";

export class HtmlKernel extends Kernel {
    constructor(
        kernelName?: string,
        private readonly htmlFragmentInserter?: (htmlFragment: string) => Promise<string>,
        private readonly htmlFragmentReplacer?: (elementSelector: string, replacementHtml: string) => Promise<void>,
        languageName?: string,
        languageVersion?: string) {

        super(kernelName ?? "html", languageName ?? "HTML", languageVersion ?? "5");
        this.kernelInfo.displayName = 'HTML';

        if (!this.htmlFragmentInserter) {
            this.htmlFragmentInserter = htmlDomFragmentInserter;
        }

        if (!this.htmlFragmentReplacer) {
            this.htmlFragmentReplacer = htmlDomFragmentReplacer;
        }

        this.registerCommandHandler({ commandType: commandsAndEvents.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.ReplaceHtmlType, handle: invocation => this.handleReplaceHtml(invocation) });
    }

    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
        const submitCode = <commandsAndEvents.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        const codeSubmissionReceivedEvent = new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.CodeSubmissionReceivedType,
            { code },
            invocation.commandEnvelope);

        invocation.context.publish(codeSubmissionReceivedEvent);

        if (!this.htmlFragmentInserter) {
            throw new Error("No HTML fragment processor registered");
        }

        const formattedValue = await this.htmlFragmentInserter(code);
        const displayedValueProduced: commandsAndEvents.DisplayedValueProduced = {
            formattedValues: [{
                mimeType: "text/html",
                value: formattedValue
            }]
        };

        const valueProducedEvent = new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.DisplayedValueProducedType,
            { formattedValues: displayedValueProduced.formattedValues },
            invocation.commandEnvelope);

        invocation.context.publish(valueProducedEvent);
    }

    private async handleReplaceHtml(invocation: IKernelCommandInvocation): Promise<void> {
        const replaceHtml = <commandsAndEvents.ReplaceHtml>invocation.commandEnvelope.command;

        if (!this.htmlFragmentReplacer) {
            throw new Error("No HTML fragment processor registered");
        }

        await this.htmlFragmentReplacer(replaceHtml.elementSelector, replaceHtml.replacementHtml);
    }
}

export interface HtmlDomFragmentInserterConfiguration {
    getOrCreateContainer?: () => HTMLElement,
    updateContainerContent?: (container: HTMLElement, htmlFragment: string) => void,
    createMutationObserver?: (callback: MutationCallback) => MutationObserver,
    normalizeHtmlFragment?: (htmlFragment: string) => string,
    jsEvaluator?: (js: string) => Promise<void>,
    selectElement?: (elementSelector: string) => HTMLElement | null,
};

export type HtmlKernelInBrowserConfiguration =
    { kernelName: string, container: HTMLElement | string, contentBehaviour: "append" | "replace" } |
    { kernelName: string, htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration };

export function htmlDomFragmentInserter(htmlFragment: string, configuration?: HtmlDomFragmentInserterConfiguration): Promise<string> {

    const getOrCreateContainer = configuration?.getOrCreateContainer ?? (() => {
        const container = document.createElement("div");
        document.body.appendChild(container);
        return container;
    });

    const nomarlizeFragment = configuration?.normalizeHtmlFragment ?? normalizeHtmlFragment;
    const updateContainerContent = configuration?.updateContainerContent ?? ((container, htmlFragment) => container.innerHTML = htmlFragment);
    const createMutationObserver = configuration?.createMutationObserver ?? (callback => new MutationObserver(callback));

    let container = getOrCreateContainer();
    const normalizedHtmlFragment = nomarlizeFragment(htmlFragment);
    const completionPromise = setupMutationObserver(createMutationObserver, container, normalizedHtmlFragment);
    updateContainerContent(container, normalizedHtmlFragment);

    return completionPromise.promise.then(() => {
        evaluateScripts(container, configuration);
        return container.innerHTML;
    });
}

export function htmlDomFragmentReplacer(elementSelector: string, replacementHtml: string, configuration?: HtmlDomFragmentInserterConfiguration): Promise<void> {

    if (!elementSelector) {
        throw new Error(`An element selector must be specified`);
    }

    const selectElement = configuration?.selectElement ?? ((selector) => document.querySelector(selector));
    const element = selectElement(elementSelector);
    if (!element) {
        throw new Error(`Element selector must match exactly one element`);
    }

    const container = element.parentElement;
    if (!container) {
        throw new Error(`Element selector must not select root element`);
    }

    const nomarlizeFragment = configuration?.normalizeHtmlFragment ?? normalizeHtmlFragment;
    const createMutationObserver = configuration?.createMutationObserver ?? (callback => new MutationObserver(callback));

    const normalizedReplacementHtml = nomarlizeFragment(replacementHtml);
    const completionPromise = setupMutationObserver(createMutationObserver, container, normalizedReplacementHtml);
    element.outerHTML = normalizedReplacementHtml;

    return completionPromise.promise.then(() => {
        evaluateScripts(container, configuration);
    });
}

function normalizeHtmlFragment(htmlFragment: string): string {
    const container = document.createElement("div");
    container.innerHTML = htmlFragment;
    return container.innerHTML;
}

function getJavaScriptEvaluator(): (code: string) => Promise<void> {
    const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
    return (code: string) => {
        const evaluator = AsyncFunction(code);
        return (<() => Promise<void>>evaluator)();
    };
}

function evaluateScripts(element: HTMLElement, configuration?: HtmlDomFragmentInserterConfiguration): void {
    let jsEvaluator = configuration?.jsEvaluator ?? getJavaScriptEvaluator();
    element.querySelectorAll("script").forEach(async (script) => {
        if (script.textContent) {
            try {
                await jsEvaluator(script.textContent);
            } catch (e: any) {
                Logger.default.error(e?.message ?? e);
            }
        }
    });
}

function setupMutationObserver(
    createMutationObserver: (callback: MutationCallback) => MutationObserver,
    container: HTMLElement,
    normalizedHtmlFragment: string): PromiseCompletionSource<void> {

    const completionPromise = new PromiseCompletionSource<void>();
    const mutationObserver = createMutationObserver((mutations: MutationRecord[], observer: MutationObserver) => {

        for (const mutation of mutations) {
            if (mutation.type === "childList" && container.innerHTML.includes(normalizedHtmlFragment)) {
                completionPromise.resolve();
                mutationObserver.disconnect();
                return;
            }
        }
    });

    mutationObserver.observe(container, { childList: true, subtree: true });
    return completionPromise;
}

export function createHtmlKernelForBrowser(config: HtmlKernelInBrowserConfiguration): Kernel {

    if (withfragmentInserterConfiguration(config)) {
        return new HtmlKernel(
            config.kernelName,
            (fragment) => htmlDomFragmentInserter(fragment, config.htmlDomFragmentInserterConfiguration),
            (elementSelector, replacementHtml) => htmlDomFragmentReplacer(elementSelector, replacementHtml, config.htmlDomFragmentInserterConfiguration));
    } else {
        const kernel = new HtmlKernel(
            config.kernelName,
            (htmlFragment: string) => htmlDomFragmentInserter(htmlFragment, {
                getOrCreateContainer: () => {
                    if (isHtmlElement(config.container)) {
                        return config.container;
                    } else {
                        const container = document.querySelector(config.container);
                        if (!container) {
                            throw new Error(`Container ${config.container} not found`);
                        }
                        return container as HTMLElement;
                    }
                },
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
}

function isHtmlElement(element: HTMLElement | string): element is HTMLElement {
    return typeof element === "object";
}

function withfragmentInserterConfiguration(config: any): config is { kernelName: string, htmlDomFragmentInserterConfiguration: HtmlDomFragmentInserterConfiguration } {
    return config?.htmlDomFragmentInserterConfiguration !== undefined;
}