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
const contracts = require("../../src/vscode-common/dotnet-interactive/contracts");
const kernelSelectorUtilities = require("../../src/vscode-common/kernelSelectorUtilities");
const chai_1 = require("chai");
const dotnet_interactive_1 = require("../../src/vscode-common/dotnet-interactive");
describe('kernel selector utility tests', () => __awaiter(void 0, void 0, void 0, function* () {
    it('kernel selector options are properly generated from composite kernel and notebook metadata', () => {
        const kernel = new dotnet_interactive_1.CompositeKernel('composite');
        // add C# kernel that supports `SubmitCode`
        const cs = new dotnet_interactive_1.Kernel('csharp', 'csharp', '10.0', 'See Sharp');
        cs.kernelInfo.supportedKernelCommands = [{ name: contracts.SubmitCodeType }];
        kernel.add(cs);
        // add webview kernel that _doesn't_ support `SubmitCode`
        const wv = new dotnet_interactive_1.Kernel('webview');
        kernel.add(wv);
        const notebookDocument = {
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
        (0, chai_1.expect)(kernelSelectorOptions).to.deep.equal([
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
}));
//# sourceMappingURL=kernelSelectorUtilities.test.js.map