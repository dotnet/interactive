// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';

export const DotNetVersion = 'DotNetVersion';

export type HelpPage =
    typeof DotNetVersion;

export class HelpService {
    constructor(private readonly context: vscode.ExtensionContext) {
    }

    async showHelpPageAndThrow(page: HelpPage): Promise<void> {
        const helpPagePath = getHelpPagePath(this.context, page);
        const helpPageUri = vscode.Uri.file(helpPagePath);
        await vscode.commands.executeCommand('markdown.showPreview', helpPageUri);
        throw new Error('Error activating extension, see the displayed help page for more details.');
    }
}

function getHelpPagePath(context: vscode.ExtensionContext, page: HelpPage) {
    const basePath = path.join(context.extensionPath, 'help');
    const helpPagePath = path.join(basePath, `${page}.md`);
    return helpPagePath;
}
