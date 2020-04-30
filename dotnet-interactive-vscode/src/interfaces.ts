import { CellKind, CellOutput } from "./interfaces/vscode";

export interface RawNotebookCell {
    kind: CellKind;
    language: string;
    content: string;
    outputs: Array<CellOutput>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}
