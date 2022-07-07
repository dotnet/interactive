// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { ConsoleCapture } from "./consoleCapture";
import * as kernel from "./kernel";
import { Logger } from "./logger";

export class JavascriptKernel extends kernel.Kernel {
    private suppressedLocals: Set<string>;
    private capture: ConsoleCapture;

    constructor(name?: string) {
        super(name ?? "javascript", "Javascript");
        this.suppressedLocals = new Set<string>(this.allLocalVariableNames());
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        this.registerCommandHandler({ commandType: contracts.RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
        this.registerCommandHandler({ commandType: contracts.RequestValueType, handle: invocation => this.handleRequestValue(invocation) });

        this.capture = new ConsoleCapture();
    }

    private async handleSubmitCode(invocation: kernel.IKernelCommandInvocation): Promise<void> {
        const submitCode = <contracts.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        invocation.context.publish({ eventType: contracts.CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });

        this.capture.kernelInvocationContext = invocation.context;
        let result: any = undefined;

        try {
            const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
            const evaluator = AsyncFunction("console", code);
            result = await evaluator(this.capture);
            if (result !== undefined) {
                const formattedValue = formatValue(result, 'application/json');
                const event: contracts.ReturnValueProduced = {
                    formattedValues: [formattedValue]
                };
                invocation.context.publish({ eventType: contracts.ReturnValueProducedType, event, command: invocation.commandEnvelope });
            }
        } catch (e) {
            throw e;//?
        }
        finally {
            this.capture.kernelInvocationContext = undefined;
        }
    }

    private handleRequestValueInfos(invocation: kernel.IKernelCommandInvocation): Promise<void> {
        const valueInfos: contracts.KernelValueInfo[] = this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).map(v => ({ name: v }));
        const event: contracts.ValueInfosProduced = {
            valueInfos
        };
        invocation.context.publish({ eventType: contracts.ValueInfosProducedType, event, command: invocation.commandEnvelope });
        return Promise.resolve();
    }

    private handleRequestValue(invocation: kernel.IKernelCommandInvocation): Promise<void> {
        const requestValue = <contracts.RequestValue>invocation.commandEnvelope.command;
        const rawValue = this.getLocalVariable(requestValue.name);
        const formattedValue = formatValue(rawValue, requestValue.mimeType || 'application/json');
        Logger.default.info(`returning ${JSON.stringify(formattedValue)} for ${requestValue.name}`);
        const event: contracts.ValueProduced = {
            name: requestValue.name,
            formattedValue
        };
        invocation.context.publish({ eventType: contracts.ValueProducedType, event, command: invocation.commandEnvelope });
        return Promise.resolve();
    }

    private allLocalVariableNames(): string[] {
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

    private getLocalVariable(name: string): any {
        return (<any>globalThis)[name];
    }
}

export function formatValue(arg: any, mimeType: string): contracts.FormattedValue {
    let value: string;

    switch (mimeType) {
        case 'text/plain':
            value = arg?.toString() || 'undefined';
            break;
        case 'application/json':
            value = JSON.stringify(arg);
            break;
        default:
            throw new Error(`unsupported mime type: ${mimeType}`);
    }

    return {
        mimeType,
        value,
    };
}
