"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.ViewColumn = exports.NotebookCellRunState = exports.ErrorOutputMimeType = exports.NotebookCellKind = void 0;
// set to match vscode notebook types
var NotebookCellKind;
(function (NotebookCellKind) {
    NotebookCellKind[NotebookCellKind["Markup"] = 1] = "Markup";
    NotebookCellKind[NotebookCellKind["Code"] = 2] = "Code";
})(NotebookCellKind = exports.NotebookCellKind || (exports.NotebookCellKind = {}));
exports.ErrorOutputMimeType = 'application/vnd.code.notebook.error';
var NotebookCellRunState;
(function (NotebookCellRunState) {
    NotebookCellRunState[NotebookCellRunState["Running"] = 1] = "Running";
    NotebookCellRunState[NotebookCellRunState["Idle"] = 2] = "Idle";
    NotebookCellRunState[NotebookCellRunState["Success"] = 3] = "Success";
    NotebookCellRunState[NotebookCellRunState["Error"] = 4] = "Error";
})(NotebookCellRunState = exports.NotebookCellRunState || (exports.NotebookCellRunState = {}));
var ViewColumn;
(function (ViewColumn) {
    /**
     * A *symbolic* editor column representing the currently active column. This value
     * can be used when opening editors, but the *resolved* {@link TextEditor.viewColumn viewColumn}-value
     * of editors will always be `One`, `Two`, `Three`,... or `undefined` but never `Active`.
     */
    ViewColumn[ViewColumn["Active"] = -1] = "Active";
    /**
     * A *symbolic* editor column representing the column to the side of the active one. This value
     * can be used when opening editors, but the *resolved* {@link TextEditor.viewColumn viewColumn}-value
     * of editors will always be `One`, `Two`, `Three`,... or `undefined` but never `Beside`.
     */
    ViewColumn[ViewColumn["Beside"] = -2] = "Beside";
    /**
     * The first editor column.
     */
    ViewColumn[ViewColumn["One"] = 1] = "One";
    /**
     * The second editor column.
     */
    ViewColumn[ViewColumn["Two"] = 2] = "Two";
    /**
     * The third editor column.
     */
    ViewColumn[ViewColumn["Three"] = 3] = "Three";
    /**
     * The fourth editor column.
     */
    ViewColumn[ViewColumn["Four"] = 4] = "Four";
    /**
     * The fifth editor column.
     */
    ViewColumn[ViewColumn["Five"] = 5] = "Five";
    /**
     * The sixth editor column.
     */
    ViewColumn[ViewColumn["Six"] = 6] = "Six";
    /**
     * The seventh editor column.
     */
    ViewColumn[ViewColumn["Seven"] = 7] = "Seven";
    /**
     * The eighth editor column.
     */
    ViewColumn[ViewColumn["Eight"] = 8] = "Eight";
    /**
     * The ninth editor column.
     */
    ViewColumn[ViewColumn["Nine"] = 9] = "Nine";
})(ViewColumn = exports.ViewColumn || (exports.ViewColumn = {}));
//# sourceMappingURL=vscode-like.js.map