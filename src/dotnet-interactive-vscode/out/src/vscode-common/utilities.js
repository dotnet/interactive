"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.extensionToDocumentType = exports.isVersionSufficient = exports.getVersionNumber = exports.computeToolInstallArguments = exports.stringify = exports.parse = exports.getWorkingDirectoryForNotebook = exports.createUri = exports.debounceAndReject = exports.clearDebounce = exports.debounce = exports.trimTrailingCarriageReturn = exports.splitAndCleanLines = exports.wait = exports.isNotNull = exports.processArguments = exports.getDotNetVersionOrThrow = exports.createOutput = exports.executeSafeAndLog = exports.executeSafe = void 0;
const compareVersions = require("compare-versions");
const cp = require("child_process");
const path = require("path");
const uuid_1 = require("uuid");
const contracts = require("./dotnet-interactive/contracts");
const dotnet_interactive_1 = require("./dotnet-interactive");
function executeSafe(command, args, workingDirectory) {
    return new Promise(resolve => {
        try {
            let output = '';
            let error = '';
            function exitOrClose(code, _signal) {
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
        }
        catch (err) {
            resolve({
                code: -1,
                output: '',
                error: '' + err,
            });
        }
    });
}
exports.executeSafe = executeSafe;
function executeSafeAndLog(outputChannel, operationName, command, args, workingDirectory) {
    return __awaiter(this, void 0, void 0, function* () {
        outputChannel.appendLine(`${operationName}: Executing [${command} ${args.join(' ')}] in '${workingDirectory}'.`);
        const result = yield executeSafe(command, args, workingDirectory);
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
    });
}
exports.executeSafeAndLog = executeSafeAndLog;
function createOutput(outputItems, outputId) {
    if (!outputId) {
        outputId = (0, uuid_1.v4)();
    }
    const output = {
        id: outputId,
        items: outputItems,
    };
    return output;
}
exports.createOutput = createOutput;
function getDotNetVersionOrThrow(dotnetPath, outputChannel) {
    return __awaiter(this, void 0, void 0, function* () {
        const dotnetVersionResult = yield executeSafe(dotnetPath, ['--version']);
        dotnet_interactive_1.Logger.default.info(`Output of "dotnet --version":\n${dotnetVersionResult.output}`);
        if (dotnetVersionResult.code !== 0) {
            const message = `Unable to determine the version of the .NET SDK.\nSTDOUT:\n${dotnetVersionResult.output}\nSTDERR:\n${dotnetVersionResult.error}`;
            outputChannel.appendLine(message);
            throw new Error(message);
        }
        const dotnetVersion = getVersionNumber(dotnetVersionResult.output);
        return dotnetVersion;
    });
}
exports.getDotNetVersionOrThrow = getDotNetVersionOrThrow;
function processArguments(template, workingDirectory, dotnetPath, globalStoragePath, env) {
    let map = {
        'dotnet_path': dotnetPath,
        'global_storage_path': globalStoragePath,
        'working_dir': workingDirectory
    };
    let processed = template.args.map(a => performReplacement(a, map));
    return {
        command: processed[0],
        args: [...processed.slice(1)],
        workingDirectory: performReplacement(template.workingDirectory, map),
        env: env || {},
    };
}
exports.processArguments = processArguments;
function isNotNull(obj) {
    return obj !== undefined;
}
exports.isNotNull = isNotNull;
function wait(milliseconds) {
    return new Promise((resolve => {
        setTimeout(() => {
            resolve();
        }, milliseconds);
    }));
}
exports.wait = wait;
function performReplacement(template, map) {
    let result = template;
    for (let key in map) {
        let fullKey = `{${key}}`;
        result = result.replace(fullKey, map[key]);
    }
    return result;
}
function splitAndCleanLines(source) {
    let lines;
    if (typeof source === 'string') {
        lines = source.split('\n');
    }
    else {
        lines = source;
    }
    return lines.map(ensureNoNewlineTerminators);
}
exports.splitAndCleanLines = splitAndCleanLines;
function ensureNoNewlineTerminators(line) {
    if (line.endsWith('\n')) {
        line = line.substr(0, line.length - 1);
    }
    if (line.endsWith('\r')) {
        line = line.substr(0, line.length - 1);
    }
    return line;
}
function trimTrailingCarriageReturn(value) {
    if (value.endsWith('\r')) {
        return value.substr(0, value.length - 1);
    }
    return value;
}
exports.trimTrailingCarriageReturn = trimTrailingCarriageReturn;
let debounceTimeoutMap = new Map();
function clearDebounceTimeout(key) {
    const timeout = debounceTimeoutMap.get(key);
    if (timeout) {
        clearTimeout(timeout);
        debounceTimeoutMap.delete(key);
    }
}
function debounce(key, timeout, callback) {
    clearDebounceTimeout(key);
    const newTimeout = setTimeout(callback, timeout);
    debounceTimeoutMap.set(key, newTimeout);
}
exports.debounce = debounce;
function clearDebounce(key) {
    rejectPendingPromise(key);
    debounce(key, 0, () => { });
}
exports.clearDebounce = clearDebounce;
function rejectPendingPromise(key) {
    const promiseRejection = lastPromiseRejections.get(key);
    lastPromiseRejections.delete(key);
    if (promiseRejection) {
        promiseRejection();
    }
}
let lastPromiseRejections = new Map();
function debounceAndReject(key, timeout, callback) {
    const newPromise = new Promise((resolve, reject) => {
        rejectPendingPromise(key);
        lastPromiseRejections.set(key, reject);
        debounce(key, timeout, () => __awaiter(this, void 0, void 0, function* () {
            const result = yield callback();
            lastPromiseRejections.delete(key);
            resolve(result);
        }));
    });
    return newPromise;
}
exports.debounceAndReject = debounceAndReject;
function createUri(fsPath, scheme) {
    return {
        fsPath,
        scheme: scheme || 'file',
        toString: () => fsPath
    };
}
exports.createUri = createUri;
function getWorkingDirectoryForNotebook(notebookUri, workspaceFolderUris, fallackWorkingDirectory) {
    var _a;
    switch (notebookUri.scheme) {
        case 'file':
            // local file, use it's own directory
            return path.dirname(notebookUri.fsPath);
        case 'untitled':
            // unsaved notebook, use first local workspace folder
            const firstLocalWorkspaceFolderUri = workspaceFolderUris.find(uri => uri.scheme === 'file');
            return (_a = firstLocalWorkspaceFolderUri === null || firstLocalWorkspaceFolderUri === void 0 ? void 0 : firstLocalWorkspaceFolderUri.fsPath) !== null && _a !== void 0 ? _a : fallackWorkingDirectory;
        default:
            // something else (e.g., remote notebook), use fallback
            return fallackWorkingDirectory;
    }
}
exports.getWorkingDirectoryForNotebook = getWorkingDirectoryForNotebook;
function parse(text) {
    return JSON.parse(text, (key, value) => {
        if (key === 'rawData' && typeof value === 'string') {
            // this looks suspicously like a base64-encoded byte array; special-case this by interpreting this as a base64-encoded string
            const buffer = Buffer.from(value, 'base64');
            return Uint8Array.from(buffer.values());
        }
        return value;
    });
}
exports.parse = parse;
function stringify(value) {
    return JSON.stringify(value, (key, value) => {
        if (key === 'rawData' && (typeof value.length === 'number' || value.type === 'Buffer')) {
            // this looks suspicously like a `Uint8Array` or `Buffer` object; special-case this by returning a base64-encoded string
            const buffer = Buffer.from(value);
            return buffer.toString('base64');
        }
        if (key.indexOf('/') > 0 && Array.isArray(value.data) && value.type === 'Buffer') {
            // this looks like a cell output where `key` is a mime type and `value` is a UTF-8 string
            const buffer = Buffer.from(value);
            return buffer.toString('utf-8');
        }
        return value;
    });
}
exports.stringify = stringify;
function computeToolInstallArguments(args) {
    let installArgs = {
        dotnetPath: 'dotnet',
        toolVersion: undefined,
    };
    if (typeof args === 'string') {
        installArgs.dotnetPath = args;
    }
    else if (typeof args === 'object' && typeof args.dotnetPath === 'string') {
        installArgs = args;
    }
    return installArgs;
}
exports.computeToolInstallArguments = computeToolInstallArguments;
function getVersionNumber(output) {
    const lines = output.trim().split('\n');
    return lines[lines.length - 1];
}
exports.getVersionNumber = getVersionNumber;
function isVersionSufficient(firstVersion, secondVersion) {
    try {
        return compareVersions.compare(firstVersion, secondVersion, '>=');
    }
    catch (_) {
        return false;
    }
}
exports.isVersionSufficient = isVersionSufficient;
function extensionToDocumentType(extension) {
    switch (extension) {
        case '.dib':
        case '.dotnet-interactive':
            return contracts.DocumentSerializationType.Dib;
        case '.ipynb':
            return contracts.DocumentSerializationType.Ipynb;
        default:
            throw new Error(`Unsupported notebook extension '${extension}'`);
    }
}
exports.extensionToDocumentType = extensionToDocumentType;
//# sourceMappingURL=utilities.js.map