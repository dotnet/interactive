// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./commandsAndEvents";
import * as connection from "./connection";
import { ConsoleCapture } from "./consoleCapture";
import { Kernel, IKernelCommandInvocation } from "./kernel";
import { Logger } from "./logger";
import * as polyglotNotebooksApi from "./api";

export class JavascriptKernel extends Kernel {
    private suppressedLocals: Set<string>;
    private capture: ConsoleCapture;

    constructor(name?: string) {
        super(name ?? "javascript", "JavaScript");
        this.kernelInfo.displayName = `${this.kernelInfo.localName} - ${this.kernelInfo.languageName}`;
        this.suppressedLocals = new Set<string>(this.allLocalVariableNames());
        this.registerCommandHandler({ commandType: commandsAndEvents.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.RequestValueType, handle: invocation => this.handleRequestValue(invocation) });
        this.registerCommandHandler({ commandType: commandsAndEvents.SendValueType, handle: invocation => this.handleSendValue(invocation) });

        this.capture = new ConsoleCapture();
    }

    private handleSendValue(invocation: IKernelCommandInvocation): Promise<void> {
        const sendValue = <commandsAndEvents.SendValue>invocation.commandEnvelope.command;
        if (sendValue.formattedValue) {
            switch (sendValue.formattedValue.mimeType) {
                case 'application/json':
                    (<any>globalThis)[sendValue.name] = connection.Deserialize(sendValue.formattedValue.value);
                    break;
                default:
                    (<any>globalThis)[sendValue.name] = sendValue.formattedValue.value;
                    break;
            }
            return Promise.resolve();
        }
        throw new Error("formattedValue is required");
    }

    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
        const submitCode = <commandsAndEvents.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        super.kernelInfo.localName;//?
        super.kernelInfo.uri;//?
        super.kernelInfo.remoteUri;//?
        const codeSubmissionReceivedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.CodeSubmissionReceivedType, { code }, invocation.commandEnvelope);
        invocation.context.publish(codeSubmissionReceivedEvent);
        invocation.context.commandEnvelope.routingSlip;//?
        this.capture.kernelInvocationContext = invocation.context;
        let result: any = undefined;

        try {
            const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
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
        } catch (e) {
            throw e;//?
        }
        finally {
            this.capture.kernelInvocationContext = undefined;
        }
    }

    private handleRequestValueInfos(invocation: IKernelCommandInvocation): Promise<void> {
        const valueInfos: commandsAndEvents.KernelValueInfo[] = this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).map(v => (
            {
                name: v,
                typeName: getType(this.getLocalVariable(v)),
                formattedValue: formatValue(this.getLocalVariable(v), "text/plain"),
                preferredMimeTypes: []
            }));

        const event: commandsAndEvents.ValueInfosProduced = {
            valueInfos
        };

        const valueInfosProducedEvent = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.ValueInfosProducedType, event, invocation.commandEnvelope);
        invocation.context.publish(valueInfosProducedEvent);
        return Promise.resolve();
    }

    private handleRequestValue(invocation: IKernelCommandInvocation): Promise<void> {
        const requestValue = <commandsAndEvents.RequestValue>invocation.commandEnvelope.command;
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
                    if (typeof (<any>globalThis)[key] !== 'function') {
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
        return (<any>globalThis)[name];
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
