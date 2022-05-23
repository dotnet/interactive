// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import * as azdata from 'azdata';
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

export async function handleRequestInput(prompt: string, isPassword: boolean, inputName?: string): Promise<string | undefined> {
    let result: string | undefined;
    if (inputName === 'connectionString' && !isPassword) {
        let connection = await azdata.connection.openConnectionDialog();
        if (connection) {
            result = await azdata.connection.getConnectionString(connection.connectionId, true);
        }
    } else {
        result = await vscode.window.showInputBox({ prompt, password: isPassword });
    }
    return result;
}
