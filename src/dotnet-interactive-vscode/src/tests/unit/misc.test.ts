// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { NotebookCellDisplayOutput, NotebookCellErrorOutput, NotebookCellTextOutput } from '../../contracts';

import { debounce, isDisplayOutput, isErrorOutput, isTextOutput, mergeObjects, parse, processArguments, stringify } from '../../utilities';

describe('Miscellaneous tests', () => {
    describe('JSON object merging', () => {
        it(`two JSON objects can be merged`, () => {
            const baseObject = {
                key1: {
                    key2: 'value 2'
                },
                key3: 1
            };
            const additionalData = {
                key3: 3
            };
            const result = mergeObjects(baseObject, additionalData);
            expect(result).to.deep.equal({
                key1: {
                    key2: 'value 2'
                },
                key3: 3
            });
        });

        it(`objects can be merged if first is undefined`, () => {
            const baseObject = undefined;
            const additionalData = {
                key3: 3
            };
            const result = mergeObjects(baseObject, additionalData);
            expect(result).to.deep.equal({
                key3: 3
            });
        });

        it(`objects can be merged if second is undefined`, () => {
            const baseObject = {
                key1: {
                    key2: 'value 2'
                }
            };
            const additionalData = undefined;
            const result = mergeObjects(baseObject, additionalData);
            expect(result).to.deep.equal({
                key1: {
                    key2: 'value 2'
                }
            });
        });

        it(`objects can be merged if both are undefined`, () => {
            const result = mergeObjects(undefined, undefined);
            expect(result).to.deep.equal({});
        });
    });

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
        let actual = processArguments(template, 'replacement-working-dir/notebook-file.dib', 'unused-working-dir', 'replacement-dotnet-path', 'replacement-global-storage-path');
        expect(actual).to.deep.equal({
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
            workingDirectory: 'replacement-global-storage-path'
        });
    });

    it(`uses the fallback working directory when it can't be reasonably determined`, () => {
        const template = {
            args: [
                '{dotnet_path}',
                '--working-dir',
                '{working_dir}'
            ],
            workingDirectory: '{global_storage_path}'
        };
        let actual = processArguments(template, 'notebook-file-with-no-dir.dib', 'fallback-working-dir', 'dotnet-path', 'global-storage-path');
        expect(actual).to.deep.equal({
            command: 'dotnet-path',
            args: [
                '--working-dir',
                'fallback-working-dir'
            ],
            workingDirectory: 'global-storage-path'
        });
    });

    it('debounce test', async () => {
        let callbackHit = false;
        let debounceDelay = 500;

        // repeatedly call `debounce` with small delays
        for (let i = 0; i < 10; i++) {
            debounce('key', debounceDelay, () => {
                if (callbackHit) {
                    throw new Error('Deboune callback was called more than once; this should never happen.');
                }

                callbackHit = true;
            });

            await new Promise(resolve => setTimeout(resolve, 100));
        }

        // wait for the debounced callback to actually have a chance
        await new Promise(resolve => setTimeout(resolve, debounceDelay * 1.1));
        expect(callbackHit).to.be.true;
    }).timeout(5000);

    it('cell error output shape can be detected', () => {
        // strongly typed to catch interface changes
        const error: NotebookCellErrorOutput = {
            errorName: 'ename',
            errorValue: 'evalue',
            stackTrace: [
                'stack trace line 1',
                'stack trace line 2'
            ]
        };
        expect(isErrorOutput(error)).to.be.true;
    });

    it('cell display output shape can be detected', () => {
        // strongly typed to catch interface changes
        const display: NotebookCellDisplayOutput = {
            data: {
                'text/html': 'html',
                'text/plain': 'text'
            }
        };
        expect(isDisplayOutput(display)).to.be.true;
    });

    it('cell text output shape can be detected', () => {
        // strongly typed to catch interface changes
        const text: NotebookCellTextOutput = {
            text: 'some text'
        };
        expect(isTextOutput(text)).to.be.true;
    });

    it('parse function can handle base64-encoded Uint8Array', () => {
        const numbers = [1, 2, 3];
        const base64 = Buffer.from(numbers).toString('base64');
        const value = parse(`{"rawData":"${base64}"}`);
        expect(value).to.deep.equal({
            rawData: Uint8Array.from(numbers)
        });
    });

    it('stringify function can handle Uint8Array', () => {
        const numbers = [1, 2, 3];
        const text = stringify({
            rawData: new Uint8Array(numbers)
        });
        const expectedBase64 = Buffer.from(numbers).toString('base64');
        expect(text).to.equal(`{"rawData":"${expectedBase64}"}`);
    });

    it('stringify function can handle Buffer', () => {
        const numbers = [1, 2, 3];
        const text = stringify({
            rawData: Buffer.from(numbers)
        });
        const expectedBase64 = Buffer.from(numbers).toString('base64');
        expect(text).to.equal(`{"rawData":"${expectedBase64}"}`);
    });
});
