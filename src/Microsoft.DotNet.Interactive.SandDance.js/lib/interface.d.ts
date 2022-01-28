import { Data } from "./dataTypes";
import "@msrvida/sanddance-explorer/dist/css/sanddance-explorer.css";
import "./app.css";
export interface DataExplorerSettings {
    container: HTMLDivElement;
    data: Data;
    id: string;
}
export declare function createSandDanceExplorer(settings: DataExplorerSettings): void;
