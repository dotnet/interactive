import { Observable } from "rxjs";
import { EventEnvelope } from "./events";
import { CellKind, CellOutput } from "./interfaces/vscode";

export interface RawNotebookCell {
    kind: CellKind;
    content: string;
    language: string;
    outputs: Array<CellOutput>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}

export interface ClientAdapter {
    submitCommand: { (commandType: string, command: any, targetKernelName: string): Observable<EventEnvelope> };
}
