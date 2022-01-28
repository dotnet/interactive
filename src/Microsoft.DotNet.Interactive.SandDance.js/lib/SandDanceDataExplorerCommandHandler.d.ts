import { Explorer_Class } from '@msrvida/sanddance-explorer';
import { Data } from "./dataTypes";
export declare class SandDanceDataExplorerCommandHandler {
    readonly id: string;
    private explorer;
    private data;
    constructor(id: string);
    setExplorer(explorer: Explorer_Class): void;
    loadData(data: Data): void;
}
