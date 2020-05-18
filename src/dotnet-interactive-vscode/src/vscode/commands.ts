import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as jp from '../interop/jupyter';
import { acquireDotnetInteractive } from '../acquisition';
import { InstallInteractiveArgs, InteractiveLaunchOptions } from '../interfaces';
import { serializeNotebook } from '../interactiveNotebook';
import { OutputChannelAdapter } from '../OutputChannelAdapter';

export function registerCommands(context: vscode.ExtensionContext, dotnetPath: string) {

    const config = vscode.workspace.getConfiguration('dotnet-interactive');
    const minDotNetInteractiveVersion = config.get<string>('minimumInteractiveToolVersion');
    const interactiveToolSource = config.get<string>('interactiveToolSource');
    const acquireChannel = new OutputChannelAdapter(vscode.window.createOutputChannel('.NET interactive : tool acquisition'));
    acquireChannel.show();

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.convertJupyterNotebook', async (jupyterUri?: vscode.Uri | undefined, notebookUri?: vscode.Uri | undefined) => {
        // ensure we have a jupyter uri
        if (!jupyterUri) {
            const uris = await vscode.window.showOpenDialog({
                filters: {
                    'Jupyter Notebook Files': ['ipynb']
                }
            });

            if (uris && uris.length > 0) {
                jupyterUri = uris[0];
            }

            if (!jupyterUri) {
                // still no appropriate path
                return;
            }
        }

        // ensure we have a notebook uri
        if (!notebookUri) {
            notebookUri = await vscode.window.showSaveDialog({
                filters: {
                    '.NET Interactive Notebook Files': ['dotnet-interactive']
                }
            });

            if (!notebookUri) {
                // still no appropriate path
                return;
            }
        }

        const fileContents = (Buffer.from(await vscode.workspace.fs.readFile(jupyterUri))).toString('utf-8');
        const jupyterJson = JSON.parse(fileContents);
        const notebook = jp.convertFromJupyter(jupyterJson);
        const notebookContents = serializeNotebook(notebook);
        const notebookBuffer = Buffer.from(notebookContents);
        await vscode.workspace.fs.writeFile(notebookUri, notebookBuffer);

        // currently no api to open the just-created notebook, so we'll prompt instead
        await vscode.window.showInformationMessage(`.NET Interactive notebook saved to '${notebookUri.fsPath}'`);
    }));

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
            minDotNetInteractiveVersion!,
            context.globalStoragePath,
            getInteractiveVersion,
            createToolManifest,
            (version: string) => { acquireChannel.append(`Installing .NET Interactive version ${version}...`); },
            installInteractiveTool,
            () => { acquireChannel.append('.NET Interactive installation complete.'); },
            acquireChannel);
        return launchOptions;
    }));
   
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
            interactiveToolSource!,
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
