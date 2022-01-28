export interface DataExplorerSettings {
    container: HTMLDivElement;
    data: DataProps;
}
export interface DataProps {
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
export interface TabularDataResource {
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
export declare function createDataExplorer(settings: DataExplorerSettings): void;
