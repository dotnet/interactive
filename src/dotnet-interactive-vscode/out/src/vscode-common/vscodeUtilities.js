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
exports.vsCodeCellOutputToContractCellOutput = exports.toInteractiveDocumentElement = exports.ensureCellLanguage = exports.ensureCellKernelKind = exports.setCellKernelName = exports.getCellKernelName = exports.toNotebookDocument = exports.getEol = exports.toVsCodeDiagnostic = exports.convertToRange = exports.isStableBuild = exports.isInsidersBuild = void 0;
const os = require("os");
const vscode = require("vscode");
const interfaces_1 = require("./interfaces");
const contracts_1 = require("./dotnet-interactive/contracts");
const metadataUtilities = require("./metadataUtilities");
const vscodeLike = require("./interfaces/vscode-like");
const constants = require("./constants");
const versionSpecificFunctions = require("../versionSpecificFunctions");
function isInsidersBuild() {
    return vscode.version.indexOf('-insider') >= 0;
}
exports.isInsidersBuild = isInsidersBuild;
function isStableBuild() {
    return !isInsidersBuild();
}
exports.isStableBuild = isStableBuild;
function convertToPosition(linePosition) {
    return new vscode.Position(linePosition.line, linePosition.character);
}
function convertToRange(linePositionSpan) {
    if (linePositionSpan === undefined) {
        return undefined;
    }
    return new vscode.Range(convertToPosition(linePositionSpan.start), convertToPosition(linePositionSpan.end));
}
exports.convertToRange = convertToRange;
function toVsCodeDiagnostic(diagnostic) {
    return {
        range: convertToRange(diagnostic.linePositionSpan),
        message: diagnostic.message,
        severity: toDiagnosticSeverity(diagnostic.severity)
    };
}
exports.toVsCodeDiagnostic = toVsCodeDiagnostic;
function toDiagnosticSeverity(severity) {
    switch (severity) {
        case contracts_1.DiagnosticSeverity.Error:
            return vscode.DiagnosticSeverity.Error;
        case contracts_1.DiagnosticSeverity.Info:
            return vscode.DiagnosticSeverity.Information;
        case contracts_1.DiagnosticSeverity.Warning:
            return vscode.DiagnosticSeverity.Warning;
        default:
            return vscode.DiagnosticSeverity.Error;
    }
}
function getEol() {
    const fileConfig = vscode.workspace.getConfiguration('files');
    const eol = fileConfig.get('eol');
    switch (eol) {
        case interfaces_1.NonWindowsEol:
            return interfaces_1.NonWindowsEol;
        case interfaces_1.WindowsEol:
            return interfaces_1.WindowsEol;
        default:
            // could be `undefined` or 'auto'
            if (os.platform() === 'win32') {
                return interfaces_1.WindowsEol;
            }
            else {
                return interfaces_1.NonWindowsEol;
            }
    }
}
exports.getEol = getEol;
function toNotebookDocument(document) {
    return {
        elements: document.getCells().map(toInteractiveDocumentElement),
        metadata: document.metadata
    };
}
exports.toNotebookDocument = toNotebookDocument;
function getCellKernelName(cell) {
    var _a;
    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
    return (_a = cellMetadata.kernelName) !== null && _a !== void 0 ? _a : 'csharp';
}
exports.getCellKernelName = getCellKernelName;
function setCellKernelName(cell, kernelName) {
    return __awaiter(this, void 0, void 0, function* () {
        if (cell.index < 0) {
            const x = cell;
        }
        const cellMetadata = {
            kernelName
        };
        const rawCellMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(cellMetadata);
        yield versionSpecificFunctions.replaceNotebookCellMetadata(cell.notebook.uri, cell.index, rawCellMetadata);
    });
}
exports.setCellKernelName = setCellKernelName;
function ensureCellKernelKind(cell, kind) {
    return __awaiter(this, void 0, void 0, function* () {
        if (cell.kind === kind) {
            return cell;
        }
        const newCellData = {
            kind: kind,
            languageId: kind === vscode.NotebookCellKind.Markup ? 'markdown' : constants.CellLanguageIdentifier,
            value: cell.document.getText(),
            metadata: cell.metadata,
        };
        const cellIndex = cell.index; // this gets reset to -1 when the cell is replaced so we have to capture it here
        yield versionSpecificFunctions.replaceNotebookCells(cell.notebook.uri, new vscode.NotebookRange(cellIndex, cellIndex + 1), [newCellData]);
        const cells = cell.notebook.getCells();
        return cells[cellIndex];
    });
}
exports.ensureCellKernelKind = ensureCellKernelKind;
function ensureCellLanguage(cell) {
    return __awaiter(this, void 0, void 0, function* () {
        if (cell.kind === vscode.NotebookCellKind.Code) {
            if (cell.document.languageId !== constants.CellLanguageIdentifier) {
                const updatedCellData = new vscode.NotebookCellData(vscode.NotebookCellKind.Code, cell.document.getText(), constants.CellLanguageIdentifier);
                updatedCellData.metadata = cell.metadata;
                yield versionSpecificFunctions.replaceNotebookCells(cell.notebook.uri, new vscode.NotebookRange(cell.index, cell.index + 1), [updatedCellData]);
            }
        }
    });
}
exports.ensureCellLanguage = ensureCellLanguage;
function toInteractiveDocumentElement(cell) {
    var _a, _b, _c;
    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
    return {
        executionOrder: (_b = (_a = cell.executionSummary) === null || _a === void 0 ? void 0 : _a.executionOrder) !== null && _b !== void 0 ? _b : 0,
        kernelName: cell.kind === vscode.NotebookCellKind.Code
            ? (_c = cellMetadata.kernelName) !== null && _c !== void 0 ? _c : 'csharp'
            : 'markdown',
        contents: cell.document.getText(),
        outputs: cell.outputs.map(vsCodeCellOutputToContractCellOutput)
    };
}
exports.toInteractiveDocumentElement = toInteractiveDocumentElement;
function vsCodeCellOutputToContractCellOutput(output) {
    const outputItems = output.items;
    const errorOutputItems = outputItems.filter(oi => oi.mime === vscodeLike.ErrorOutputMimeType);
    if (errorOutputItems.length > 0) {
        // any error-like output takes precedence
        const errorOutputItem = errorOutputItems[0];
        const error = {
            errorName: 'Error',
            errorValue: '' + errorOutputItem.data,
            stackTrace: [],
        };
        return error;
    }
    else {
        //otherwise build the mime=>value dictionary
        const data = {};
        for (const outputItem of outputItems) {
            data[outputItem.mime] = outputItem.data;
        }
        const cellOutput = {
            data,
            metadata: {}
        };
        return cellOutput;
    }
}
exports.vsCodeCellOutputToContractCellOutput = vsCodeCellOutputToContractCellOutput;
//# sourceMappingURL=vscodeUtilities.js.map