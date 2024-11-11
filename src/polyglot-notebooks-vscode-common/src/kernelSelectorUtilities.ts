// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './polyglot-notebooks';
import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import * as metadataUtilities from './metadataUtilities';
import * as vscodeLike from './interfaces/vscode-like';

export interface KernelSelectorOption {
    kernelName: string;
    displayValue: string;
    languageName?: string;
    description?: string;
}

export function getKernelInfoDisplayValue(kernelInfo: { localName: string, displayName: string }): string {
    const localName = kernelInfo.localName;
    const displayName = kernelInfo.displayName;
    if (localName === displayName) {
        return localName;
    } else {
        return displayName;
    }
}

function extractInfo(kernelInfo: commandsAndEvents.KernelInfo) {
    return {
        localName: kernelInfo.localName,
        displayName: kernelInfo.displayName,
        description: kernelInfo.description,
        languageName: kernelInfo.languageName,
        supportedKernelCommands: Array.from(kernelInfo.supportedKernelCommands.map(c => c.name))
    };
}

export function getKernelSelectorOptions(kernel: CompositeKernel, document: vscodeLike.NotebookDocument, requiredSupportedCommandType: commandsAndEvents.KernelCommandType): KernelSelectorOption[] {
    const kernelInfos: Map<string, { localName: string, displayName: string, languageName?: string, description?: string, supportedKernelCommands: string[] }> = new Map();

    // create and collect all `KernelInfo`s from document metadata...
    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
    for (const item of notebookMetadata.kernelInfo.items) {
        const kernelInfo = {
            localName: item.name,
            displayName: item.name,
            languageName: item.languageName,
            // a few lines down we filter kernels to only those that support the requisite type, so we artificially add that
            // value here to ensure we show kernels that haven't been re-connected yet
            supportedKernelCommands: [requiredSupportedCommandType],
        };
        kernelInfos.set(item.name, kernelInfo);
    }

    // ...overwrite with any "real" `KernelInfo` that we might actually have...
    kernelInfos.set(kernel.name, extractInfo(kernel.kernelInfo));
    for (const childKernel of kernel.childKernels) {
        kernelInfos.set(childKernel.name, extractInfo(childKernel.kernelInfo));
    }

    // ...order by kernel name...
    const orderedKernels = [...kernelInfos.values()].sort((a, b) => a.localName.localeCompare(b.localName));

    // ...filter to only kernels that can handle `requiredSupportedCommandType`...
    const filteredKernels = orderedKernels.filter(k => k.supportedKernelCommands.findIndex(kci => kci === requiredSupportedCommandType) >= 0);

    // ...and pull out just the information necessary
    const selectorOptions: KernelSelectorOption[] = filteredKernels.map(kernelInfo => {
        const result: KernelSelectorOption = {
            kernelName: kernelInfo.localName,
            displayValue: getKernelInfoDisplayValue(kernelInfo),
            description: kernelInfo.description
        };
        if (kernelInfo.languageName) {
            result.languageName = kernelInfo.languageName;
        }

        return result;
    });

    return selectorOptions;
}
