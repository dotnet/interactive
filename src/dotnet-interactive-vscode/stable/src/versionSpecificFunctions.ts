// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as utilities from './common/interfaces/utilities';
import { contractCellOutputToVsCodeCellOutput } from './common/vscode/notebookContentProvider';

export function getCellKind(cell: vscode.NotebookCell): vscode.NotebookCellKind {
    return cell.cellKind;
}

export function createVsCodeNotebookCellData(cellData: { cellKind: vscodeLike.NotebookCellKind, source: string, language: string, outputs: contracts.NotebookCellOutput[], metadata?: vscode.NotebookCellMetadata }): vscode.NotebookCellData {
    return {
        cellKind: cellData.cellKind,
        source: cellData.source,
        language: cellData.language,
        outputs: cellData.outputs.map(contractCellOutputToVsCodeCellOutput),
        metadata: cellData.metadata,
    };
}
