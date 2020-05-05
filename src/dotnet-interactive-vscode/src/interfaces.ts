import { Observable } from "rxjs";
import { KernelCommand, KernelCommandType, KernelEventEnvelope } from "./contracts";

export interface RawNotebookCell {
    language: string;
    contents: Array<string>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}

export interface ClientTransport {
    submitCommand: { (commandType: KernelCommandType, command: KernelCommand, targetKernelName: string): Observable<KernelEventEnvelope> };
}
