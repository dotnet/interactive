// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";
import { KernelCommandScheduler } from "../src/kernelCommandScheduler";

describe('kernel-command-scheduler', () => {

    it('work can be scheduled from within scheduled work', async () => {
        const executionList: string[] = [];

        const scheduler = new KernelCommandScheduler<string>();

        await scheduler.runAsync('outer', async () => {
            executionList.push('outer 1');
            await scheduler.runAsync('inner', async () => {
                executionList.push("inner 1");
                await new Promise(resolve => setTimeout(resolve, 0));
                executionList.push("inner 2");
            });
            executionList.push('outer 1');
        });

        expect(executionList).to.deep.equal([
            'outer 1',
            'inner 1',
            'inner 2',
            'outer 1',
        ]);
    });

});
