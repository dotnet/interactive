export interface InteractiveLaunchOptions {
    args: Array<string>;
    workingDirectory?: string | undefined;
}

export interface RawNotebookCell {
    language: string;
    contents: Array<string>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}
