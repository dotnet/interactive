// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './polyglot-notebooks';
import * as contracts from './polyglot-notebooks/contracts';
import * as metadataUtilities from './metadataUtilities';
import * as vscodeLike from './interfaces/vscode-like';

export interface KernelSelectorOption {
    kernelName: string;
    displayValue: string;
    languageName?: string;
}

export function getKernelInfoDisplayValue(kernelInfo: contracts.KernelInfo): string {
    const localName = kernelInfo.localName;
    const displayName = kernelInfo.displayName;
    if (localName === displayName) {
        return localName;
    } else {
        return `${kernelInfo.localName} - ${kernelInfo.displayName}`;
    }
}

export function getKernelSelectorOptions(kernel: CompositeKernel, document: vscodeLike.NotebookDocument, requiredSupportedCommandType: contracts.KernelCommandType): KernelSelectorOption[] {
    const kernelInfos: Map<string, contracts.KernelInfo> = new Map();

    // create and collect all `KernelInfo`s from document metadata...
    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
    for (const item of notebookMetadata.kernelInfo.items) {
        const kernelInfo: contracts.KernelInfo = {
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

    // ...order by kernel name...
    const orderedKernels = [...kernelInfos.values()].sort((a, b) => a.localName.localeCompare(b.localName));

    // ...filter to only kernels that can handle `requiredSupportedCommandType`...
    const filteredKernels = orderedKernels.filter(k => k.supportedKernelCommands.findIndex(kci => kci.name === requiredSupportedCommandType) >= 0);

    // ...and pull out just the information necessary
    const selectorOptions: KernelSelectorOption[] = filteredKernels.map(kernelInfo => {
        const result: KernelSelectorOption = {
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
