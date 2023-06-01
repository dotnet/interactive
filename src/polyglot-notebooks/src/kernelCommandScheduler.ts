// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommandEnvelope } from "./commandsAndEvents";
import { KernelScheduler } from "./kernelScheduler";

export class KernelCommandScheduler extends KernelScheduler<KernelCommandEnvelope>{
    constructor() {
        super();
    }
}