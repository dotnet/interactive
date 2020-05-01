// set to match the Jupyter file format

export type JupyterStreamStdoutOutputType = 'stdout';
export type JupyterStreamStderrOutputType = 'stderr';
export type JupyterNamedStreamOutputType = JupyterStreamStdoutOutputType | JupyterStreamStderrOutputType;

export interface JupyterStreamOutput {
    output_type: 'stream';
    name: JupyterNamedStreamOutputType;
    text: string;
}

export interface JupyterDisplayDataOutput {
    output_type: 'display_data';
    data: { [mimeType: string]: any };
    metadata: { [mimeType: string]: any };
}

export interface JupyterExecuteResultOutput {
    output_type: 'execute_result';
    execution_count: number;
    data: { [mimeType: string]: any };
    metadata: { [mimeType: string]: any };
}

export interface JupyterErrorResultOutput {
    output_type: 'error';
    ename: string;
    evalue: string;
    traceback: Array<string>;
}

export type JupyterOutput = JupyterStreamOutput | JupyterDisplayDataOutput | JupyterExecuteResultOutput | JupyterErrorResultOutput;

export interface JupyterMarkdownCell {
    cell_type: 'markdown';
    metadata: { [key: string]: any };
    source: string;
}

export interface JupyterCodeCell {
    cell_type: 'code';
    execution_count: number;
    metadata: { [key: string]: any };
    source: Array<string>;
    outputs: Array<JupyterOutput>;
}

export type JupyterCell = JupyterMarkdownCell | JupyterCodeCell;

export interface JupyterKernelSpec {
    display_name: string;
    language: string;
    name: string;
}

export interface JupyterLanguageInfo {
    file_extension: string;
    mimetype: string;
    name: string;
    pygments_lexer: string;
    version: string;
}

export interface JupyterMetadata {
    kernelspec: JupyterKernelSpec;
    language_info: JupyterLanguageInfo;
}

export interface JupyterNotebook {
    cells: Array<JupyterCell>;
    metadata: JupyterMetadata;
    nbformat: number;
    nbformat_minor: number;
}
