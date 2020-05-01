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
