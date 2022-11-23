// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../../src/vscode-common/dotnet-interactive/contracts';
import * as kernelSelectorUtilities from '../../src/vscode-common/kernelSelectorUtilities';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';
import { expect } from 'chai';
import { CompositeKernel, Kernel } from '../../src/vscode-common/dotnet-interactive';

describe('kernel selector utility tests', async () => {

    it('kernel selector options are properly generated from composite kernel and notebook metadata', () => {
        const kernel = new CompositeKernel('composite');

        // add C# kernel that supports `SubmitCode`
        const cs = new Kernel('csharp', 'csharp', '10.0', 'See Sharp');
        cs.kernelInfo.supportedKernelCommands = [{ name: contracts.SubmitCodeType }];
        kernel.add(cs);

        // add webview kernel that _doesn't_ support `SubmitCode`
        const wv = new Kernel('webview');
        kernel.add(wv);

        const notebookDocument: vscodeLike.NotebookDocument = {
            uri: {
                fsPath: 'some-notebook',
                scheme: 'file'
            },
            metadata: {
                polyglot_notebook: {
                    kernelInfo: {
                        defaultKernelName: 'unused',
                        items: [
                            {
                                name: 'csharp',
                                aliases: []
                            },
                            {
                                name: 'fsharp',
                                aliases: []
                            }
                        ]
                    }
                }
            }
        };

        const kernelSelectorOptions = kernelSelectorUtilities.getKernelSelectorOptions(kernel, notebookDocument, contracts.SubmitCodeType);
        expect(kernelSelectorOptions).to.deep.equal([
            {
                kernelName: 'csharp',
                displayValue: 'csharp - See Sharp',
                languageName: 'csharp'
            },
            {
                kernelName: 'fsharp',
                displayValue: 'fsharp - fsharp'
            }
        ]);
    });

});
