import * as cp from 'child_process';
import * as vscode from 'vscode';
import { Writable } from 'stream';

interface RawNotebookCell {
    kind: vscode.CellKind;
    language: string;
    content: string;
    outputs: { [key: string]: any }[];
}

interface InteractiveEvent {
    eventType: string;
    event: any;
    cause: {token: string};
}

export class DotNetInteractiveNotebookProvider implements vscode.NotebookProvider {
    private buffer: string = "";
    private next: number = 1;
    private stdin: Writable;
    private callbacks: Map<string, {(event: InteractiveEvent): void}[]> = new Map();

    constructor() {
        let childProcess = cp.spawn('dotnet', ['interactive', 'stdio']);
        childProcess.on('exit', (code: number, _signal: string) => {
            //
            let x = 1;
        });
        childProcess.stdout.on('data', (data) => {
            let str: string = data.toString();
            this.buffer += str;

            let i = this.buffer.indexOf('\n');
            while (i >= 0) {
                let temp = this.buffer.substr(0, i + 1);
                this.buffer = this.buffer.substr(i + 1);
                i = this.buffer.indexOf('\n');
                let obj = JSON.parse(temp);
                try {
                    let event = <InteractiveEvent>obj;
                    let callbacks = this.callbacks.get(event.cause.token);
                    if (callbacks) {
                        for (let callback of callbacks) {
                            callback(event);
                        }
                    }
                } catch {
                }
            }
        });

        this.stdin = childProcess.stdin;
    }

    private registerCallback(token: string, callback: {(event: InteractiveEvent): void}) {
        if (!this.callbacks.has(token)) {
            this.callbacks.set(token, []);
        }

        this.callbacks.get(token)?.push(callback);
    }

    async submitCode(code: string, returnValueProducedCallback: {(returnValueProduced: any): void}) {
        /*
        let sampleReturnEvent = {
            eventType: "ReturnValueProduced",
            event: {
                value: 2,
                formattedValues: [
                    {
                        mimeType: "text/html",
                        value: "2"
                    }
                ],
                valueId: null
            },
            cause: {
                token: "abc",
                commandType: "SubmitCode",
                command: {
                    code: "1+1",
                    submissionType: 0,
                    targetKernelName: null
                }
            }
        };
        // */

        let token = "abc" + this.next++;
        let submit = {
            token: token,
            commandType: "SubmitCode",
            command: {
                code: code,
                submissionType: 0,
                targetKernelName: null
            }
        };

        this.registerCallback(token, (event) => {
            if (event.eventType === "ReturnValueProduced") {
                returnValueProducedCallback(event.event);
            }
        });

        let str = JSON.stringify(submit);
        this.stdin.write(str);
        this.stdin.write("\n");
    }

    async resolveNotebook(editor: vscode.NotebookEditor): Promise<void> {
        editor.document.languages = ['dotnet-interactive'];

        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(editor.document.uri)).toString('utf-8');
        } catch {
        }

        let rawCells: RawNotebookCell[];
        try {
            rawCells = <RawNotebookCell[]>JSON.parse(contents);
        } catch {
            rawCells = [];
        }

        editor.edit(editBuilder => {
            for (let rawCell of rawCells) {
                editBuilder.insert(
                    0,
                    rawCell.content,
                    rawCell.language,
                    rawCell.kind,
                    [], // TODO: load cell outputs?
                    {
                        editable: true,
                        runnable: true
                    }
                );
            }
        });

        setTimeout(() => {
            for (let cell of editor.document.cells) {
                if (cell.cellKind === vscode.CellKind.Code) {
                    //
                }
            }
        }, 0);
    }

    executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell | undefined, token: vscode.CancellationToken): Promise<void> {
        if (!cell) {
            // TODO: run everything
            return Promise.resolve();
        }

        let source = cell.source.toString();
        this.submitCode(source, returnValueProduced => {
            let data: { [key: string]: any } = {};
            for (let formattedValue of returnValueProduced.formattedValues) {
                data[formattedValue.mimeType] = formattedValue.value;
            }

            let output: vscode.CellDisplayOutput = {
                outputKind: vscode.CellOutputKind.Rich,
                data: data
            };

            cell.outputs = [output];
        });

        return Promise.resolve();
    }

    async save(document: vscode.NotebookDocument): Promise<boolean> {
        let rawCells: RawNotebookCell[] = [];
        for (let cell of document.cells) {
            rawCells.push({
                language: cell.language,
                content: cell.document.getText(),
                outputs: [], // TODO: save cell outputs?
                kind: cell.cellKind
            });
        }

        await vscode.workspace.fs.writeFile(document.uri, Buffer.from(JSON.stringify(rawCells)));
        return true;
    }
}
