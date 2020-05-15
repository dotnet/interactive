import * as vscode from 'vscode';
import * as path from 'path';
import { ClientMapper } from './../clientMapper';
import { NotebookFile, parseNotebook, serializeNotebook, editorLanguages } from '../interactiveNotebook';
import { RawNotebookCell } from '../interfaces';
import { JupyterNotebook } from '../interfaces/jupyter';
import { convertFromJupyter } from '../interop/jupyter';
import { trimTrailingCarriageReturn } from '../utilities';
import { KernelEventEnvelope, DisplayedValueProduced, DisplayedValueProducedType, ReturnValueProducedType } from '../contracts';
import { CellDisplayOutput, CellOutputKind, CellOutput } from '../interfaces/vscode';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider {
    private deferredOutput: Array<CellOutput> = [];

    constructor(readonly clientMapper: ClientMapper) {
    }

    async openNotebook(uri: vscode.Uri): Promise<vscode.NotebookData> {
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(uri)).toString('utf-8');
        } catch {
        }

        let notebook: NotebookFile;
        let extension = getExtension(uri.path);
        switch (extension) {
            case '.ipynb':
                let json = JSON.parse(contents);
                let jupyter = <JupyterNotebook>json;
                notebook = convertFromJupyter(jupyter);
                break;
            case '.dotnet-interactive':
            default:
                notebook = parseNotebook(contents);
                break;
        }

        let client = this.clientMapper.getOrAddClient(uri);
        let notebookPath = path.dirname(uri.fsPath);

        let notebookData: vscode.NotebookData = {
            languages: editorLanguages,
            metadata: {
                hasExecutionOrder: false
            },
            cells: notebook.cells.map(toNotebookCellData)
        };

        client.setDeferredCommandEventsListener((eventEnvelope) => {
            switch (eventEnvelope.eventType) {
                case DisplayedValueProducedType:
                case DisplayedValueProducedType:
                case ReturnValueProducedType:
                    let event = <DisplayedValueProduced>eventEnvelope.event;

                    let data: { [key: string]: any } = {};
                    if (event.formattedValues && event.formattedValues.length > 0) {
                        for (let formatted of event.formattedValues) {
                            let value: any = formatted.mimeType === 'application/json'
                                ? JSON.parse(formatted.value)
                                : formatted.value;
                            data[formatted.mimeType] = value;
                        }
                    } else if (event.value) {
                        // no formatted values returned, this is the best we can do
                        data['text/plain'] = event.value.toString();
                    }

                    let output: CellDisplayOutput = {
                        outputKind: CellOutputKind.Rich,
                        data: data,
                    };
                    this.deferredOutput.push(output);
                    break;
            }

        });

        client.changeWorkingDirectory(notebookPath).catch((err) => {
            let message = `Unable to set notebook working directory to '${notebookPath}'.`;
            if (err && err.message) {
                message += '\n' + err.message.toString();
            }

            vscode.window.showInformationMessage(message);
        });

        return notebookData;
    }

    saveNotebook(document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, document.uri);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, targetResource);
    }

    onDidChangeNotebook: vscode.Event<void> = new vscode.EventEmitter<void>().event;

    executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell | undefined, token: vscode.CancellationToken): Promise<void> {
        if (!cell) {
            // TODO: run everything
            return new Promise((resolve, reject) => resolve());
        }

        cell.outputs = [];
        let client = this.clientMapper.getOrAddClient(document.uri);
        let source = cell.source.toString();
        return client.execute(source, cell.language, outputs => {
            let cellOutput = [...this.deferredOutput, ...outputs];
            this.deferredOutput = [];

            // to properly trigger the UI update, `cell.outputs` needs to be uniquely assigned; simply setting it to the local variable has no effect
            cell.outputs = [];
            cell.outputs = cellOutput;
        });
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        let notebook: NotebookFile = {
            cells: [],
        };
        for (let cell of document.cells) {
            notebook.cells.push({
                language: cell.language,
                contents: cell.document.getText().split('\n').map(trimTrailingCarriageReturn),
            });
        }

        await vscode.workspace.fs.writeFile(targetResource, Buffer.from(serializeNotebook(notebook)));
    }
}

function toNotebookCellData(cell: RawNotebookCell): vscode.NotebookCellData {
    return {
        cellKind: languageToCellKind(cell.language),
        source: cell.contents.join('\n'),
        language: cell.language,
        outputs: [],
        metadata: {}
    };
}

function getExtension(filename: string): string {
    let dot = filename.indexOf('.');
    if (dot < 0) {
        return '';
    }

    return filename.substr(dot);
}

function languageToCellKind(language: string): vscode.CellKind {
    switch (language) {
        case 'md':
        case 'markdown':
            return vscode.CellKind.Markdown;
        default:
            return vscode.CellKind.Code;
    }
}
