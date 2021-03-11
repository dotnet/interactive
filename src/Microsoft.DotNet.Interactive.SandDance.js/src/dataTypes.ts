// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface Data {
    schema: TableSchema;
    data: Datapoint[];
}
export interface TableSchema {
    fields: TableSchemaFieldDescriptor[];
    pandas_version?: string;
    primaryKey?: string[];
}

export interface TableSchemaFieldDescriptor {
    name: string;
    type: string;
    description?: string;
    format?: string;
}

export interface Datapoint {
    [fieldName: string]: any;
}

// this type follow the schema published at https://specs.frictionlessdata.io//tabular-data-resource/
export interface TabularDataResource {
    profile: string;
    schema: {
        fields: Array<{ name: string, type: string }>,
        primaryKey: string
    },
    data: Array<{
        [key: string]: any
    }>
}