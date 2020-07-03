// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import { ProcessStart } from "./interfaces";

export function processArguments(template: { args: Array<string>, workingDirectory: string }, notebookPath: string, dotnetPath: string, globalStoragePath: string): ProcessStart {
    let map: { [key: string]: string } = {
        'dotnet_path': dotnetPath,
        'global_storage_path': globalStoragePath,
        'working_dir': path.dirname(notebookPath)
    };
    let processed = template.args.map(a => performReplacement(a, map));
    return {
        command: processed[0],
        args: [...processed.slice(1)],
        workingDirectory: performReplacement(template.workingDirectory, map)
    };
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
