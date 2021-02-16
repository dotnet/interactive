// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import { ProcessStart } from "./interfaces";
import { Uri } from 'vscode-interfaces/out/notebook';

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
