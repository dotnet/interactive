"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.notebookCellChanged = exports.backupNotebook = exports.languageToCellKind = exports.isJupyterNotebookViewType = void 0;
const fs = require("fs");
const path = require("path");
const utilities_1 = require("./utilities");
const vscode_like_1 = require("./interfaces/vscode-like");
const constants = require("./constants");
function isJupyterNotebookViewType(viewType) {
    return viewType === constants.JupyterViewType;
}
exports.isJupyterNotebookViewType = isJupyterNotebookViewType;
function languageToCellKind(language) {
    switch (language) {
        case 'markdown':
            return vscode_like_1.NotebookCellKind.Markup;
        default:
            return vscode_like_1.NotebookCellKind.Code;
    }
}
exports.languageToCellKind = languageToCellKind;
function backupNotebook(rawData, location) {
    return new Promise((resolve, reject) => {
        // ensure backup directory exists
        const parsedPath = path.parse(location);
        fs.mkdir(parsedPath.dir, { recursive: true }, (err, _path) => __awaiter(this, void 0, void 0, function* () {
            if (err) {
                reject(err);
                return;
            }
            // save notebook to location
            fs.writeFile(location, rawData, () => {
                resolve({
                    id: location,
                    delete: () => {
                        fs.unlinkSync(location);
                    }
                });
            });
        }));
    });
}
exports.backupNotebook = backupNotebook;
function notebookCellChanged(clientMapper, documentUri, documentText, kernelName, diagnosticDelay, callback) {
    (0, utilities_1.debounce)(`diagnostics-${documentUri.toString()}`, diagnosticDelay, () => __awaiter(this, void 0, void 0, function* () {
        let diagnostics = [];
        try {
            const client = yield clientMapper.getOrAddClient(documentUri);
            diagnostics = yield client.getDiagnostics(kernelName, documentText);
        }
        finally {
            callback(diagnostics);
        }
    }));
}
exports.notebookCellChanged = notebookCellChanged;
//# sourceMappingURL=interactiveNotebook.js.map