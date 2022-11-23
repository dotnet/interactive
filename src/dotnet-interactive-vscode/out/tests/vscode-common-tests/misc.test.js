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
const chai_1 = require("chai");
const utilities_1 = require("../../src/vscode-common/interfaces/utilities");
const utilities_2 = require("../../src/vscode-common/utilities");
const utilities_3 = require("./utilities");
const vscodeLike = require("../../src/vscode-common/interfaces/vscode-like");
describe('Miscellaneous tests', () => {
    it(`verify command and argument replacement is as expected`, () => {
        let template = {
            args: [
                '{dotnet_path}',
                'tool',
                'run',
                'dotnet-interactive',
                '--',
                'stdio',
                '--working-dir',
                '{working_dir}'
            ],
            workingDirectory: '{global_storage_path}'
        };
        let actual = (0, utilities_2.processArguments)(template, 'replacement-working-dir', 'replacement-dotnet-path', 'replacement-global-storage-path');
        (0, chai_1.expect)(actual).to.deep.equal({
            command: 'replacement-dotnet-path',
            args: [
                'tool',
                'run',
                'dotnet-interactive',
                '--',
                'stdio',
                '--working-dir',
                'replacement-working-dir'
            ],
            workingDirectory: 'replacement-global-storage-path',
            env: {}
        });
    });
    it('notebook working directory comes from notebook uri if local', () => {
        const notebookUri = (0, utilities_2.createUri)('path/to/notebook.dib');
        const workspaceFolderUris = [
            (0, utilities_2.createUri)('not/used/1'),
            (0, utilities_2.createUri)('not/used/2'),
        ];
        const workingDir = (0, utilities_2.getWorkingDirectoryForNotebook)(notebookUri, workspaceFolderUris, 'fallback-not-used');
        (0, chai_1.expect)(workingDir).to.equal('path/to');
    });
    it('notebook working directory comes from local workspace if notebook is untitled', () => {
        const notebookUri = (0, utilities_2.createUri)('path/to/notebook.dib', 'untitled');
        const workspaceFolderUris = [
            (0, utilities_2.createUri)('not/used', 'remote'),
            (0, utilities_2.createUri)('this/is/local/and/used'),
        ];
        const workingDir = (0, utilities_2.getWorkingDirectoryForNotebook)(notebookUri, workspaceFolderUris, 'fallback-not-used');
        (0, chai_1.expect)(workingDir).to.equal('this/is/local/and/used');
    });
    it('notebook working directory comes from fallback if notebook is remote', () => {
        const notebookUri = (0, utilities_2.createUri)('path/to/notebook.dib', 'remote');
        const workspaceFolderUris = [
            (0, utilities_2.createUri)('not/used/1'),
            (0, utilities_2.createUri)('not/used/2'),
        ];
        const workingDir = (0, utilities_2.getWorkingDirectoryForNotebook)(notebookUri, workspaceFolderUris, 'fallback-is-used');
        (0, chai_1.expect)(workingDir).to.equal('fallback-is-used');
    });
    it('debounce test', () => __awaiter(void 0, void 0, void 0, function* () {
        let callbackHit = false;
        let debounceDelay = 500;
        // repeatedly call `debounce` with small delays
        for (let i = 0; i < 10; i++) {
            (0, utilities_2.debounce)('key', debounceDelay, () => {
                if (callbackHit) {
                    throw new Error('Deboune callback was called more than once; this should never happen.');
                }
                callbackHit = true;
            });
            yield new Promise(resolve => setTimeout(resolve, 100));
        }
        // wait for the debounced callback to actually have a chance
        yield new Promise(resolve => setTimeout(resolve, debounceDelay * 1.1));
        (0, chai_1.expect)(callbackHit).to.be.true;
    })).timeout(5000);
    it('cell error output shape can be detected', () => {
        // strongly typed to catch interface changes
        const error = {
            errorName: 'ename',
            errorValue: 'evalue',
            stackTrace: [
                'stack trace line 1',
                'stack trace line 2'
            ]
        };
        (0, chai_1.expect)((0, utilities_1.isErrorOutput)(error)).to.be.true;
    });
    it('cell display output shape can be detected', () => {
        // strongly typed to catch interface changes
        const display = {
            data: {
                'text/html': 'html',
                'text/plain': 'text'
            },
            metadata: {}
        };
        (0, chai_1.expect)((0, utilities_1.isDisplayOutput)(display)).to.be.true;
    });
    it('cell text output shape can be detected', () => {
        // strongly typed to catch interface changes
        const text = {
            name: 'some name',
            text: 'some text'
        };
        (0, chai_1.expect)((0, utilities_1.isTextOutput)(text)).to.be.true;
    });
    it('parse function can handle base64-encoded Uint8Array', () => {
        const numbers = [1, 2, 3];
        const base64 = Buffer.from(numbers).toString('base64');
        const value = (0, utilities_2.parse)(`{"rawData":"${base64}"}`);
        (0, chai_1.expect)(value).to.deep.equal({
            rawData: Uint8Array.from(numbers)
        });
    });
    it('stringify function can handle Uint8Array', () => {
        const numbers = [1, 2, 3];
        const text = (0, utilities_2.stringify)({
            rawData: new Uint8Array(numbers)
        });
        const expectedBase64 = Buffer.from(numbers).toString('base64');
        (0, chai_1.expect)(text).to.equal(`{"rawData":"${expectedBase64}"}`);
    });
    it('stringify function can handle Buffer', () => {
        const numbers = [1, 2, 3];
        const text = (0, utilities_2.stringify)({
            rawData: Buffer.from(numbers)
        });
        const expectedBase64 = Buffer.from(numbers).toString('base64');
        (0, chai_1.expect)(text).to.equal(`{"rawData":"${expectedBase64}"}`);
    });
    it('stringify function can handle cell output', () => {
        const numbers = [97, 98, 99];
        const text = (0, utilities_2.stringify)({
            'text/html': Buffer.from(numbers)
        });
        (0, chai_1.expect)(text).to.equal(`{"text/html":"abc"}`);
    });
    describe('vs code output value reshaping', () => {
        it('error string is reshaped', () => {
            const reshaped = (0, utilities_1.reshapeOutputValueForVsCode)('some error message', vscodeLike.ErrorOutputMimeType);
            const decoded = JSON.parse((0, utilities_3.decodeToString)(reshaped));
            (0, chai_1.expect)(decoded).to.deep.equal({
                ename: 'Error',
                evalue: 'some error message',
                traceback: [],
            });
        });
        it(`non-error mime type doesn't reshape output`, () => {
            const reshaped = (0, utilities_1.reshapeOutputValueForVsCode)('some error message', 'text/plain');
            const decoded = (0, utilities_3.decodeToString)(reshaped);
            (0, chai_1.expect)(decoded).to.equal('some error message');
        });
    });
    it('executing a non-existent process still returns', () => __awaiter(void 0, void 0, void 0, function* () {
        const result = yield (0, utilities_2.executeSafe)('this-is-a-command-that-will-fail', []);
        (0, chai_1.expect)(result).to.deep.equal({
            code: -1,
            output: '',
            error: 'Error: spawn this-is-a-command-that-will-fail ENOENT',
        });
    }));
    describe('dotnet version checking', () => {
        it('version number can be obtained from simple value', () => {
            const version = (0, utilities_2.getVersionNumber)('5.0');
            (0, chai_1.expect)(version).to.equal('5.0');
        });
        it(`version number from empty string doesn't throw`, () => {
            const version = (0, utilities_2.getVersionNumber)('');
            (0, chai_1.expect)(version).to.equal('');
        });
        for (const newline of ['\n', '\r\n']) {
            it(`version number with leading and trailing newlines returns correct result with ${JSON.stringify(newline)} newlines`, () => {
                const version = (0, utilities_2.getVersionNumber)(`${newline}5.0${newline}`);
                (0, chai_1.expect)(version).to.equal('5.0');
            });
            it(`version number with first-run text is properly pulled out with ${JSON.stringify(newline)} newlines`, () => {
                const version = (0, utilities_2.getVersionNumber)(`${newline}Welcome to .NET 5.0!${newline}--------${newline}5.0.101`);
                (0, chai_1.expect)(version).to.equal('5.0.101');
            });
        }
    });
});
//# sourceMappingURL=misc.test.js.map