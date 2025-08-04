// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./commandsAndEvents";
import * as connection from "./connection";
import { ConsoleCapture } from "./consoleCapture";
import { Kernel, IKernelCommandInvocation } from "./kernel";
import { Logger } from "./logger";
import * as polyglotNotebooksApi from "./api";

// This is a workaround for rollup warnings. See their documentation for more details: https://rollupjs.org/troubleshooting/#avoiding-eval
const eval2 = eval;

export class JavascriptKernel extends Kernel {
    private suppressedLocals: Set<string>;
    private capture: ConsoleCapture;

    constructor(name?: string) {
        super(name ?? "javascript", "JavaScript");
        this.kernelInfo.displayName = `${this.kernelInfo.localName} - ${this.kernelInfo.languageName}`;
        this.kernelInfo.description = `Run JavaScript code`;
        this.suppressedLocals = new Set<string>(this.allLocalVariableNames());
        this.registerCommandHandler({ commandType: commandsAndEvents.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.RequestValueType, handle: invocation => this.handleRequestValue(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.SendValueType, handle: invocation => this.handleSendValue(invocation) });

        this.capture = new ConsoleCapture();
    }

    private handleSendValue(invocation: IKernelCommandInvocation): Promise<void> {
        const sendValue = invocation.commandEnvelope.command as commandsAndEvents.SendValue;
        if (sendValue.formattedValue) {
            switch (sendValue.formattedValue.mimeType) {
                case 'application/json':
                    (globalThis as any)[sendValue.name] = connection.Deserialize(sendValue.formattedValue.value);
                    break;
                default:
                    (globalThis as any)[sendValue.name] = sendValue.formattedValue.value;
                    break;
            }
            return Promise.resolve();
        }
        throw new Error("formattedValue is required");
    }

    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
        const submitCode = invocation.commandEnvelope.command as commandsAndEvents.SubmitCode;
        const code = submitCode.code;

        super.kernelInfo.localName;
        super.kernelInfo.uri;
        super.kernelInfo.remoteUri;
        const codeSubmissionReceivedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CodeSubmissionReceivedType, { code }, invocation.commandEnvelope);
        invocation.context.publish(codeSubmissionReceivedEvent);
        invocation.context.commandEnvelope.routingSlip;
        this.capture.kernelInvocationContext = invocation.context;
        let result: any = undefined;

        try {
            const AsyncFunction = eval2(`Object.getPrototypeOf(async function(){}).constructor`);
            const evaluator = AsyncFunction("console", "polyglotNotebooks", code);
            result = await evaluator(this.capture, polyglotNotebooksApi);
            if (result !== undefined) {
                const formattedValue = formatValue(result, 'application/json');
                const event: commandsAndEvents.ReturnValueProduced = {
                    formattedValues: [formattedValue]
                };
                const returnValueProducedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ReturnValueProducedType, event, invocation.commandEnvelope);
                invocation.context.publish(returnValueProducedEvent);
            }
        } catch (e: any) {
            const errorProduced = new commandsAndEvents.KernelEventEnvelope(
                commandsAndEvents.ErrorProducedType,
                {
                    message:
                        `${e.message}

                    ${e.stack}`
                },
                invocation.commandEnvelope);
            invocation.context.publish(errorProduced);
        }
        finally {
            this.capture.kernelInvocationContext = undefined;
        }
    }

    private handleRequestValueInfos(invocation: IKernelCommandInvocation): Promise<void> {
        const valueInfos: commandsAndEvents.KernelValueInfo[] = [];

        this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).forEach(v => {
            const variableValue = this.getLocalVariable(v);
            try {
                const valueInfo = {
                    name: v,
                    typeName: getType(variableValue),
                    formattedValue: formatValue(variableValue, "text/plain"),
                    preferredMimeTypes: []
                };
                valueInfos.push(valueInfo);
            } catch (e) {
                Logger.default.error(`error formatting value ${v} : ${e}`);
            }
        });

        const event: commandsAndEvents.ValueInfosProduced = {
            valueInfos
        };

        const valueInfosProducedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueInfosProducedType, event, invocation.commandEnvelope);
        invocation.context.publish(valueInfosProducedEvent);
        return Promise.resolve();
    }

    private handleRequestValue(invocation: IKernelCommandInvocation): Promise<void> {
        const requestValue = invocation.commandEnvelope.command as commandsAndEvents.RequestValue;
        const rawValue = this.getLocalVariable(requestValue.name);
        const formattedValue = formatValue(rawValue, requestValue.mimeType || 'application/json');
        Logger.default.info(`returning ${JSON.stringify(formattedValue)} for ${requestValue.name}`);
        const event: commandsAndEvents.ValueProduced = {
            name: requestValue.name,
            formattedValue
        };

        const valueProducedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueProducedType, event, invocation.commandEnvelope);
        invocation.context.publish(valueProducedEvent);
        return Promise.resolve();
    }

    public allLocalVariableNames(): string[] {
        const result: string[] = [];
        try {
            for (const key in globalThis) {
                try {
                    if (typeof (globalThis as any)[key] !== 'function') {
                        result.push(key);
                    }
                } catch (e) {
                    Logger.default.error(`error getting value for ${key} : ${e}`);
                }
            }
        } catch (e) {
            Logger.default.error(`error scanning globla variables : ${e}`);
        }

        return result;
    }

    public getLocalVariable(name: string): any {
        return (globalThis as any)[name];
    }
}

export function formatValue(arg: any, mimeType: string): commandsAndEvents.FormattedValue {
    let value: string;

    switch (mimeType) {
        case 'text/plain':
            value = arg?.toString() || 'undefined';
            if (Array.isArray(arg)) {
                value = `[${value}]`;
            }
            break;
        case 'application/json':
            value = connection.Serialize(arg);
            break;
        default:
            throw new Error(`unsupported mime type: ${mimeType}`);
    }

    return {
        mimeType,
        value,
        suppressDisplay: false
    };
}

export function getType(arg: any): string {
    let type: string = arg ? typeof (arg) : "";//?

    if (Array.isArray(arg)) {
        type = `${typeof (arg[0])}[]`;//?
    }

    if (arg === Infinity || arg === -Infinity || (arg !== arg)) {
        type = "number";
    }

    return type; //?
}

