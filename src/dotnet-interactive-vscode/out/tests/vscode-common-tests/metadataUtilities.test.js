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
const metadataUtilities = require("../../src/vscode-common/metadataUtilities");
const vscodeLike = require("../../src/vscode-common/interfaces/vscode-like");
const chai_1 = require("chai");
const dotnet_interactive_1 = require("../../src/vscode-common/dotnet-interactive");
describe('metadata utility tests', () => __awaiter(void 0, void 0, void 0, function* () {
    it('ipynb notebooks can be identified', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.ipynb',
                scheme: 'untitled'
            },
            metadata: {}
        };
        const isIpynb = metadataUtilities.isIpynbNotebook(notebookDocument);
        (0, chai_1.expect)(isIpynb).to.be.true;
    });
    it('interactive notebook can be identified from .dib', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.dib',
                scheme: 'untitled'
            },
            metadata: {}
        };
        const isDotNet = metadataUtilities.isDotNetNotebook(notebookDocument);
        (0, chai_1.expect)(isDotNet).to.be.true;
    });
    it('interactive notebook can be identified from metadata in .ipynb', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.ipynb',
                scheme: 'untitled'
            },
            metadata: {
                custom: {
                    metadata: {
                        kernelspec: {
                            name: '.net-csharp'
                        }
                    }
                }
            }
        };
        const isDotNet = metadataUtilities.isDotNetNotebook(notebookDocument);
        (0, chai_1.expect)(isDotNet).to.be.true;
    });
    it('non-interactive notebook is not identified from metadata in .ipynb', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.ipynb',
                scheme: 'untitled'
            },
            metadata: {
                custom: {
                    metadata: {
                        kernelspec: {
                            name: 'python'
                        }
                    }
                }
            }
        };
        const isDotNet = metadataUtilities.isDotNetNotebook(notebookDocument);
        (0, chai_1.expect)(isDotNet).to.be.false;
    });
    it('cell metadata can be extracted from an interactive document element with old metadata', () => {
        const interactiveDocumentElement = {
            contents: '',
            outputs: [],
            executionOrder: 0,
            metadata: {
                dotnet_interactive: {
                    language: 'fsharp'
                }
            }
        };
        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromInteractiveDocumentElement(interactiveDocumentElement);
        (0, chai_1.expect)(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('cell metadata can be extracted from an interactive document element', () => {
        const interactiveDocumentElement = {
            contents: '',
            outputs: [],
            executionOrder: 0,
            metadata: {
                polyglot_notebook: {
                    kernelName: 'fsharp'
                }
            }
        };
        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromInteractiveDocumentElement(interactiveDocumentElement);
        (0, chai_1.expect)(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('cell metadata can be extracted from a notebook cell with old metadata', () => {
        const notebookCell = {
            kind: vscodeLike.NotebookCellKind.Code,
            metadata: {
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language: 'fsharp'
                        }
                    }
                }
            }
        };
        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(notebookCell);
        (0, chai_1.expect)(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('cell metadata can be extracted from a notebook cell', () => {
        const notebookCell = {
            kind: vscodeLike.NotebookCellKind.Code,
            metadata: {
                custom: {
                    metadata: {
                        polyglot_notebook: {
                            kernelName: 'fsharp'
                        }
                    }
                }
            }
        };
        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(notebookCell);
        (0, chai_1.expect)(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('cell metadata can be extracted from a notebook cell', () => {
    });
    it('notebook metadata can be extracted from an interactive document', () => {
        const interactiveDocument = {
            elements: [],
            metadata: {
                custom: {
                    name: 'some value'
                },
                kernelInfo: {
                    defaultKernelName: 'fsharp',
                    items: [
                        {
                            name: 'fsharp',
                            aliases: ['fs'],
                            languageName: 'fsharp'
                        }
                    ]
                }
            }
        };
        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromInteractiveDocument(interactiveDocument);
        (0, chai_1.expect)(notebookDocumentMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        });
    });
    it('notebook metadata can be extracted from vscode notebook document', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'some-notebook.ipynb',
                scheme: 'file'
            },
            metadata: {
                custom: {
                    name: 'some value'
                },
                polyglot_notebook: {
                    kernelInfo: {
                        defaultKernelName: 'fsharp',
                        items: [
                            {
                                name: 'fsharp',
                                aliases: ['fs'],
                                languageName: 'fsharp'
                            }
                        ]
                    }
                }
            }
        };
        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
        (0, chai_1.expect)(notebookDocumentMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        });
    });
    it('kernel infos can be created from .dib notebook document', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.dib',
                scheme: 'file'
            },
            metadata: {
                polyglot_notebook: {
                    kernelInfo: {
                        defaultKernelName: 'fsharp',
                        items: [
                            {
                                name: 'fsharp',
                                aliases: []
                            },
                            {
                                name: 'snake',
                                languageName: 'python',
                                aliases: []
                            }
                        ]
                    }
                }
            }
        };
        const kernelInfos = metadataUtilities.getKernelInfosFromNotebookDocument(notebookDocument);
        (0, chai_1.expect)(kernelInfos).to.deep.equal([
            {
                localName: 'fsharp',
                uri: 'unused',
                aliases: [],
                languageName: undefined,
                displayName: 'unused',
                supportedKernelCommands: [],
                supportedDirectives: []
            },
            {
                localName: 'snake',
                uri: 'unused',
                aliases: [],
                languageName: 'python',
                displayName: 'unused',
                supportedKernelCommands: [],
                supportedDirectives: []
            }
        ]);
    });
    it('kernel infos can be created from .ipynb notebook document', () => {
        const notebookDocument = {
            uri: {
                fsPath: 'notebook.ipynb',
                scheme: 'file'
            },
            metadata: {
                custom: {
                    metadata: {
                        polyglot_notebook: {
                            kernelInfo: {
                                defaultKernelName: 'fsharp',
                                items: [
                                    {
                                        name: 'fsharp',
                                        aliases: []
                                    },
                                    {
                                        name: 'snake',
                                        languageName: 'python',
                                        aliases: []
                                    }
                                ]
                            }
                        }
                    }
                }
            }
        };
        const kernelInfos = metadataUtilities.getKernelInfosFromNotebookDocument(notebookDocument);
        (0, chai_1.expect)(kernelInfos).to.deep.equal([
            {
                localName: 'fsharp',
                uri: 'unused',
                aliases: [],
                languageName: undefined,
                displayName: 'unused',
                supportedKernelCommands: [],
                supportedDirectives: []
            },
            {
                localName: 'snake',
                uri: 'unused',
                aliases: [],
                languageName: 'python',
                displayName: 'unused',
                supportedKernelCommands: [],
                supportedDirectives: []
            }
        ]);
    });
    it('kernelspec metadata can be created from notebook document metadata (C#)', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'csharp',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        (0, chai_1.expect)(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (C#)',
            language: 'C#',
            name: '.net-csharp'
        });
    });
    it('kernelspec metadata can be created from notebook document metadata (F#)', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        (0, chai_1.expect)(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (F#)',
            language: 'F#',
            name: '.net-fsharp'
        });
    });
    it('kernelspec metadata can be created from notebook document metadata (PowerShell)', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'pwsh',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        (0, chai_1.expect)(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (PowerShell)',
            language: 'PowerShell',
            name: '.net-pwsh'
        });
    });
    it('new ipynb metadata can be created from existing data', () => {
        const notebookMetadata = {
            kernelInfo: {
                defaultKernelName: 'csharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    }
                ]
            }
        };
        const existingMetadata = {
            custom: {
                metadata: {
                    kernelspec: 'this gets replaced',
                    someKey: 'some value'
                },
                metadata2: 'electric boogaloo'
            },
            notCustom: 'not custom'
        };
        const newRawMetadata = metadataUtilities.createNewIpynbMetadataWithNotebookDocumentMetadata(existingMetadata, notebookMetadata);
        (0, chai_1.expect)(newRawMetadata).to.deep.equal({
            custom: {
                metadata: {
                    kernelspec: {
                        display_name: '.NET (C#)',
                        language: 'C#',
                        name: '.net-csharp'
                    },
                    polyglot_notebook: {
                        kernelInfo: {
                            defaultKernelName: 'csharp',
                            items: [
                                {
                                    name: 'csharp',
                                    aliases: ['cs'],
                                    languageName: 'csharp'
                                }
                            ]
                        }
                    },
                    someKey: 'some value'
                },
                metadata2: 'electric boogaloo'
            },
            notCustom: 'not custom'
        });
    });
    it('notebook metadata can be extracted from a composite kernel', () => {
        const kernel = new dotnet_interactive_1.CompositeKernel('composite');
        const cs = new dotnet_interactive_1.Kernel('csharp', 'csharp');
        cs.kernelInfo.aliases.push('cs');
        kernel.add(cs);
        const fs = new dotnet_interactive_1.Kernel('fsharp', 'fsharp');
        fs.kernelInfo.aliases.push('fs');
        kernel.add(fs);
        kernel.defaultKernelName = fs.name;
        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromCompositeKernel(kernel);
        (0, chai_1.expect)(notebookDocumentMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        });
    });
    it('interactive document cell metadata can be created from notebook cell metadata', () => {
        const notebookCellMetadata = {
            kernelName: 'fsharp'
        };
        const interactiveDocumentElementMetadata = metadataUtilities.getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata(notebookCellMetadata);
        (0, chai_1.expect)(interactiveDocumentElementMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('notebook cell metadata can be created from notebook cell metadata', () => {
        const notebookCellMetadata = {
            kernelName: 'fsharp'
        };
        const rawNotebookDocumentElementMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
        (0, chai_1.expect)(rawNotebookDocumentElementMetadata).to.deep.equal({
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'fsharp'
                    },
                    polyglot_notebook: {
                        kernelName: 'fsharp'
                    }
                }
            }
        });
    });
    it('interactive document metadata can be created from notebook metadata', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        };
        const interactiveDocumentMetadata = metadataUtilities.getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        (0, chai_1.expect)(interactiveDocumentMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        });
    });
    it('notebook document metadata can be created from notebook metadata for ipynb', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        };
        const rawNotebookDocumentMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, true);
        (0, chai_1.expect)(rawNotebookDocumentMetadata).to.deep.equal({
            custom: {
                metadata: {
                    kernelspec: {
                        display_name: ".NET (F#)",
                        language: "F#",
                        name: ".net-fsharp"
                    },
                    polyglot_notebook: {
                        kernelInfo: {
                            defaultKernelName: 'fsharp',
                            items: [
                                {
                                    name: 'csharp',
                                    aliases: ['cs'],
                                    languageName: 'csharp'
                                },
                                {
                                    name: 'fsharp',
                                    aliases: ['fs'],
                                    languageName: 'fsharp'
                                }
                            ]
                        }
                    }
                }
            }
        });
    });
    it('notebook document metadata can be created from notebook metadata for dib', () => {
        const notebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        };
        const rawNotebookDocumentMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, false);
        (0, chai_1.expect)(rawNotebookDocumentMetadata).to.deep.equal({
            polyglot_notebook: {
                kernelInfo: {
                    defaultKernelName: 'fsharp',
                    items: [
                        {
                            name: 'csharp',
                            aliases: ['cs'],
                            languageName: 'csharp'
                        },
                        {
                            name: 'fsharp',
                            aliases: ['fs'],
                            languageName: 'fsharp'
                        }
                    ]
                }
            }
        });
    });
    it('notebook cell metadata can be merged', () => {
        const baseMetadata = {
            kernelName: 'csharp'
        };
        const metadataWithNewValues = {
            kernelName: 'fsharp'
        };
        const resultMetadata = metadataUtilities.mergeNotebookCellMetadata(baseMetadata, metadataWithNewValues);
        (0, chai_1.expect)(resultMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });
    it('notebook metadata can be merged', () => {
        const baseMetadata = {
            kernelInfo: {
                defaultKernelName: 'base default kernel name',
                items: [
                    {
                        name: 'csharp',
                        aliases: [],
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        };
        const metadataWithNewValues = {
            kernelInfo: {
                defaultKernelName: 'original default kernel name will be retained',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    }
                ]
            }
        };
        const resultMetadata = metadataUtilities.mergeNotebookDocumentMetadata(baseMetadata, metadataWithNewValues);
        (0, chai_1.expect)(resultMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'base default kernel name',
                items: [
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    },
                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    }
                ]
            }
        });
    });
    it('raw metadata can be merged', () => {
        const baseMetadata = {
            custom: {
                name: 'some value'
            },
            polyglot_notebook: {
                this_will: 'be replaced'
            }
        };
        const metadataWithNewValues = {
            polyglot_notebook: {
                kernelInfo: {
                    defaultKernelName: 'fsharp',
                    items: [
                        {
                            name: 'fsharp',
                            aliases: ['fs'],
                            languageName: 'fsharp'
                        }
                    ]
                }
            }
        };
        const resultMetadata = metadataUtilities.mergeRawMetadata(baseMetadata, metadataWithNewValues);
        (0, chai_1.expect)(resultMetadata).to.deep.equal({
            custom: {
                name: 'some value'
            },
            polyglot_notebook: {
                kernelInfo: {
                    defaultKernelName: 'fsharp',
                    items: [
                        {
                            name: 'fsharp',
                            aliases: ['fs'],
                            languageName: 'fsharp'
                        }
                    ]
                }
            }
        });
    });
}));
//# sourceMappingURL=metadataUtilities.test.js.map