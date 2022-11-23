"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.getKernelSelectorOptions = exports.getKernelInfoDisplayValue = void 0;
const metadataUtilities = require("./metadataUtilities");
function getKernelInfoDisplayValue(kernelInfo) {
    return `${kernelInfo.localName} - ${kernelInfo.displayName}`;
}
exports.getKernelInfoDisplayValue = getKernelInfoDisplayValue;
function getKernelSelectorOptions(kernel, document, requiredSupportedCommandType) {
    const kernelInfos = new Map();
    // create and collect all `KernelInfo`s from document metadata...
    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
    for (const item of notebookMetadata.kernelInfo.items) {
        const kernelInfo = {
            localName: item.name,
            aliases: item.aliases,
            languageName: item.languageName,
            displayName: item.name,
            uri: 'unused',
            // a few lines down we filter kernels to only those that support the requisite type, so we artificially add that
            // value here to ensure we show kernels that haven't been re-connected yet
            supportedKernelCommands: [{ name: requiredSupportedCommandType }],
            supportedDirectives: []
        };
        kernelInfos.set(item.name, kernelInfo);
    }
    // ...overwrite with any "real" `KernelInfo` that we might actually have...
    for (const childKernel of kernel.childKernels) {
        kernelInfos.set(childKernel.name, childKernel.kernelInfo);
    }
    // ...filter to only kernels that can handle `requiredSupportedCommandType`...
    const filteredKernels = [...kernelInfos.values()].filter(k => k.supportedKernelCommands.findIndex(kci => kci.name === requiredSupportedCommandType) >= 0);
    // ...and pull out just the information necessary
    const selectorOptions = filteredKernels.map(kernelInfo => {
        const result = {
            kernelName: kernelInfo.localName,
            displayValue: getKernelInfoDisplayValue(kernelInfo)
        };
        if (kernelInfo.languageName) {
            result.languageName = kernelInfo.languageName;
        }
        return result;
    });
    return selectorOptions;
}
exports.getKernelSelectorOptions = getKernelSelectorOptions;
//# sourceMappingURL=kernelSelectorUtilities.js.map