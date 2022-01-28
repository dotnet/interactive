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
export interface TabularDataResource {
    profile: string;
    schema: {
        fields: Array<{
            name: string;
            type: string;
        }>;
        primaryKey: string;
    };
    data: Array<{
        [key: string]: any;
    }>;
}
