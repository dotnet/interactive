// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import { getNotebookSpecificLanguage, isDotnetInteractiveLanguage, notebookCellLanguages } from "./interactiveNotebook";
import { isDotnetKernel } from './utilities';

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

export function getDotNetMetadata(metadata: any): DotNetCellMetadata {
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

export function getLanguageInfoMetadata(metadata: any): LanguageInfoMetadata {
    let languageMetadata: LanguageInfoMetadata = {
        name: undefined,
    };

    if (metadata &&
        metadata.custom &&
        metadata.custom.metadata &&
        metadata.custom.metadata.language_info &&
        isLanguageInfoMetadata(metadata.custom.metadata.language_info)) {
        languageMetadata = { ...metadata.custom.metadata.language_info };
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

export function getCellLanguage(cellText: string, cellMetadata: DotNetCellMetadata, documentMetadata: LanguageInfoMetadata, reportedCellLanguage: string): string {
    const cellLines = cellText.split('\n').map(line => line.trim());
    let cellLanguageSpecifier: string | undefined = undefined;
    if (cellLines.length > 0 && cellLines[0].startsWith('#!')) {
        const cellLanguage = cellLines[0].substr(2);
        const notebookSpecficLanguage = getNotebookSpecificLanguage(cellLanguage);
        if (notebookCellLanguages.includes(notebookSpecficLanguage)) {
            cellLanguageSpecifier = cellLanguage;
        }
    }

    let dotnetDocumentLanguage: string | undefined = undefined;
    if (isDotnetInteractiveLanguage(reportedCellLanguage) || notebookCellLanguages.includes(getNotebookSpecificLanguage(reportedCellLanguage))) {
        // reported language is either something like `dotnet-interactive.csharp` or it's `csharp` that can be turned into a known supported language
        dotnetDocumentLanguage = getNotebookSpecificLanguage(reportedCellLanguage);
    }
    const dotnetCellLanguage = cellLanguageSpecifier || cellMetadata.language || dotnetDocumentLanguage || documentMetadata.name;
    if (dotnetCellLanguage) {
        return getNotebookSpecificLanguage(dotnetCellLanguage);
    }

    return reportedCellLanguage;
}

export interface KernelspecMetadata {
    readonly display_name: string,
    readonly language: string,
    readonly name: string,
}

export const requiredKernelspecData: KernelspecMetadata = {
    display_name: '.NET (C#)',
    language: 'C#',
    name: '.net-csharp',
};

export const requiredLanguageInfoData = {
    file_extension: '.cs',
    mimetype: 'text/x-csharp',
    name: 'C#',
    pygments_lexer: 'csharp',
    version: '9.0',
};

export function withDotNetKernelMetadata(metadata: { [key: string]: any } | undefined): any | undefined {
    // clone the existing metadata
    let result: { [key: string]: any } = {};
    if (metadata) {
        for (const key in metadata) {
            result[key] = metadata[key];
        }
    }

    result.custom = {
        ...result.custom,
        metadata: {
            ...result.custom?.metadata,
            kernelspec: {
                ...result.custom?.metadata?.kernelspec,
                ...requiredKernelspecData,
            },
            language_info: requiredLanguageInfoData,
        },
    };

    return result;
}

export function isIpynbFile(filePath: string): boolean {
    return path.extname(filePath).toLowerCase() === '.ipynb';
}

export function validateNotebookShape(notebookData: any, notificationCallback: (isError: boolean, message: string) => void) {
    const kernelspecName = notebookData?.metadata?.kernelspec?.name;
    if (isDotnetKernel(kernelspecName)) {
        // looks like us, check the cell languages
        let hasLanguages = true;
        for (const cell of notebookData?.cells) {
            const cellLanguage = cell?.metadata?.dotnet_interactive?.language;
            if (cell?.cell_type === 'code' && typeof cellLanguage !== 'string') {
                hasLanguages = false;
                break;
            }
        }

        if (!hasLanguages) {
            notificationCallback(false, `.NET Interactive could not determine the language of the notebook cells.  Please ensure each cell's language is correctly set.`);
        }
    } else {
        // might be something else
        notificationCallback(true, `Unexpected kernelspec name '${kernelspecName}' in notebook.  Right-click the notebook file and select 'Open With...' to select the correct editor.`);
    }
}
