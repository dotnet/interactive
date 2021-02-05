// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getNotebookSpecificLanguage, notebookCellLanguages } from "./interactiveNotebook";

// the shape of this is meant to match the cell metadata from VS Code
interface CellMetadata {
    custom?: { [key: string]: any } | undefined,
}

export interface DotNetCellMetadata {
    language: string | undefined,
}

function isDotNetCellMetadata(arg: any): arg is DotNetCellMetadata {
    return arg
        && typeof arg.language === 'string';
}

export function getDotNetMetadata(metadata: CellMetadata | undefined): DotNetCellMetadata {
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

export function withDotNetMetadata(metadata: { [key: string]: any } | undefined, cellMetadata: DotNetCellMetadata): any {
    let result: { [key: string]: any } = {};
    if (metadata) {
        for (const key in metadata) {
            result[key] = metadata[key];
        }
    }

    result.custom ||= {};
    result.custom.metadata ||= {};
    result.custom.metadata.dotnet_interactive ||= {};
    for (const key in cellMetadata) {
        result.custom.metadata.dotnet_interactive[key] = (<any>cellMetadata)[key];
    }

    return result;
}

// the shape of this is meant to match the document metadata from VS Code
export interface DocumentMetadata {
    custom?: { [key: string]: any } | undefined,
}

export interface LanguageInfoMetadata {
    name: string | undefined,
}

function isLanguageInfoMetadata(arg: any): arg is LanguageInfoMetadata {
    return arg
        && typeof arg.name === 'string';
}

export function getLanguageInfoMetadata(metadata: DocumentMetadata | undefined): LanguageInfoMetadata {
    let languageMetadata: LanguageInfoMetadata = {
        name: undefined,
    };

    if (metadata &&
        metadata.custom &&
        metadata.custom.metadata &&
        metadata.custom.metadata.language_info &&
        isLanguageInfoMetadata(metadata.custom.metadata.language_info)) {
        languageMetadata = metadata.custom.metadata.language_info;
    }

    languageMetadata.name = mapIpynbLanguageName(languageMetadata.name);
    return languageMetadata;
}

function mapIpynbLanguageName(name: string | undefined): string | undefined {
    if (name) {
        // The .NET Interactive Jupyter kernel serializes the language names as "C#", "F#", and "PowerShell"; these
        // need to be normalized to .NET Interactive kernel language names.
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

export function getCellLanguage(cellText: string, cellMetadata: DotNetCellMetadata, documentMetadata: LanguageInfoMetadata, fallbackLanguage: string): string {
    const cellLines = cellText.split('\n').map(line => line.trim());
    let cellLanguageSpecifier: string | undefined = undefined;
    if (cellLines.length > 0 && cellLines[0].startsWith('#!')) {
        const cellLanguage = cellLines[0].substr(2);
        const notebookSpecficLanguage = getNotebookSpecificLanguage(cellLanguage);
        if (notebookCellLanguages.includes(notebookSpecficLanguage)) {
            cellLanguageSpecifier = cellLanguage;
        }
    }

    return getNotebookSpecificLanguage(cellLanguageSpecifier || cellMetadata.language || documentMetadata.name || fallbackLanguage);
}

export function withDotNetKernelMetadata(metadata: { [key: string]: any } | undefined): any | undefined {
    // clone the existing metadata
    let result: { [key: string]: any } = {};
    if (metadata) {
        for (const key in metadata) {
            result[key] = metadata[key];
        }
    }

    result.custom ||= {};
    result.custom.metadata ||= {};
    result.custom.metadata.kernelspec ||= {};

    const requiredKernelspecData: { [key: string]: any } = {
        display_name: '.NET (C#)',
        language: 'C#',
        name: '.net-csharp',
    };

    // always set kernelspec data so that this notebook can be opened in Jupyter Lab
    for (const key in requiredKernelspecData) {
        result.custom.metadata.kernelspec[key] = requiredKernelspecData[key];
    }

    return result;
}
