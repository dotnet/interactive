// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { ClientMapper } from '../clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './notebookContentProvider';
import { StdioKernelTransport } from '../stdioKernelTransport';
import { registerLanguageProviders } from './languageProvider';
import { execute, registerAcquisitionCommands, registerKernelCommands, registerFileCommands } from './commands';

import { IDotnetAcquireResult } from '../interfaces/dotnet';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from '../interfaces';

import compareVersions = require("compare-versions");
import { processArguments } from '../utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';
import { DotNetInteractiveNotebookKernel } from './notebookKernel';
import { DotNetInteractiveNotebookKernelProvider } from './notebookKernelProvider';

export async function activate(context: vscode.ExtensionContext) {
    // install dotnet or use global
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET Interactive : diagnostics'));
    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion');
    let dotnetPath: string;
    if (await isDotnetUpToDate(minDotNetSdkVersion!)) {
        dotnetPath = 'dotnet';
    } else {
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: minDotNetSdkVersion, requestingExtensionId: 'ms-dotnettools.dotnet-interactive-vscode' });
        dotnetPath = commandResult!.dotnetPath;
    }

    registerAcquisitionCommands(context, dotnetPath);

    // install dotnet-interactive
    const installArgs: InstallInteractiveArgs = {
        dotnetPath,
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);
    const apiBootstrapperUri = vscode.Uri.file(path.join(context.extensionPath, 'resources', 'kernelHttpApiBootstrapper.js'));
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
        let notification = {
            displayError: async (message: string) => { await vscode.window.showErrorMessage(message, { modal: false }); },
            displayInfo: async (message: string) => { await vscode.window.showInformationMessage(message, { modal: false }); },
        };
        const transport = new StdioKernelTransport(processStart, diagnosticsChannel, vscode.Uri.parse, notification);
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
        viewType: ['dotnet-interactive', 'dotnet-interactive-jupyter'],
        filenamePattern: '*.{dib,dotnet-interactive,ipynb}'
    };
    const notebookContentProvider = new DotNetInteractiveNotebookContentProvider(clientMapper);
    const notebookKernel = new DotNetInteractiveNotebookKernel(clientMapper, apiBootstrapperUri);
    const notebookKernelProvider = new DotNetInteractiveNotebookKernelProvider(notebookKernel);
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', notebookContentProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', notebookContentProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selector, notebookKernelProvider));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper, diagnosticDelay));
}

export function deactivate() {
}

async function isDotnetUpToDate(minVersion: string): Promise<boolean> {
    const result = await execute('dotnet', ['--version']);
    return result.code === 0 && compareVersions.compare(result.output, minVersion, '>=');
}
