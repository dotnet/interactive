// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as compareVersions from 'compare-versions';
import * as cp from 'child_process';
import * as path from 'path';
import { v4 as uuid } from 'uuid';
import { InstallInteractiveArgs, ProcessStart } from "./interfaces";
import { ErrorOutputMimeType, NotebookCellOutput, NotebookCellOutputItem, ReportChannel, Uri } from 'dotnet-interactive-vscode-interfaces/out/notebook';

export function executeSafe(command: string, args: Array<string>, workingDirectory?: string | undefined): Promise<{ code: number, output: string, error: string }> {
    return new Promise<{ code: number, output: string, error: string }>(resolve => {
        try {
            let output = '';
            let error = '';

            function exitOrClose(code: number, _signal: string) {
                resolve({
                    code,
                    output: output.trim(),
                    error: error.trim(),
                });
            }

            let childProcess = cp.spawn(command, args, { cwd: workingDirectory });
            childProcess.stdout.on('data', data => output += data);
            childProcess.stderr.on('data', data => error += data);
            childProcess.on('error', err => {
                resolve({
                    code: -1,
                    output: '',
                    error: '' + err,
                });
            });
            childProcess.on('close', exitOrClose);
            childProcess.on('exit', exitOrClose);
        } catch (err) {
            resolve({
                code: -1,
                output: '',
                error: '' + err,
            });
        }
    });
}

export async function executeSafeAndLog(outputChannel: ReportChannel, operationName: string, command: string, args: Array<string>, workingDirectory?: string): Promise<{ code: number, output: string, error: string }> {
    outputChannel.appendLine(`${operationName}: Executing [${command} ${args.join(' ')}] in '${workingDirectory}'.`);
    const result = await executeSafe(command, args, workingDirectory);
    outputChannel.appendLine(`${operationName}: Finished with code ${result.code}.`);
    if (result.output.length > 0) {
        outputChannel.appendLine(`${operationName}: STDOUT:`);
        outputChannel.appendLine(result.output);
    }

    if (result.error.length > 0) {
        outputChannel.appendLine(`${operationName}: STDERR:`);
        outputChannel.appendLine(result.error);
    }

    return result;
}

export function createOutput(outputItems: Array<NotebookCellOutputItem>, outputId?: string): NotebookCellOutput {
    if (!outputId) {
        outputId = uuid();
    }

    const output: NotebookCellOutput = {
        id: outputId,
        outputs: outputItems,
    };
    return output;
}

export function createErrorOutput(message: string, outputId?: string): NotebookCellOutput {
    const outputItem: NotebookCellOutputItem = {
        mime: ErrorOutputMimeType,
        value: message,
    };
    const output = createOutput([outputItem], outputId);
    return output;
}

export function isDotNetUpToDate(minVersion: string, commandResult: { code: number, output: string }): boolean {
    return commandResult.code === 0 && compareVersions.compare(commandResult.output, minVersion, '>=');
}

export function processArguments(template: { args: Array<string>, workingDirectory: string }, notebookPath: string, fallbackWorkingDirectory: string, dotnetPath: string, globalStoragePath: string): ProcessStart {
    let workingDirectory = path.parse(notebookPath).dir;
    if (workingDirectory === '') {
        workingDirectory = fallbackWorkingDirectory;
    }

    let map: { [key: string]: string } = {
        'dotnet_path': dotnetPath,
        'global_storage_path': globalStoragePath,
        'working_dir': workingDirectory
    };

    let processed = template.args.map(a => performReplacement(a, map));
    return {
        command: processed[0],
        args: [...processed.slice(1)],
        workingDirectory: performReplacement(template.workingDirectory, map)
    };
}

export function isNotNull<T>(obj: T | null): obj is T {
    return obj !== undefined;
}

export function wait(milliseconds: number): Promise<void> {
    return new Promise<void>((resolve => {
        setTimeout(() => {
            resolve();
        }, milliseconds);
    }));
}

function performReplacement(template: string, map: { [key: string]: string }): string {
    let result = template;
    for (let key in map) {
        let fullKey = `{${key}}`;
        result = result.replace(fullKey, map[key]);
    }

    return result;
}

export function splitAndCleanLines(source: string | Array<string>): Array<string> {
    let lines: Array<string>;
    if (typeof source === 'string') {
        lines = source.split('\n');
    } else {
        lines = source;
    }

    return lines.map(ensureNoNewlineTerminators);
}

function ensureNoNewlineTerminators(line: string): string {
    if (line.endsWith('\n')) {
        line = line.substr(0, line.length - 1);
    }
    if (line.endsWith('\r')) {
        line = line.substr(0, line.length - 1);
    }

    return line;
}

export function trimTrailingCarriageReturn(value: string): string {
    if (value.endsWith('\r')) {
        return value.substr(0, value.length - 1);
    }

    return value;
}

let debounceTimeoutMap: Map<string, NodeJS.Timeout> = new Map();

function clearDebounceTimeout(key: string) {
    const timeout = debounceTimeoutMap.get(key);
    if (timeout) {
        clearTimeout(timeout);
        debounceTimeoutMap.delete(key);
    }
}

export function debounce(key: string, timeout: number, callback: () => void) {
    clearDebounceTimeout(key);
    const newTimeout = setTimeout(callback, timeout);
    debounceTimeoutMap.set(key, newTimeout);
}

export function createUri(fsPath: string): Uri {
    return {
        fsPath,
        toString: () => fsPath
    };
}

export function parse(text: string): any {
    return JSON.parse(text, (key, value) => {
        if (key === 'rawData' && typeof value === 'string') {
            // this looks suspicously like a base64-encoded byte array; special-case this by interpreting this as a base64-encoded string
            const buffer = Buffer.from(value, 'base64');
            return Uint8Array.from(buffer.values());
        }

        return value;
    });
}

export function stringify(value: any): string {
    return JSON.stringify(value, (key, value) => {
        if (key === 'rawData' && (typeof value.length === 'number' || value.type === 'Buffer')) {
            // this looks suspicously like a `Uint8Array` or `Buffer` object; special-case this by returning a base64-encoded string
            const buffer = Buffer.from(value);
            return buffer.toString('base64');
        }

        return value;
    });
}

export function isDotNetKernelPreferred(filename: string, fileMetadata: any): boolean {
    const extension = path.extname(filename);
    switch (extension) {
        // always preferred for our own extension
        case '.dib':
        case '.dotnet-interactive':
            return true;
        // maybe preferred if the kernelspec data matches
        case '.ipynb':
            const kernelName = fileMetadata?.custom?.metadata?.kernelspec?.name;
            return typeof kernelName === 'string'
                && kernelName.toLowerCase().startsWith('.net-');
        // never preferred if it's an unknown extension
        default:
            return false;
    }
}

export function computeToolInstallArguments(args: InstallInteractiveArgs | string | undefined): InstallInteractiveArgs {
    let installArgs: InstallInteractiveArgs = {
        dotnetPath: 'dotnet', // if nothing is specified, we have to fall back to _something_
        toolVersion: undefined,
    };

    if (typeof args === 'string') {
        installArgs.dotnetPath = args;
    } else if (typeof args === 'object' && typeof args.dotnetPath === 'string') {
        installArgs = args;
    }

    return installArgs;
}
