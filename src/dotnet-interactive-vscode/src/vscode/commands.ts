import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as jp from '../interop/jupyter';
import { acquireDotnetInteractive, interactiveToolSource } from '../acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from '../interfaces';

export function registerCommands(context: vscode.ExtensionContext, dotnetPath: string) {
    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.exportAsJupyterNotebook', async () => {
        if (vscode.notebook.activeNotebookEditor) {
            const uri = await vscode.window.showSaveDialog({
                filters: {
                    'Jupyter Notebook Files': ['ipynb']
                }
            });
            if (!uri) {
                return;
            }

            const { document } = vscode.notebook.activeNotebookEditor;
            const jupyter = jp.convertToJupyter(document);
            await vscode.workspace.fs.writeFile(uri, Buffer.from(JSON.stringify(jupyter, null, 1)));
        }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.acquire', async (args?: InstallInteractiveArgs | undefined): Promise<InteractiveLaunchOptions | undefined> => {
        if (!args) {
            args = {
                dotnetPath: dotnetPath,
                toolVersion: undefined
            };
        }

        const launchOptions = await acquireDotnetInteractive(
            args,
            context.globalStoragePath,
            getInteractiveVersion,
            createToolManifest,
            () => { vscode.window.showInformationMessage('Installing .NET Interactive...'); },
            installInteractiveTool,
            () => { vscode.window.showInformationMessage('.NET Interactive installation complete.'); });
        return launchOptions;
    }));

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.reportInteractiveVersion', async (args?: InstallInteractiveArgs | undefined) => {
        const interactiveVersrion = await getInteractiveVersion(dotnetPath, context.globalStoragePath);
        if (interactiveVersrion) {
            vscode.window.showInformationMessage(`.NET Interactive tool version: ${interactiveVersrion}.`);
        } else {
            vscode.window.showWarningMessage('Unable to determine .NET Interactive tool version.');
        }
    }));
}

// callbacks used to install interactive tool

async function getInteractiveVersion(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
    const result = await execute(dotnetPath, ['tool', 'run', 'dotnet-interactive', '--', '--version'], globalStoragePath);
    if (result.code === 0) {
        return result.output;
    }

    return undefined;
}

async function createToolManifest(dotnetPath: string, globalStoragePath: string): Promise<void> {
    const result = await execute(dotnetPath, ['new', 'tool-manifest'], globalStoragePath);
    if (result.code !== 0) {
        throw new Error('Unable to create local tool manifest.');
    }
}

async function installInteractiveTool(args: InstallInteractiveArgs, globalStoragePath: string): Promise<void> {
    // remove previous tool; swallow errors in case it's not already installed
    let uninstallArgs = [
        'tool',
        'uninstall',
        'Microsoft.dotnet-interactive'
    ];
    await execute(args.dotnetPath, uninstallArgs, globalStoragePath);

    let toolArgs = [
        'tool',
        'install',
        '--add-source',
        interactiveToolSource,
        'Microsoft.dotnet-interactive'
    ];
    if (args.toolVersion) {
        toolArgs.push('--version', args.toolVersion);
    }

    return new Promise(async (resolve, reject) => {
        const result = await execute(args.dotnetPath, toolArgs, globalStoragePath);
        if (result.code === 0) {
            resolve();
        }

        reject();
    });
}

export function execute(command: string, args: Array<string>, workingDirectory?: string | undefined): Promise<{ code: number, output: string, error: string }> {
    return new Promise<{ code: number, output: string, error: string }>((resolve, reject) => {
        let output = '';
        let error = '';

        let childProcess = cp.spawn(command, args, { cwd: workingDirectory });
        childProcess.stdout.on('data', data => output += data);
        childProcess.stderr.on('data', data => error += data);
        childProcess.on('exit', (code: number, _signal: string) => {
            resolve({
                code: code,
                output: output.trim(),
                error: error.trim()
            });
        });
    });
}
