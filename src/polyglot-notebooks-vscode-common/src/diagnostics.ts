// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

let diagnosticCollectionMap: Map<string, vscode.DiagnosticCollection> = new Map();

export function getDiagnosticCollection(cellUri: vscode.Uri): vscode.DiagnosticCollection {
    const key = cellUri.toString();
    let collection = diagnosticCollectionMap.get(key);
    if (!collection) {
        collection = vscode.languages.createDiagnosticCollection();
        diagnosticCollectionMap.set(key, collection);
    }

    collection.clear();
    return collection;
}
