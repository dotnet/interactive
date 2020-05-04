import { Observable } from "rxjs";
import { EventEnvelope } from "./events";
import { Command } from "./commands";

export interface RawNotebookCell {
    language: string;
    contents: Array<string>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}

export interface ClientTransport {
    submitCommand: { (commandType: string, command: Command, targetKernelName: string): Observable<EventEnvelope> };
}
