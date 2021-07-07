// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SubmitCodeType, SubmitCode, CodeSubmissionReceivedType, ReturnValueProducedType, ReturnValueProduced, FormattedValue, Disposable } from "../interfaces/contracts";
import { ConsoleCapture } from "./consoleCapture";
import { IKernelCommandInvocation, Kernel } from "./kernel";

export class JavascriptKernel extends Kernel {

    constructor() {
        super("javascript");
        this.registerCommandHandler({ commandType: SubmitCodeType, handle: this.handleSubmitCode });
    }
    private async handleSubmitCode(invocation: IKernelCommandInvocation): Promise<void> {
        const submitCode = <SubmitCode>invocation.commandEnvelope.command;
        const code = submitCode.code;

        invocation.context.publish({ eventType: CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });


        let capture: Disposable | undefined = new ConsoleCapture(invocation.context);
        let result: any = undefined;

        try {

            const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);

            const evaluator = AsyncFunction("console", code);

            result = await evaluator(capture);
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
        const formattedValue = formatValue(result);
        if (formattedValue) {

            const event: ReturnValueProduced = {
                formattedValues: [formattedValue]
            };

            invocation.context.publish({ eventType: ReturnValueProducedType, event, command: invocation.commandEnvelope });
        }
        return Promise.resolve();
    }
}

export function formatValue(arg: any): FormattedValue | undefined {
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