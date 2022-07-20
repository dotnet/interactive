// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from "../src/compositeKernel";
import { JavascriptKernel } from "../src/javascriptKernel";
import * as contracts from "../src/contracts";

describe("kernelRouting", () => {
    it.only("commands routing slip contains kernels that have been traversed", async () => {
        let composite = new CompositeKernel("vscode");
        composite.add(new JavascriptKernel("javascript"));
        composite.add(new JavascriptKernel("typescript"));

        composite.defaultKernelName = "typescript";

        let command: contracts.KernelCommandEnvelope = {
            commandType: contracts.SubmitCodeType,
            command: <contracts.SubmitCode>{
                code: "return 12;",
                targetKernelName: "javascript"
            }
        };

        let events: contracts.KernelEventEnvelope[] = [];

        composite.subscribeToKernelEvents(e => events.push(e));

        await composite.send(command);

        expect(command.routingSlip).not.to.be.undefined;

        expect(Array.from(command.routingSlip!.values())).to.deep.eq(
            [
                'kernel://local/vscode',
                'kernel://local/javascript']);
    });
});