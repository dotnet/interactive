"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.isDotNetNotebookMetadata = exports.isIpynbFile = exports.withDotNetKernelMetadata = exports.requiredLanguageInfoData = exports.requiredKernelspecData = exports.getCellLanguage = exports.mapIpynbLanguageName = exports.getLanguageInfoMetadata = exports.withDotNetCellMetadata = exports.getDotNetMetadata = void 0;
const path = require("path");
const interactiveNotebook_1 = require("./interactiveNotebook");
function isDotNetCellMetadata(arg) {
    return arg
        && typeof arg.language === 'string';
}
function getDotNetMetadata(metadata) {
    if (metadata &&
        metadata.custom &&
        metadata.custom.metadata &&
        metadata.custom.metadata.dotnet_interactive &&
        isDotNetCellMetadata(metadata.custom.metadata.dotnet_interactive)) {
        return metadata.custom.metadata.dotnet_interactive;
    }
    return {
        language: undefined,
    };
}
exports.getDotNetMetadata = getDotNetMetadata;
function withDotNetCellMetadata(metadata, cellLanguage) {
    const newMetadata = Object.assign({}, metadata);
    newMetadata.custom = newMetadata.custom || {};
    newMetadata.custom.metadata = newMetadata.custom.metadata || {};
    newMetadata.custom.metadata.dotnet_interactive = newMetadata.custom.metadata.dotnet_interactive || {};
    newMetadata.custom.metadata.dotnet_interactive.language = cellLanguage;
    return newMetadata;
}
exports.withDotNetCellMetadata = withDotNetCellMetadata;
function isLanguageInfoMetadata(arg) {
    return arg
        && typeof arg.name === 'string';
}
function getLanguageInfoMetadata(metadata) {
    let languageMetadata = {
        name: undefined,
    };
    if (metadata &&
        metadata.custom &&
        metadata.custom.metadata &&
        metadata.custom.metadata.language_info &&
        isLanguageInfoMetadata(metadata.custom.metadata.language_info)) {
        languageMetadata = Object.assign({}, metadata.custom.metadata.language_info);
    }
    languageMetadata.name = mapIpynbLanguageName(languageMetadata.name);
    return languageMetadata;
}
exports.getLanguageInfoMetadata = getLanguageInfoMetadata;
function mapIpynbLanguageName(name) {
    if (name) {
        // The .NET Interactive Jupyter kernel serializes the language names as "C#", "F#", and "PowerShell"; these
        // need to be normalized to Polyglot Notebook kernel language names.
        switch (name.toLowerCase()) {
            case 'c#':
                return 'csharp';
            case 'f#':
                return 'fsharp';
            case 'powershell':
                return 'pwsh';
            default:
                return name;
        }
    }
    return undefined;
}
exports.mapIpynbLanguageName = mapIpynbLanguageName;
function getCellLanguage(cellText, cellMetadata, documentMetadata, reportedCellLanguage) {
    const cellLines = cellText.split('\n').map(line => line.trim());
    let cellLanguageSpecifier = undefined;
    if (cellLines.length > 0 && cellLines[0].startsWith('#!')) {
        const cellLanguage = cellLines[0].substr(2);
        const notebookSpecficLanguage = (0, interactiveNotebook_1.getNotebookSpecificLanguage)(cellLanguage);
        if (interactiveNotebook_1.notebookCellLanguages.includes(notebookSpecficLanguage)) {
            cellLanguageSpecifier = cellLanguage;
        }
    }
    let dotnetDocumentLanguage = undefined;
    if ((0, interactiveNotebook_1.isDotnetInteractiveLanguage)(reportedCellLanguage) || interactiveNotebook_1.notebookCellLanguages.includes((0, interactiveNotebook_1.getNotebookSpecificLanguage)(reportedCellLanguage))) {
        // reported language is either something like `dotnet-interactive.csharp` or it's `csharp` that can be turned into a known supported language
        dotnetDocumentLanguage = (0, interactiveNotebook_1.getNotebookSpecificLanguage)(reportedCellLanguage);
    }
    const dotnetCellLanguage = cellLanguageSpecifier || cellMetadata.language || dotnetDocumentLanguage || documentMetadata.name;
    if (dotnetCellLanguage) {
        return (0, interactiveNotebook_1.getNotebookSpecificLanguage)(dotnetCellLanguage);
    }
    return reportedCellLanguage;
}
exports.getCellLanguage = getCellLanguage;
exports.requiredKernelspecData = {
    display_name: '.NET (C#)',
    language: 'C#',
    name: '.net-csharp',
};
exports.requiredLanguageInfoData = {
    file_extension: '.cs',
    mimetype: 'text/x-csharp',
    name: 'C#',
    pygments_lexer: 'csharp',
    version: '9.0',
};
function withDotNetKernelMetadata(metadata) {
    var _a, _b, _c, _d, _e, _f;
    if (isDotnetKernel((_c = (_b = (_a = metadata === null || metadata === void 0 ? void 0 : metadata.custom) === null || _a === void 0 ? void 0 : _a.metadata) === null || _b === void 0 ? void 0 : _b.kernelspec) === null || _c === void 0 ? void 0 : _c.name)) {
        return metadata; // don't change anything
    }
    const result = Object.assign(Object.assign({}, metadata), { custom: Object.assign(Object.assign({}, metadata === null || metadata === void 0 ? void 0 : metadata.custom), { metadata: Object.assign(Object.assign({}, (_d = metadata === null || metadata === void 0 ? void 0 : metadata.custom) === null || _d === void 0 ? void 0 : _d.metadata), { kernelspec: Object.assign(Object.assign({}, (_f = (_e = metadata === null || metadata === void 0 ? void 0 : metadata.custom) === null || _e === void 0 ? void 0 : _e.metadata) === null || _f === void 0 ? void 0 : _f.kernelspec), exports.requiredKernelspecData), language_info: exports.requiredLanguageInfoData }) }) });
    return result;
}
exports.withDotNetKernelMetadata = withDotNetKernelMetadata;
function isIpynbFile(filePath) {
    return path.extname(filePath).toLowerCase() === '.ipynb';
}
exports.isIpynbFile = isIpynbFile;
function isDotnetKernel(kernelspecName) {
    return typeof kernelspecName === 'string' && kernelspecName.toLowerCase().startsWith('.net-');
}
function isDotNetNotebookMetadata(notebookMetadata) {
    var _a, _b, _c, _d, _e, _f;
    const kernelName = (_c = (_b = (_a = notebookMetadata === null || notebookMetadata === void 0 ? void 0 : notebookMetadata.custom) === null || _a === void 0 ? void 0 : _a.metadata) === null || _b === void 0 ? void 0 : _b.kernelspec) === null || _c === void 0 ? void 0 : _c.name;
    const languageInfo = (_f = (_e = (_d = notebookMetadata === null || notebookMetadata === void 0 ? void 0 : notebookMetadata.custom) === null || _d === void 0 ? void 0 : _d.metadata) === null || _e === void 0 ? void 0 : _e.language_info) === null || _f === void 0 ? void 0 : _f.name;
    const isDotnetLanguageInfo = typeof languageInfo === 'string' && (0, interactiveNotebook_1.isDotnetInteractiveLanguage)(languageInfo);
    return isDotnetKernel(kernelName) || isDotnetLanguageInfo;
}
exports.isDotNetNotebookMetadata = isDotNetNotebookMetadata;
//# sourceMappingURL=ipynbUtilities.js.map