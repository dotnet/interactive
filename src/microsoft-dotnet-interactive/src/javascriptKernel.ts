// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { ConsoleCapture } from "./consoleCapture";
import * as kernel from "./kernel";

export class JavascriptKernel extends kernel.Kernel {

    constructor() {
        super("javascript");
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: this.handleSubmitCode });
    }
    private async handleSubmitCode(invocation: kernel.IKernelCommandInvocation): Promise<void> {
        const submitCode = <contracts.SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        invocation.context.publish({ eventType: contracts.CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });

        let capture: contracts.Disposable | undefined = new ConsoleCapture(invocation.context);
        let result: any = undefined;

        try {
            const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
            const evaluator = AsyncFunction("console", code);
            result = await evaluator(capture);
            const formattedValue = formatValue(result);
            if (formattedValue) {
                const event: contracts.ReturnValueProduced = {
                    formattedValues: [formattedValue]
                };
                invocation.context.publish({ eventType: contracts.ReturnValueProducedType, event, command: invocation.commandEnvelope });
            }
        } catch (e) {
            capture.dispose();
            capture = undefined;

            throw e;
        }
        finally {
            if (capture) {
                capture.dispose();
            }
        }
    }
}

export function formatValue(arg: any): contracts.FormattedValue | undefined {
    if (arg === undefined) {
        return undefined;
    }
    let mimeType: string;
    let value: string;

    if (typeof arg !== 'object' && !Array.isArray(arg)) {
        mimeType = 'text/plain';
        value = arg.toString();
    } else {
        mimeType = 'application/json';
        value = JSON.stringify(arg);
    }

    return {
        mimeType,
        value,
    };
}