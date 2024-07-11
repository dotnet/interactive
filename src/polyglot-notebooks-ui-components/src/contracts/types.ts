// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface VariableGridRow {
    id?: string;
    name: string;
    value: string;
    typeName: string;
    kernelDisplayName: string;
    kernelName: string;
}

export interface VariableInfo {
    sourceKernelName: string;
    valueName: string;
}

export interface GridLocalization {
    typeColumnHeader: string;
    valueColumnHeader: string;
    nameColumnHeader: string;
    actionsColumnHeader: string;
    kernelNameColumnHeader: string;
    shareTemplate: string;
    gridCaption: string;
}