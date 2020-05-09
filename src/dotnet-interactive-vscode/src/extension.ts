import * as vscode from 'vscode';
import * as cp from 'child_process';
import { ClientMapper } from './clientMapper';
import { DotNetInteractiveNotebookContentProvider } from './vscode/notebookProvider';
import { StdioKernelTransport } from './stdioKernelTransport';
import { registerLanguageProviders } from './vscode/languageProvider';
import { registerCommands } from './vscode/commands';
import { IDotnetAcquireResult } from './interfaces/dotnet';

import compareVersions = require("../node_modules/compare-versions");

export async function activate(context: vscode.ExtensionContext) {
    const dotnet = await getDotnetPath();
    await ensureDotnetInteractive(dotnet);
    const clientMapper = new ClientMapper(() => new StdioKernelTransport(dotnet));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', new DotNetInteractiveNotebookContentProvider(clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => clientMapper.closeClient(notebookDocument.uri)));
    context.subscriptions.push(registerLanguageProviders(clientMapper));
    registerCommands(context);
}

export function deactivate() {
}

async function getDotnetPath(): Promise<string> {
    // ensure valid `dotnet`
    const validDotnetVersion: RegExp = /^(3|5)\./; // 3.x and 5.x
    const { code, output } = await processExitCodeAndOutput('dotnet', ['--version']);
    let dotnetPath: string;
    if (code === 0 && validDotnetVersion.test(output)) {
        // global `dotnet` is present and good
        dotnetPath = 'dotnet';
    } else {
        // need to acquire one
        const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: '3.1' });
        if (commandResult) {
            dotnetPath = commandResult.dotnetPath;
        } else {
            throw new Error('Unable to find or acquire appropriate version of the `dotnet` CLI.');
        }
    }

    return dotnetPath;
}

async function ensureDotnetInteractive(dotnetPath: string): Promise<void> {
    // ensure valid `dotnet-interactive`
    const minRequiredVersion = '1.0.125402';
    const validDotnetInteractiveDevVersion: RegExp = /^.*-dev$/; // e.g., 1.0.0-dev.  Locally produced versions are always assumed to be good.
    const { code, output } = await processExitCodeAndOutput(dotnetPath, ['interactive', '--version']);
    if (code === 0) {
        if (validDotnetInteractiveDevVersion.test(output)) {
            // locally built versions are always assumed to be valid
            return;
        }

        // otherwise we have to check the version
        if (compareVersions.compare(output, minRequiredVersion, '<')) {
            // TODO: acquire it for them
            await vscode.window.showErrorMessage(`Unsupported \`dotnet-interactive\` version, '${output}'.  Minimum required is '${minRequiredVersion}'.`);
            throw new Error('aborting');
        }

        // good to go
    }
}

function processExitCodeAndOutput(command: string, args?: Array<string> | undefined): Promise<{ code: number, output: string }> {
    return new Promise<{ code: number, output: string }>((resolve, reject) => {
        let output = '';
        let childProcess = cp.spawn(command, args || []);
        childProcess.stdout.on('data', data => output += data);
        childProcess.on('exit', (code: number, _signal: string) => {
            resolve({
                code: code,
                output: output.trim()
            });
        });
    });
}
