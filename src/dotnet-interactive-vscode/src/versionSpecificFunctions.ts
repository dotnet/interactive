// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import * as fs from 'fs';

export function getPreloads(extensionPath: string): vscode.Uri[] {
    const preloads: vscode.Uri[] = [];
    const errors: string[] = [];
    const apiFiles: string[] = [
        'kernelApiBootstrapper.js'
    ];

    for (const apiFile of apiFiles) {
        const apiFileUri = vscode.Uri.file(path.join(extensionPath, 'resources', apiFile));
        if (fs.existsSync(apiFileUri.fsPath)) {
            preloads.push(apiFileUri);
        } else {
            errors.push(`Unable to find API file expected at  ${apiFileUri.fsPath}`);
        }
    }

    if (errors.length > 0) {
        const error = errors.join("\n");
        throw new Error(error);
    }

    return preloads;
}

export async function getSqlConnectionString(): Promise<string | undefined> {
    throw new Error('This method is not supported for VS Code.');
}