"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.getDiagnosticCollection = void 0;
const vscode = require("vscode");
let diagnosticCollectionMap = new Map();
function getDiagnosticCollection(cellUri) {
    const key = cellUri.toString();
    let collection = diagnosticCollectionMap.get(key);
    if (!collection) {
        collection = vscode.languages.createDiagnosticCollection();
        diagnosticCollectionMap.set(key, collection);
    }
    collection.clear();
    return collection;
}
exports.getDiagnosticCollection = getDiagnosticCollection;
//# sourceMappingURL=diagnostics.js.map