// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface Data {
    schema: Schema;
    data: Datapoint[];
}
export interface Schema {
    fields: Field[];
    pandas_version?: string;
    primaryKey?: string[];
}

export interface Field {
    name: string;
    type: string;
}

export interface Datapoint {
    [fieldName: string]: any;
}

// this type follow the schema published at https://specs.frictionlessdata.io//tabular-data-resource/
export interface TabularDataResource {
    schema: {
        fields: Array<{ name: string, type: string }>,
        primaryKey: string
    },
    data: Array<{
        [key: string]: any
    }>
}