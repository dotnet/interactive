// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from './polyglot-notebooks';
import * as contracts from './polyglot-notebooks/contracts';
import * as vscodeLike from './interfaces/vscode-like';

export interface NotebookDocumentMetadata {
    kernelInfo: contracts.DocumentKernelInfoCollection;
}

export interface NotebookCellMetadata {
    kernelName?: string;
}

export interface KernelspecMetadata {
    display_name: string;
    language: string;
    name: string;
}

export function isIpynbNotebook(notebookDocument: vscodeLike.NotebookDocument) {
    return notebookDocument.uri.fsPath.toLowerCase().endsWith('.ipynb');
}

export function isDotNetNotebook(notebook: vscodeLike.NotebookDocument): boolean {
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

export function getNotebookCellMetadataFromInteractiveDocumentElement(interactiveDocumentElement: contracts.InteractiveDocumentElement): NotebookCellMetadata {
    const cellMetadata = createDefaultNotebookCellMetadata();

    // first try to get the old `dotnet_interactive` value...
    const dotnet_interactive = interactiveDocumentElement.metadata?.dotnet_interactive;
    if (typeof dotnet_interactive === 'object') {
        const language = dotnet_interactive.language;
        if (typeof language === 'string') {
            // this is a really unfortunate case where we were storing the kernel name, but calling it the language
            cellMetadata.kernelName = language;
        }
    }

    // ...then try newer `polyglot_notebook` value
    const polyglot_notebook = interactiveDocumentElement.metadata?.polyglot_notebook;
    if (typeof polyglot_notebook === 'object') {
        const kernelName = polyglot_notebook.kernelName;
        if (typeof kernelName === 'string') {
            cellMetadata.kernelName = kernelName;
        }
    }

    return cellMetadata;
}

export function getNotebookCellMetadataFromNotebookCellElement(notebookCell: vscodeLike.NotebookCell): NotebookCellMetadata {
    const cellMetadata = createDefaultNotebookCellMetadata();

    const metadata = notebookCell.metadata?.custom?.metadata;

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

export function getNotebookDocumentMetadataFromInteractiveDocument(interactiveDocument: contracts.InteractiveDocument): NotebookDocumentMetadata {
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

export function getNotebookDocumentMetadataFromNotebookDocument(document: vscodeLike.NotebookDocument): NotebookDocumentMetadata {
    const notebookMetadata = createDefaultNotebookDocumentMetadata();
    let setDefaultKernel = false;
    let setItems = false;

    // .dib files will have their metadata at the root; .ipynb files will have their metadata a little deeper
    const polyglot_notebook = document.metadata.polyglot_notebook ?? document.metadata?.custom?.metadata?.polyglot_notebook;
    if (typeof polyglot_notebook === 'object') {
        const kernelInfo = polyglot_notebook.kernelInfo;
        if (typeof kernelInfo === 'object') {
            if (typeof kernelInfo.defaultKernelName === 'string') {
                notebookMetadata.kernelInfo.defaultKernelName = kernelInfo.defaultKernelName;
                setDefaultKernel = true;
            }

            const items = kernelInfo.items;
            if (Array.isArray(items) && items.every(item => typeof item === 'object')) {
                notebookMetadata.kernelInfo.items = items;
                setItems = true;
            }
        }
    }

    // if nothing was found, populate it from the kernelspec metadata
    if (isIpynbNotebook(document)) {
        if (!setDefaultKernel) {
            const kernelSpecMetadata = getKernelspecMetadataFromIpynbNotebookDocument(document);
            if (kernelSpecMetadata.name.startsWith('.net-')) {
                // the command `dotnet interactive jupyter install` lays down 3 well-known kernelspecs, all with the name `.net-<kernelName>`
                notebookMetadata.kernelInfo.defaultKernelName = kernelSpecMetadata.name.substring('.net-'.length);
            }
        }

        if (!setItems) {
            notebookMetadata.kernelInfo.items = [
                {
                    name: notebookMetadata.kernelInfo.defaultKernelName,
                    aliases: [],
                }
            ];
        }
    }

    notebookMetadata.kernelInfo.items = notebookMetadata.kernelInfo.items.map(item => ensureProperShapeForDocumentKernelInfo(item));
    return notebookMetadata;
}

export function getNotebookDocumentMetadataFromCompositeKernel(kernel: CompositeKernel): NotebookDocumentMetadata {
    const notebookMetadata = createDefaultNotebookDocumentMetadata();
    notebookMetadata.kernelInfo.defaultKernelName = kernel.defaultKernelName ?? notebookMetadata.kernelInfo.defaultKernelName;
    notebookMetadata.kernelInfo.items = kernel.childKernels.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0).map(k => ({ name: k.name, aliases: k.kernelInfo.aliases, languageName: k.kernelInfo.languageName }));

    return notebookMetadata;
}

function ensureProperShapeForDocumentKernelInfo(kernelInfo: contracts.DocumentKernelInfo) {
    if (!kernelInfo.aliases) {
        kernelInfo.aliases = [];
    }

    return kernelInfo;
}

export function getKernelspecMetadataFromIpynbNotebookDocument(notebook: vscodeLike.NotebookDocument): KernelspecMetadata {
    // defaulting to empty values so we don't mis-represent the document
    const kernelspecMetadata: KernelspecMetadata = {
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

export function getKernelInfosFromNotebookDocument(notebookDocument: vscodeLike.NotebookDocument): contracts.KernelInfo[] {
    const notebookDocumentMetadata = getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
    const kernelInfos: contracts.KernelInfo[] = notebookDocumentMetadata.kernelInfo.items.map(item => ({
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

export function getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata: NotebookDocumentMetadata): KernelspecMetadata {
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

export function createNewIpynbMetadataWithNotebookDocumentMetadata(existingMetadata: { [key: string]: any }, notebookDocumentMetadata: NotebookDocumentMetadata): { [key: string]: any } {
    const resultMetadata: { [key: string]: any } = { ...existingMetadata };

    // kernelspec
    const kernelspec = getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
    resultMetadata.custom = resultMetadata.custom ?? {};
    resultMetadata.custom.metadata = resultMetadata.custom.metadata ?? {};
    resultMetadata.custom.metadata.kernelspec = kernelspec;
    resultMetadata.custom.metadata.polyglot_notebook = notebookDocumentMetadata;
    return resultMetadata;
}

export function getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata(notebookCellMetadata: NotebookCellMetadata): { [key: string]: any } {
    return notebookCellMetadata;
}

export function getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata: NotebookCellMetadata): { [key: string]: any } {
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

export function getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata: NotebookDocumentMetadata): { [key: string]: any } {
    return notebookDocumentMetadata;
}

export function getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata: NotebookDocumentMetadata, createForIpynb: boolean): { [key: string]: any } {
    const rawMetadata: { [key: string]: any } = {};

    if (createForIpynb) {
        const kernelspec = getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        rawMetadata.custom = {
            metadata: {
                kernelspec,
                polyglot_notebook: notebookDocumentMetadata
            },
        };
    } else {
        rawMetadata.polyglot_notebook = notebookDocumentMetadata;
    }

    return rawMetadata;
}

export function mergeNotebookCellMetadata(baseMetadata: NotebookCellMetadata, metadataWithNewValues: NotebookCellMetadata): NotebookCellMetadata {
    const resultMetadata = { ...baseMetadata };
    if (metadataWithNewValues.kernelName) {
        resultMetadata.kernelName = metadataWithNewValues.kernelName;
    }

    return resultMetadata;
}

export function mergeNotebookDocumentMetadata(baseMetadata: NotebookDocumentMetadata, metadataWithNewValues: NotebookDocumentMetadata): NotebookDocumentMetadata {
    const resultMetadata = { ...baseMetadata };
    const kernelInfoItems: Map<string, contracts.DocumentKernelInfo> = new Map();
    for (const item of baseMetadata.kernelInfo.items) {
        kernelInfoItems.set(item.name, item);
    }
    for (const item of metadataWithNewValues.kernelInfo.items) {
        kernelInfoItems.set(item.name, item);
    }

    resultMetadata.kernelInfo.items = [...kernelInfoItems.values()];
    return resultMetadata;
}

export function mergeRawMetadata(baseMetadata: { [key: string]: any }, metadataWithNewValues: { [key: string]: any }): { [key: string]: any } {
    const resultMetadata = { ...baseMetadata };
    for (const key in metadataWithNewValues) {
        resultMetadata[key] = metadataWithNewValues[key];
    }

    return resultMetadata;
}

export function createDefaultNotebookDocumentMetadata(): NotebookDocumentMetadata {
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
    };;
}

function createDefaultNotebookCellMetadata(): NotebookCellMetadata {
    return {};
}