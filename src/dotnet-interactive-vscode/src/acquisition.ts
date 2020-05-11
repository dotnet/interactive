import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { InteractiveLaunchOptions } from './interfaces';
import { IDotnetAcquireResult } from './interfaces/dotnet';
import * as versions from './minVersions';

import compareVersions = require("../node_modules/compare-versions");

export async function getInteractiveLaunchOptions(globalStoragePath: string): Promise<{ dotnetPath: string, launchOptions: InteractiveLaunchOptions }> {
    const dotnetPath = await getDotnetPath();
    const launchOptions = await getArgsToLaunchInteractive(dotnetPath, globalStoragePath);
    return {
        dotnetPath,
        launchOptions
    };
}

async function getDotnetPath(): Promise<string> {
    // ensure valid `dotnet`
    const { code, output } = await processExitCodeAndOutput('dotnet', ['--version']);
    if (code === 0 && compareVersions.compare(output, versions.minimumDotNetSdkVersion, '>=')) {
        // global `dotnet` is present and good
        return 'dotnet';
    }

    // need to acquire one
    vscode.window.showInformationMessage(`Acquiring .NET SDK version ${versions.minimumDotNetSdkVersion}...`); // don't block, just show and go
    const commandResult = await vscode.commands.executeCommand<IDotnetAcquireResult>('dotnet.acquire', { version: versions.minimumDotNetSdkVersion });
    if (commandResult) {
        return commandResult.dotnetPath;
    }

    throw new Error(`Unable to find or acquire .NET SDK >= ${versions.minimumDotNetSdkVersion}`);
}

async function getArgsToLaunchInteractive(dotnetPath: string, globalStoragePath: string): Promise<InteractiveLaunchOptions> {
    // ensure valid `dotnet-interactive`

    // the possible arguments to invoke `interactive` are:
    //   interactive
    //   tool run dotnet-interactive --
    // and both need to be tried
    let localToolArguments = [
        'tool',
        'run',
        'dotnet-interactive',
        '--'
    ];
    let launchOptionSets = [
        ['interactive'], // when launched as a global tool; working directory is irrelevant
        localToolArguments // when launched as a local tool; working directory is possibly relevant
    ];
    let workingDirectory: string | undefined = undefined;
    if (fs.existsSync(globalStoragePath)) {
        // can't launch a process with a non-existent working directory
        workingDirectory = globalStoragePath;
    }
    for (let launchOptions of launchOptionSets) {
        let versionArguments = [...launchOptions];
        versionArguments.push('--version');
        let result = await processExitCodeAndOutput(dotnetPath, versionArguments, workingDirectory);
        if (result.code === 0) {
            // the interactive command was found, but maybe not the right version
            const validDotnetInteractiveDevVersion: RegExp = /^.*-dev$/; // e.g., 1.0.0-dev
            if (validDotnetInteractiveDevVersion.test(result.output)) {
                // locally built versions are always assumed to be valid
                return {
                    args: launchOptions,
                    workingDirectory
                };
            }

            // otherwise we have to check the version
            if (compareVersions.compare(result.output, versions.minimumDotNetInteractiveVersion, '>=')) {
                return {
                    args: launchOptions,
                    workingDirectory
                };
            }
        }
    }

    // if we got here there wasn't an appropriate version installed, so we'll install a local tool
    // at this point the global storage path _must_ exist
    // vs code doesn't guarantee that it exists, but does guarantee that we can create it
    await vscode.workspace.fs.createDirectory(vscode.Uri.file(globalStoragePath));

    // first ensure a tool manifest file exists
    let localToolManifestFile = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
    if (!fs.existsSync(localToolManifestFile)) {
        const { code } = await processExitCodeAndOutput(dotnetPath, ['new', 'tool-manifest', '--output', globalStoragePath]);
        if (code !== 0) {
            throw new Error('Unable to create local tool manifest file.');
        }
    }

    // then acquire the tool and add it to the manifest
    vscode.window.showInformationMessage('Acquiring latest `dotnet-interactive` tool.');
    let toolSource = 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json';
    const result = await processExitCodeAndOutput(dotnetPath, ['tool', 'install', '--add-source', toolSource, 'Microsoft.dotnet-interactive', '--tool-manifest', localToolManifestFile]);
    if (result.code !== 0) {
        throw new Error('Unable to install local tool.');
    }

    // at this point the interactive tool is invoked as a local tool
    return {
        args: localToolArguments,
        workingDirectory: globalStoragePath
    };
}

function processExitCodeAndOutput(command: string, args: Array<string>, workingDirectory?: string | undefined): Promise<{ code: number, output: string, error: string }> {
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
