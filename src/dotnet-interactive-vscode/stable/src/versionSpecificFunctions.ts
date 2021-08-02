// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as ipynbUtilities from './common/ipynbUtilities';
import * as utilities from './common/utilities';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';
import * as notebookControllers from './notebookControllers';
import * as notebookSerializers from './notebookSerializers';
import { ClientMapper } from './common/clientMapper';
import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';
import { ErrorOutputCreator } from './common/interactiveClient';
import fetch from 'node-fetch';
import * as fs from 'fs';

export function getPreloads(extensionPath: string): vscode.Uri[] {
    const preloads: vscode.Uri[] = [];
    const errors: string[] = [];
    const apiFiles: string[] = [
        //'kernelApiBootstrapper.js',
        'kernelHttpApiBootstrapper.js'];

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
