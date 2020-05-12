import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { execute, registerCommands } from './vscode/commands';

import { IDotnetAcquireResult } from './interfaces/dotnet';
import * as versions from './minVersions';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from './interfaces';

import compareVersions = require("../node_modules/compare-versions");

export async function activate(context: vscode.ExtensionContext) {
    // install dotnet or use global
    let dotnetPath: string;
    if (await isDotnetUpToDate()) {
        dotnetPath = 'dotnet';
    } else {
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: versions.minimumDotNetSdkVersion });
        dotnetPath = commandResult!.dotnetPath;
    }

    registerCommands(context, dotnetPath);

    // install dotnet-interactive
    const installArgs: InstallInteractiveArgs = {
        dotnetPath,
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);
    let launchArgs = [...launchOptions!.args];
    launchArgs.push('stdio');

    const clientMapper = new ClientMapper(() => new StdioKernelTransport(dotnetPath, launchArgs, launchOptions!.workingDirectory));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', new DotNetInteractiveNotebookContentProvider(clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
}

export function deactivate() {
}

async function isDotnetUpToDate(): Promise<boolean> {
    const result = await execute('dotnet', ['--version']);
    return result.code === 0 && compareVersions.compare(result.output, versions.minimumDotNetSdkVersion, '>=');
}
