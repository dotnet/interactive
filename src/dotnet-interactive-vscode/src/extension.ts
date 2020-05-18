import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';

import { DotNetInteractiveNotebookContentProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { execute, registerCommands } from './vscode/commands';

import { IDotnetAcquireResult } from './interfaces/dotnet';
import { InteractiveLaunchOptions, InstallInteractiveArgs } from './interfaces';

import compareVersions = require("../node_modules/compare-versions");
import { processArguments } from './utilities';
import { OutputChannelAdapter } from './OutputChannelAdapter';

export async function activate(context: vscode.ExtensionContext) {
    // install dotnet or use global
    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const dotnetInteractiveChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET interactive'));
    dotnetInteractiveChannel.show();

    const diagnosticsChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET interactive : diagnostics'));
    diagnosticsChannel.show();

    const minDotNetSdkVersion = config.get<string>('minimumDotNetSdkVersion');
    let dotnetPath: string;
    if (await isDotnetUpToDate(minDotNetSdkVersion!)) {
        dotnetPath = 'dotnet';
    } else {
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: minDotNetSdkVersion });
        dotnetPath = commandResult!.dotnetPath;
    }

    registerCommands(context, dotnetPath);

    // install dotnet-interactive
    const installArgs: InstallInteractiveArgs = {
        dotnetPath,
    };
    const launchOptions = await vscode.commands.executeCommand<InteractiveLaunchOptions>('dotnet-interactive.acquire', installArgs);

    // prepare kernel transport launch arguments and working directory
    let kernelTransportArgs = config.get<Array<string>>('kernelTransportArgs')!;
    let argsTemplate = {
        args: kernelTransportArgs,
        workingDirectory: config.get<string>('kernelTransportWorkingDirectory')!
    };
    let processStart = processArguments(argsTemplate, dotnetPath, launchOptions!.workingDirectory);

    // register with VS Code
    const clientMapper = new ClientMapper(() => new StdioKernelTransport(processStart, diagnosticsChannel));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', new DotNetInteractiveNotebookContentProvider(clientMapper, dotnetInteractiveChannel)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
}

export function deactivate() {
}

async function isDotnetUpToDate(minVersion: string): Promise<boolean> {
    const result = await execute('dotnet', ['--version']);
    return result.code === 0 && compareVersions.compare(result.output, minVersion, '>=');
}
