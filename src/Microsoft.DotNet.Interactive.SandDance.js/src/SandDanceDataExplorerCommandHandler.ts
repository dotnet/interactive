// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { convertListOfRows } from "./dataConvertions";
import { Explorer_Class } from '@msrvida/sanddance-explorer';
import { Data } from "./dataTypes";

export class SandDanceDataExplorerCommandHandler {
    private explorer: Explorer_Class;
    private data: Data;

    constructor(public readonly id: string) {
    }

    public setExplorer(explorer: Explorer_Class): void {
        this.explorer = explorer;
    }

    public loadData(data: Data) {
        this.data = data;
        let converted = convertListOfRows(this.data);
        this.explorer.load(converted);
    }
}