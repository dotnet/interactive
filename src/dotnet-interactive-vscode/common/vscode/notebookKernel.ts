// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { ClientMapper } from "../clientMapper";
import { notebookCellLanguages } from "../interactiveNotebook";
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from '../ipynbUtilities';

import * as versionSpecificFunctions from '../../versionSpecificFunctions';

export const KernelId: string = 'dotnet-interactive';

export class DotNetInteractiveNotebookKernel implements vscode.NotebookKernel {
    id: string = KernelId;
    label: string;
    description?: string | undefined;
    detail?: string | undefined;
    isPreferred: boolean;
    preloads?: vscode.Uri[] | undefined;
    supportedLanguages: Array<string>;

    constructor(readonly clientMapper: ClientMapper, apiBootstrapperUri: vscode.Uri, isPreferred: boolean) {
        this.label = ".NET Interactive";
        this.preloads = [apiBootstrapperUri];
        this.isPreferred = isPreferred;
        this.supportedLanguages = notebookCellLanguages;
    }

    /////////////////////////////////////////////////////////////////////////// required for stable

    async executeAllCells(document: vscode.NotebookDocument): Promise<void> {
        for (const cell of document.cells) {
            await versionSpecificFunctions.executeCell(document, cell, this.clientMapper);
        }
    }

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        return versionSpecificFunctions.executeCell(document, cell, this.clientMapper);
    }

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        return versionSpecificFunctions.cancelCellExecution(document, cell, this.clientMapper);
    }

    cancelAllCellsExecution(document: vscode.NotebookDocument): void {
        // not supported
    }

    ///////////////////////////////////////////////////////////////////////// required for insiders

    async executeCellsRequest(document: vscode.NotebookDocument, ranges: vscode.NotebookCellRange[]): Promise<void> {
        for (const range of ranges) {
            for (let cellIndex = range.start; cellIndex < range.end; cellIndex++) {
                const cell = document.cells[cellIndex];
                await versionSpecificFunctions.executeCell(document, cell, this.clientMapper);
            }
        }
    }

    interrupt(document: vscode.NotebookDocument) {
        // not supported
    }
}

export async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);

    // workaround for https://github.com/microsoft/vscode/issues/115912; capture all cell data so we can re-apply it at the end
    const cellData: Array<vscode.NotebookCellData> = document.cells.map(c => {
        return versionSpecificFunctions.createVsCodeNotebookCellData({
            cellKind: versionSpecificFunctions.getCellKind(c),
            source: c.document.getText(),
            language: c.document.languageId,
            outputs: c.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            metadata: c.metadata
        });
    });

    edit.replaceNotebookMetadata(document.uri, documentKernelMetadata);

    // this is the re-application for the workaround mentioned above
    edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellData);

    await vscode.workspace.applyEdit(edit);
}

export async function updateCellLanguages(document: vscode.NotebookDocument): Promise<void> {
    const documentLanguageInfo = getLanguageInfoMetadata(document.metadata);

    // update cell language
    let applyUpdate = false;
    let cellDatas: Array<vscode.NotebookCellData> = [];
    for (const cell of document.cells) {
        const cellMetadata = getDotNetMetadata(cell.metadata);
        const cellText = cell.document.getText();
        const newLanguage = getCellLanguage(cellText, cellMetadata, documentLanguageInfo, cell.document.languageId);
        const cellData = versionSpecificFunctions.createVsCodeNotebookCellData({
            cellKind: versionSpecificFunctions.getCellKind(cell),
            source: cellText,
            language: newLanguage,
            outputs: cell.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            metadata: cell.metadata,
        });
        cellDatas.push(cellData);
        applyUpdate ||= cell.document.languageId !== newLanguage;
    }

    if (applyUpdate) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellDatas);
        await vscode.workspace.applyEdit(edit);
    }
}
