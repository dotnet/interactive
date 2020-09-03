// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { execute, registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './vscode/commands';

import { IDotnetAcquireResult } from './interfaces/dotnet';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from './interfaces';

import compareVersions = require("../node_modules/compare-versions");
import { processArguments } from './utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';

export async function activate(context: vscode.ExtensionContext) {
    // install dotnet or use global
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));
    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion');
    let dotnetPath: string;
    if (await isDotnetUpToDate(minDotNetSdkVersion!)) {
        dotnetPath = 'dotnet';
    } else {
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: minDotNetSdkVersion });
        dotnetPath = commandResult!.dotnetPath;
    }

    registerAcquisitionCommands(context, dotnetPath);

    // install dotnet-interactive
    const installArgs: InstallInteractiveArgs = {
        dotnetPath,
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);

    // register with VS Code
    const clientMapper = new ClientMapper(async (notebookPath) => {
        // prepare kernel transport launch arguments and working directory using a fresh config item so we don't get cached values
        const config = vscode.workspace.getConfiguration('dotnet-interactive');
        const kernelTransportArgs = config.get<Array<string>>('kernelTransportArgs')!;
        const argsTemplate = {
            args: kernelTransportArgs,
            workingDirectory: config.get<string>('kernelTransportWorkingDirectory')!
        };
        const processStart = processArguments(argsTemplate, notebookPath, dotnetPath, launchOptions!.workingDirectory);
        const transport = new StdioKernelTransport(processStart, diagnosticsChannel, vscode.Uri.parse);
        await transport.waitForReady();

        let externalUri = await vscode.env.asExternalUri(vscode.Uri.parse(`http://localhost:${transport.httpPort}`));
        //create tunnel for teh kernel transport
        await transport.setExternalUri(externalUri);
        return transport;
    });

    registerKernelCommands(context, clientMapper);
    registerFileCommands(context, clientMapper);

    const diagnosticDelay = config.get<number>('liveDiagnosticDelay') || 500; // fall back to something reasonable
    const selector = {
        viewType: ['dotnet-interactive','dotnet-interactive-jupyter'],
        filenamePattern: '*.{dib,dotnet-interactive,ipynb}'
    };
    const notebookProvider = new DotNetInteractiveNotebookContentProvider(clientMapper);
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', notebookProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', notebookProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selector, notebookProvider));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper, diagnosticDelay));
}

export function deactivate() {
}

async function isDotnetUpToDate(minVersion: string): Promise<boolean> {
    const result = await execute('dotnet', ['--version']);
    return result.code === 0 && compareVersions.compare(result.output, minVersion, '>=');
}
