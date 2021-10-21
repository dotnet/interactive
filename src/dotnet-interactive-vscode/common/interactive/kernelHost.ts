// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './compositeKernel';
import * as contracts from '../interfaces/contracts';

export class KernelHost {
    constructor(private readonly _kernel: CompositeKernel, private readonly _transport: contracts.Transport) {

    }
}