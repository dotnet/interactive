// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { DisplayElement, ErrorElement, TextElement } from '../../interfaces/contracts';
import { isDisplayOutput, isErrorOutput, isTextOutput, reshapeOutputValueForVsCode } from '../../interfaces/utilities';
import { isDotNetNotebookMetadata, isIpynbFile } from '../../ipynbUtilities';
import { createUri, debounce, executeSafe, getWorkingDirectoryForNotebook, isDotNetUpToDate, parse, processArguments, stringify } from '../../utilities';
import { decodeToString } from './utilities';

import * as vscodeLike from '../../interfaces/vscode-like';

describe('Miscellaneous tests', () => {
    describe('.NET notebook detection', () => {
        it('.NET notebook is detected by kernelspec', () => {
            const metadata = {
                custom: {
                    metadata: {
                        kernelspec: {
                            name: '.net-fsharp-dev'
                        }
                    }
                }
            };
            expect(isDotNetNotebookMetadata(metadata)).is.true;
        });

        it('.NET notebook is detected by language info', () => {
            const metadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: 'dotnet-interactive.pwsh'
                        }
                    }
                }
            };
            expect(isDotNetNotebookMetadata(metadata)).is.true;
        });

        it('non-.NET notebook is not detected by kernelspec', () => {
            const metadata = {
                custom: {
                    metadata: {
                        kernelspec: {
                            name: 'python'
                        }
                    }
                }
            };
            expect(isDotNetNotebookMetadata(metadata)).is.false;
        });

        it('non-.NET notebook is not detected by language info', () => {
            const metadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: 'python'
                        }
                    }
                }
            };
            expect(isDotNetNotebookMetadata(metadata)).is.false;
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
        let actual = processArguments(template, 'replacement-working-dir', 'replacement-dotnet-path', 'replacement-global-storage-path');
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

    it('notebook working directory comes from notebook uri if local', () => {
        const notebookUri = createUri('path/to/notebook.dib');
        const workspaceFolderUris = [
            createUri('not/used/1'),
            createUri('not/used/2'),
        ];
        const workingDir = getWorkingDirectoryForNotebook(notebookUri, workspaceFolderUris, 'fallback-not-used');
        expect(workingDir).to.equal('path/to');
    });

    it('notebook working directory comes from local workspace if notebook is untitled', () => {
        const notebookUri = createUri('path/to/notebook.dib', 'untitled');
        const workspaceFolderUris = [
            createUri('not/used', 'remote'),
            createUri('this/is/local/and/used'),
        ];
        const workingDir = getWorkingDirectoryForNotebook(notebookUri, workspaceFolderUris, 'fallback-not-used');
        expect(workingDir).to.equal('this/is/local/and/used');
    });

    it('notebook working directory comes from fallback if notebook is remote', () => {
        const notebookUri = createUri('path/to/notebook.dib', 'remote');
        const workspaceFolderUris = [
            createUri('not/used/1'),
            createUri('not/used/2'),
        ];
        const workingDir = getWorkingDirectoryForNotebook(notebookUri, workspaceFolderUris, 'fallback-is-used');
        expect(workingDir).to.equal('fallback-is-used');
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
        const error: ErrorElement = {
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
        const display: DisplayElement = {
            data: {
                'text/html': 'html',
                'text/plain': 'text'
            }
        };
        expect(isDisplayOutput(display)).to.be.true;
    });

    it('cell text output shape can be detected', () => {
        // strongly typed to catch interface changes
        const text: TextElement = {
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

    describe('vs code output value reshaping', () => {
        it('error string is reshaped', () => {
            const reshaped = reshapeOutputValueForVsCode('some error message', vscodeLike.ErrorOutputMimeType);
            const decoded = JSON.parse(decodeToString(reshaped));
            expect(decoded).to.deep.equal({
                ename: 'Error',
                evalue: 'some error message',
                traceback: [],
            });
        });

        it(`non-error mime type doesn't reshape output`, () => {
            const reshaped = reshapeOutputValueForVsCode('some error message', 'text/plain');
            const decoded = decodeToString(reshaped);
            expect(decoded).to.equal('some error message');
        });
    });

    it('executing a non-existent process still returns', async () => {
        const result = await executeSafe('this-is-a-command-that-will-fail', []);
        expect(result).to.deep.equal({
            code: -1,
            output: '',
            error: 'Error: spawn this-is-a-command-that-will-fail ENOENT',
        });
    });

    describe('dotnet version sufficiency tests', () => {
        it('supported version is allowed', () => {
            const isSupported = isDotNetUpToDate('5.0', { code: 0, output: '5.0.101' });
            expect(isSupported).to.be.true;
        });

        it(`version number acquisition passes, but version isn't sufficient`, () => {
            const isSupported = isDotNetUpToDate('5.0', { code: 0, output: '3.1.403' });
            expect(isSupported).to.be.false;
        });

        it(`version number looks good, but return code wasn't`, () => {
            const isSupported = isDotNetUpToDate('5.0', { code: 1, output: '5.0.101' });
            expect(isSupported).to.be.false;
        });

        it('version number check crashed', () => {
            const isSupported = isDotNetUpToDate('5.0', { code: -1, output: '' });
            expect(isSupported).to.be.false;
        });
    });

    describe('.ipynb helpers', () => {
        it('file extension of .ipynb matches', () => {
            expect(isIpynbFile('notebook.ipynb')).to.be.true;
            expect(isIpynbFile('NOTEBOOK.IPYNB')).to.be.true;
        });

        it(`file extension of .dib doesn't match`, () => {
            expect(isIpynbFile('notebook.dib')).to.be.false;
            expect(isIpynbFile('notebook.dotnet-interactive')).to.be.false;
        });
    });
});
