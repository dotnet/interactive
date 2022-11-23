"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.createDefaultNotebookDocumentMetadata = exports.mergeRawMetadata = exports.mergeNotebookDocumentMetadata = exports.mergeNotebookCellMetadata = exports.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata = exports.getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata = exports.getRawNotebookCellMetadataFromNotebookCellMetadata = exports.getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata = exports.createNewIpynbMetadataWithNotebookDocumentMetadata = exports.getKernelspecMetadataFromNotebookDocumentMetadata = exports.getKernelInfosFromNotebookDocument = exports.getKernelspecMetadataFromIpynbNotebookDocument = exports.getNotebookDocumentMetadataFromCompositeKernel = exports.getNotebookDocumentMetadataFromNotebookDocument = exports.getNotebookDocumentMetadataFromInteractiveDocument = exports.getNotebookCellMetadataFromNotebookCellElement = exports.getNotebookCellMetadataFromInteractiveDocumentElement = exports.isDotNetNotebook = exports.isIpynbNotebook = void 0;
function isIpynbNotebook(notebookDocument) {
    return notebookDocument.uri.fsPath.toLowerCase().endsWith('.ipynb');
}
exports.isIpynbNotebook = isIpynbNotebook;
function isDotNetNotebook(notebook) {
    const notebookUriString = notebook.uri.toString();
    if (notebookUriString.endsWith('.dib') || notebook.uri.fsPath.endsWith('.dib')) {
        return true;
    }
    const kernelspecMetadata = getKernelspecMetadataFromIpynbNotebookDocument(notebook);
    if (kernelspecMetadata.name.startsWith('.net-')) {
        return true;
    }
    // doesn't look like us
    return false;
}
exports.isDotNetNotebook = isDotNetNotebook;
function getNotebookCellMetadataFromInteractiveDocumentElement(interactiveDocumentElement) {
    var _a, _b;
    const cellMetadata = createDefaultNotebookCellMetadata();
    // first try to get the old `dotnet_interactive` value...
    const dotnet_interactive = (_a = interactiveDocumentElement.metadata) === null || _a === void 0 ? void 0 : _a.dotnet_interactive;
    if (typeof dotnet_interactive === 'object') {
        const language = dotnet_interactive.language;
        if (typeof language === 'string') {
            // this is a really unfortunate case where we were storing the kernel name, but calling it the language
            cellMetadata.kernelName = language;
        }
    }
    // ...then try newer `polyglot_notebook` value
    const polyglot_notebook = (_b = interactiveDocumentElement.metadata) === null || _b === void 0 ? void 0 : _b.polyglot_notebook;
    if (typeof polyglot_notebook === 'object') {
        const kernelName = polyglot_notebook.kernelName;
        if (typeof kernelName === 'string') {
            cellMetadata.kernelName = kernelName;
        }
    }
    return cellMetadata;
}
exports.getNotebookCellMetadataFromInteractiveDocumentElement = getNotebookCellMetadataFromInteractiveDocumentElement;
function getNotebookCellMetadataFromNotebookCellElement(notebookCell) {
    var _a, _b;
    const cellMetadata = createDefaultNotebookCellMetadata();
    const metadata = (_b = (_a = notebookCell.metadata) === null || _a === void 0 ? void 0 : _a.custom) === null || _b === void 0 ? void 0 : _b.metadata;
    if (typeof metadata === 'object') {
        // first try to get the old `dotnet_interactive` value...
        const dotnet_interactive = metadata.dotnet_interactive;
        if (typeof dotnet_interactive === 'object') {
            const language = dotnet_interactive.language;
            if (typeof language === 'string') {
                // this is a really unfortunate case where we were storing the kernel name, but calling it the language
                cellMetadata.kernelName = language;
            }
        }
        // ...then try newer `polyglot_notebook` value
        const polyglot_notebook = metadata.polyglot_notebook;
        if (typeof polyglot_notebook === 'object') {
            const kernelName = polyglot_notebook.kernelName;
            if (typeof kernelName === 'string') {
                cellMetadata.kernelName = kernelName;
            }
        }
    }
    return cellMetadata;
}
exports.getNotebookCellMetadataFromNotebookCellElement = getNotebookCellMetadataFromNotebookCellElement;
function getNotebookDocumentMetadataFromInteractiveDocument(interactiveDocument) {
    const notebookMetadata = createDefaultNotebookDocumentMetadata();
    const kernelInfo = interactiveDocument.metadata.kernelInfo;
    if (typeof kernelInfo === 'object') {
        if (typeof kernelInfo.defaultKernelName === 'string') {
            notebookMetadata.kernelInfo.defaultKernelName = kernelInfo.defaultKernelName;
        }
        const items = kernelInfo.items;
        if (Array.isArray(items) && items.every(item => typeof item === 'object')) {
            notebookMetadata.kernelInfo.items = items;
        }
    }
    notebookMetadata.kernelInfo.items = notebookMetadata.kernelInfo.items.map(item => ensureProperShapeForDocumentKernelInfo(item));
    return notebookMetadata;
}
exports.getNotebookDocumentMetadataFromInteractiveDocument = getNotebookDocumentMetadataFromInteractiveDocument;
function getNotebookDocumentMetadataFromNotebookDocument(document) {
    var _a, _b, _c, _d;
    const notebookMetadata = createDefaultNotebookDocumentMetadata();
    // .dib files will have their metadata at the root; .ipynb files will have their metadata a little deeper
    const polyglot_notebook = (_a = document.metadata.polyglot_notebook) !== null && _a !== void 0 ? _a : (_d = (_c = (_b = document.metadata) === null || _b === void 0 ? void 0 : _b.custom) === null || _c === void 0 ? void 0 : _c.metadata) === null || _d === void 0 ? void 0 : _d.polyglot_notebook;
    if (typeof polyglot_notebook === 'object') {
        const kernelInfo = polyglot_notebook.kernelInfo;
        if (typeof kernelInfo === 'object') {
            if (typeof kernelInfo.defaultKernelName === 'string') {
                notebookMetadata.kernelInfo.defaultKernelName = kernelInfo.defaultKernelName;
            }
            const items = kernelInfo.items;
            if (Array.isArray(items) && items.every(item => typeof item === 'object')) {
                notebookMetadata.kernelInfo.items = items;
            }
        }
    }
    else {
        const x = 1;
    }
    notebookMetadata.kernelInfo.items = notebookMetadata.kernelInfo.items.map(item => ensureProperShapeForDocumentKernelInfo(item));
    return notebookMetadata;
}
exports.getNotebookDocumentMetadataFromNotebookDocument = getNotebookDocumentMetadataFromNotebookDocument;
function getNotebookDocumentMetadataFromCompositeKernel(kernel) {
    var _a;
    const notebookMetadata = createDefaultNotebookDocumentMetadata();
    notebookMetadata.kernelInfo.defaultKernelName = (_a = kernel.defaultKernelName) !== null && _a !== void 0 ? _a : notebookMetadata.kernelInfo.defaultKernelName;
    notebookMetadata.kernelInfo.items = kernel.childKernels.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0).map(k => ({ name: k.name, aliases: k.kernelInfo.aliases, languageName: k.kernelInfo.languageName }));
    return notebookMetadata;
}
exports.getNotebookDocumentMetadataFromCompositeKernel = getNotebookDocumentMetadataFromCompositeKernel;
function ensureProperShapeForDocumentKernelInfo(kernelInfo) {
    if (!kernelInfo.aliases) {
        kernelInfo.aliases = [];
    }
    return kernelInfo;
}
function getKernelspecMetadataFromIpynbNotebookDocument(notebook) {
    // defaulting to empty values so we don't mis-represent the document
    const kernelspecMetadata = {
        display_name: '',
        language: '',
        name: ''
    };
    const custom = notebook.metadata.custom;
    if (typeof custom === 'object') {
        const metadata = custom.metadata;
        if (typeof metadata === 'object') {
            const kernelspec = metadata.kernelspec;
            if (typeof kernelspec === 'object') {
                const display_name = kernelspec.display_name;
                if (typeof display_name === 'string') {
                    kernelspecMetadata.display_name = display_name;
                }
                const language = kernelspec.language;
                if (typeof language === 'string') {
                    kernelspecMetadata.language = language;
                }
                const name = kernelspec.name;
                if (typeof name === 'string') {
                    kernelspecMetadata.name = name;
                }
            }
        }
    }
    return kernelspecMetadata;
}
exports.getKernelspecMetadataFromIpynbNotebookDocument = getKernelspecMetadataFromIpynbNotebookDocument;
function getKernelInfosFromNotebookDocument(notebookDocument) {
    const notebookDocumentMetadata = getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
    const kernelInfos = notebookDocumentMetadata.kernelInfo.items.map(item => ({
        // these are the only important ones
        localName: item.name,
        aliases: item.aliases,
        languageName: item.languageName,
        // these are unused
        uri: 'unused',
        displayName: 'unused',
        supportedKernelCommands: [],
        supportedDirectives: []
    }));
    return kernelInfos;
}
exports.getKernelInfosFromNotebookDocument = getKernelInfosFromNotebookDocument;
function getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata) {
    // these options are hard-coded because this is exactly what we put on disk with `dotnet interactive jupyter install`
    switch (notebookDocumentMetadata.kernelInfo.defaultKernelName) {
        case 'fsharp':
            return {
                display_name: '.NET (F#)',
                language: 'F#',
                name: '.net-fsharp'
            };
        case 'pwsh':
            return {
                display_name: '.NET (PowerShell)',
                language: 'PowerShell',
                name: '.net-pwsh'
            };
        case 'csharp':
        default:
            return {
                display_name: '.NET (C#)',
                language: 'C#',
                name: '.net-csharp'
            };
    }
}
exports.getKernelspecMetadataFromNotebookDocumentMetadata = getKernelspecMetadataFromNotebookDocumentMetadata;
function createNewIpynbMetadataWithNotebookDocumentMetadata(existingMetadata, notebookDocumentMetadata) {
    var _a, _b;
    const resultMetadata = Object.assign({}, existingMetadata);
    // kernelspec
    const kernelspec = getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
    resultMetadata.custom = (_a = resultMetadata.custom) !== null && _a !== void 0 ? _a : {};
    resultMetadata.custom.metadata = (_b = resultMetadata.custom.metadata) !== null && _b !== void 0 ? _b : {};
    resultMetadata.custom.metadata.kernelspec = kernelspec;
    resultMetadata.custom.metadata.polyglot_notebook = notebookDocumentMetadata;
    return resultMetadata;
}
exports.createNewIpynbMetadataWithNotebookDocumentMetadata = createNewIpynbMetadataWithNotebookDocumentMetadata;
function getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata(notebookCellMetadata) {
    return notebookCellMetadata;
}
exports.getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata = getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata;
function getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata) {
    return {
        custom: {
            metadata: {
                // this is the canonical metadata
                polyglot_notebook: notebookCellMetadata,
                // this is to maintain backwards compatibility for a while
                dotnet_interactive: {
                    language: notebookCellMetadata.kernelName
                }
            }
        }
    };
}
exports.getRawNotebookCellMetadataFromNotebookCellMetadata = getRawNotebookCellMetadataFromNotebookCellMetadata;
function getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata) {
    return notebookDocumentMetadata;
}
exports.getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata = getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata;
function getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, createForIpynb) {
    const rawMetadata = {};
    if (createForIpynb) {
        const kernelspec = getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        rawMetadata.custom = {
            metadata: {
                kernelspec,
                polyglot_notebook: notebookDocumentMetadata
            },
        };
    }
    else {
        rawMetadata.polyglot_notebook = notebookDocumentMetadata;
    }
    return rawMetadata;
}
exports.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata = getRawNotebookDocumentMetadataFromNotebookDocumentMetadata;
function mergeNotebookCellMetadata(baseMetadata, metadataWithNewValues) {
    const resultMetadata = Object.assign({}, baseMetadata);
    if (metadataWithNewValues.kernelName) {
        resultMetadata.kernelName = metadataWithNewValues.kernelName;
    }
    return resultMetadata;
}
exports.mergeNotebookCellMetadata = mergeNotebookCellMetadata;
function mergeNotebookDocumentMetadata(baseMetadata, metadataWithNewValues) {
    const resultMetadata = Object.assign({}, baseMetadata);
    const kernelInfoItems = new Map();
    for (const item of baseMetadata.kernelInfo.items) {
        kernelInfoItems.set(item.name, item);
    }
    for (const item of metadataWithNewValues.kernelInfo.items) {
        kernelInfoItems.set(item.name, item);
    }
    resultMetadata.kernelInfo.items = [...kernelInfoItems.values()];
    return resultMetadata;
}
exports.mergeNotebookDocumentMetadata = mergeNotebookDocumentMetadata;
function mergeRawMetadata(baseMetadata, metadataWithNewValues) {
    const resultMetadata = Object.assign({}, baseMetadata);
    for (const key in metadataWithNewValues) {
        resultMetadata[key] = metadataWithNewValues[key];
    }
    return resultMetadata;
}
exports.mergeRawMetadata = mergeRawMetadata;
function createDefaultNotebookDocumentMetadata() {
    return {
        kernelInfo: {
            defaultKernelName: 'csharp',
            items: [
                {
                    name: 'csharp',
                    aliases: [],
                }
            ],
        }
    };
    ;
}
exports.createDefaultNotebookDocumentMetadata = createDefaultNotebookDocumentMetadata;
function createDefaultNotebookCellMetadata() {
    return {};
}
//# sourceMappingURL=metadataUtilities.js.map