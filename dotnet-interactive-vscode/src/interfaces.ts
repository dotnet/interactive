import { Observable } from "rxjs";
import { EventEnvelope } from "./events";

export interface RawNotebookCell {
    language: string;
    contents: Array<string>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}

export interface ClientTransport {
    submitCommand: { (commandType: string, command: any, targetKernelName: string): Observable<EventEnvelope> };
}
