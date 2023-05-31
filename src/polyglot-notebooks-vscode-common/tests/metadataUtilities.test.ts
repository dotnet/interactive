// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from '../../src/vscode-common/polyglot-notebooks/commandsAndEvents';
import * as metadataUtilities from '../../src/vscode-common/metadataUtilities';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';
import { expect } from 'chai';
import { CompositeKernel, Kernel } from '../../src/vscode-common/polyglot-notebooks';

describe('metadata utility tests', async () => {

    it('ipynb notebooks can be identified', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
            uri: {
                fsPath: 'notebook.ipynb',
                scheme: 'untitled'
            },
            metadata: {}
        };
        const isIpynb = metadataUtilities.isIpynbNotebook(notebookDocument);
        expect(isIpynb).to.be.true;
    });

    it('interactive notebook can be identified from .dib', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
            uri: {
                fsPath: 'notebook.dib',
                scheme: 'untitled'
            },
            metadata: {}
        };
        const isDotNet = metadataUtilities.isDotNetNotebook(notebookDocument);
        expect(isDotNet).to.be.true;
    });

    it('interactive notebook can be identified from metadata in .ipynb', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
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
        expect(isDotNet).to.be.true;
    });

    it('non-interactive notebook is not identified from metadata in .ipynb', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
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
        expect(isDotNet).to.be.false;
    });

    it('cell metadata can be extracted from an interactive document element with old metadata', () => {
        const interactiveDocumentElement: commandsAndEvents.InteractiveDocumentElement = {
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
        expect(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('cell metadata can be extracted from an interactive document element', () => {
        const interactiveDocumentElement: commandsAndEvents.InteractiveDocumentElement = {
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
        expect(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('cell metadata can be extracted from a notebook cell with old metadata', () => {
        const notebookCell: vscodeLike.NotebookCell = {
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
        expect(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('cell metadata can be extracted from a notebook cell', () => {
        const notebookCell: vscodeLike.NotebookCell = {
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
        expect(notebookCellMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('cell metadata can be extracted from a notebook cell', () => {

    });

    it('notebook metadata can be extracted from an interactive document', () => {
        const interactiveDocument: commandsAndEvents.InteractiveDocument = {
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
        expect(notebookDocumentMetadata).to.deep.equal({
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
        const notebookDocument: vscodeLike.NotebookDocument = {
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
        expect(notebookDocumentMetadata).to.deep.equal({
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

    it('notebook document metadata can be synthesized from .ipynb kernelspec', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
            uri: {
                fsPath: 'test-notebook.ipynb',
                scheme: 'unused',
            },
            metadata: {
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (F#)',
                            language: 'F#',
                            name: '.net-fsharp',
                        }
                    }
                }
            }
        };
        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
        expect(notebookDocumentMetadata).to.deep.equal({
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [
                    {
                        name: 'fsharp',
                        aliases: [],
                    }
                ]
            }
        });
    });

    it('kernel infos can be created from .dib notebook document', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
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
        expect(kernelInfos).to.deep.equal([{
            aliases: [],
            displayName: 'unused',
            isComposite: false,
            isProxy: false,
            localName: 'fsharp',
            supportedDirectives: [],
            supportedKernelCommands: [],
            uri: 'unused'
        },
        {
            aliases: [],
            displayName: 'unused',
            isComposite: false,
            isProxy: false,
            languageName: 'python',
            localName: 'snake',
            supportedDirectives: [],
            supportedKernelCommands: [],
            uri: 'unused'
        }]);
    });

    it('kernel infos can be created from .ipynb notebook document', () => {
        const notebookDocument: vscodeLike.NotebookDocument = {
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
        expect(kernelInfos).to.deep.equal([{
            aliases: [],
            displayName: 'unused',
            isComposite: false,
            isProxy: false,
            localName: 'fsharp',
            supportedDirectives: [],
            supportedKernelCommands: [],
            uri: 'unused'
        },
        {
            aliases: [],
            displayName: 'unused',
            isComposite: false,
            isProxy: false,
            languageName: 'python',
            localName: 'snake',
            supportedDirectives: [],
            supportedKernelCommands: [],
            uri: 'unused'
        }]);
    });

    it('kernelspec metadata can be created from notebook document metadata (C#)', () => {
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'csharp',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        expect(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (C#)',
            language: 'C#',
            name: '.net-csharp'
        });
    });

    it('kernelspec metadata can be created from notebook document metadata (F#)', () => {
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        expect(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (F#)',
            language: 'F#',
            name: '.net-fsharp'
        });
    });

    it('kernelspec metadata can be created from notebook document metadata (PowerShell)', () => {
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'pwsh',
                items: []
            }
        };
        const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        expect(kernelspecMetadata).to.deep.equal({
            display_name: '.NET (PowerShell)',
            language: 'PowerShell',
            name: '.net-pwsh'
        });
    });

    it('new ipynb metadata can be created from existing data', () => {
        const notebookMetadata: metadataUtilities.NotebookDocumentMetadata = {
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
        const existingMetadata: { [key: string]: any } = {
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
        expect(newRawMetadata).to.deep.equal({
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
        const kernel = new CompositeKernel('composite');
        const cs = new Kernel('csharp', 'csharp');
        cs.registerCommandHandler({ commandType: commandsAndEvents.SubmitCodeType, handle: (_invocation) => Promise.resolve() });
        cs.kernelInfo.aliases.push('cs');
        kernel.add(cs);
        const fs = new Kernel('fsharp', 'fsharp');
        fs.registerCommandHandler({ commandType: commandsAndEvents.SubmitCodeType, handle: (_invocation) => Promise.resolve() });
        fs.kernelInfo.aliases.push('fs');
        kernel.add(fs);
        const value = new Kernel('value', 'value');
        kernel.defaultKernelName = fs.name;

        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromCompositeKernel(kernel);
        expect(notebookDocumentMetadata).to.deep.equal({
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
        const notebookCellMetadata: metadataUtilities.NotebookCellMetadata = {
            kernelName: 'fsharp'
        };
        const interactiveDocumentElementMetadata = metadataUtilities.getRawInteractiveDocumentElementMetadataFromNotebookCellMetadata(notebookCellMetadata);
        expect(interactiveDocumentElementMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('notebook cell metadata can be created from notebook cell metadata', () => {
        const notebookCellMetadata: metadataUtilities.NotebookCellMetadata = {
            kernelName: 'fsharp'
        };
        const rawNotebookDocumentElementMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
        expect(rawNotebookDocumentElementMetadata).to.deep.equal({
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
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
            kernelInfo: {
                defaultKernelName: 'fsharp',
                items: [

                    {
                        name: 'fsharp',
                        aliases: ['fs'],
                        languageName: 'fsharp'
                    },
                    {
                        name: 'csharp',
                        aliases: ['cs'],
                        languageName: 'csharp'
                    }
                ]
            }
        };
        const interactiveDocumentMetadata = metadataUtilities.getRawInteractiveDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata);
        expect(interactiveDocumentMetadata).to.deep.equal({
            kernelInfo:
            {
                defaultKernelName: 'fsharp',
                items:
                    [{ aliases: ['fs'], languageName: 'fsharp', name: 'fsharp' },
                    { aliases: ['cs'], languageName: 'csharp', name: 'csharp' }]
            }
        });
    });

    it('notebook document metadata can be created from notebook metadata for ipynb', () => {
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
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
        const rawNotebookDocumentMetadata = metadataUtilities.getMergedRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, {}, true);
        expect(rawNotebookDocumentMetadata).to.deep.equal({
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
        const notebookDocumentMetadata: metadataUtilities.NotebookDocumentMetadata = {
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
        const rawNotebookDocumentMetadata = metadataUtilities.getMergedRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookDocumentMetadata, {}, false);
        expect(rawNotebookDocumentMetadata).to.deep.equal({
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
        const baseMetadata: metadataUtilities.NotebookCellMetadata = {
            kernelName: 'csharp'
        };
        const metadataWithNewValues: metadataUtilities.NotebookCellMetadata = {
            kernelName: 'fsharp'
        };
        const resultMetadata = metadataUtilities.mergeNotebookCellMetadata(baseMetadata, metadataWithNewValues);
        expect(resultMetadata).to.deep.equal({
            kernelName: 'fsharp'
        });
    });

    it('notebook metadata can be merged', () => {
        const baseMetadata: metadataUtilities.NotebookDocumentMetadata = {
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
        const metadataWithNewValues: metadataUtilities.NotebookDocumentMetadata = {
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
        expect(resultMetadata).to.deep.equal({
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

    it("merges metadata correctly", () => {
        let destination = {
            "custom": {
                "metadata": {
                    "kernelspec": {
                        "display_name": ".NET (C#)",
                        "language": "C#",
                        "name": ".net-csharp"
                    },
                    "polyglot_notebook": {
                        "kernelInfo": {
                            "defaultKernelName": "powershell",
                            "items": [
                                {
                                    "aliases": [
                                    ],
                                    "languageName": null,
                                    "name": ".NET"
                                },
                                {
                                    "aliases": [
                                        "C#",
                                        "c#"
                                    ],
                                    "languageName": "C#",
                                    "name": "csharp"
                                },
                                {
                                    "aliases": [
                                        "F#",
                                        "f#"
                                    ],
                                    "languageName": "F#",
                                    "name": "fsharp"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": "HTML",
                                    "name": "html"
                                },
                                {
                                    "aliases": [
                                        "js"
                                    ],
                                    "languageName": "JavaScript",
                                    "name": "javascript"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": "Mermaid",
                                    "name": "mermaid"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "name": "powershell"
                                },
                                {
                                    "aliases": [
                                        "powershell"
                                    ],
                                    "languageName": "PowerShell",
                                    "name": "pwsh"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": null,
                                    "name": "value"
                                },
                                {
                                    "aliases": [
                                        "frontend"
                                    ],
                                    "languageName": null,
                                    "name": "vscode"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "name": "webview"
                                }
                            ]
                        }
                    }
                }
            }
        };

        let source = {
            "custom": {
                "metadata": {
                    "kernelspec": {
                        "display_name": ".NET (C#)",
                        "language": "C#",
                        "name": ".net-csharp"
                    },
                    "polyglot_notebook": {
                        "kernelInfo": {
                            "defaultKernelName": "powershell",
                            "items": [
                                {
                                    "aliases": [
                                        "C#",
                                        "c#"
                                    ],
                                    "languageName": "C#",
                                    "name": "csharp"
                                },
                                {
                                    "aliases": [
                                        "F#",
                                        "f#"
                                    ],
                                    "languageName": "F#",
                                    "name": "fsharp"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": "HTML",
                                    "name": "html"
                                },
                                {
                                    "aliases": [
                                        "js"
                                    ],
                                    "languageName": "JavaScript",
                                    "name": "javascript"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": "Mermaid",
                                    "name": "mermaid"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "name": "powershell"
                                },
                                {
                                    "aliases": [
                                        "powershell"
                                    ],
                                    "languageName": "PowerShell",
                                    "name": "pwsh"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "languageName": null,
                                    "name": "value"
                                },
                                {
                                    "aliases": [
                                        "frontend"
                                    ],
                                    "languageName": null,
                                    "name": "vscode"
                                },
                                {
                                    "aliases": [
                                    ],
                                    "name": "webview"
                                }
                            ]
                        }
                    }
                },
                "cells": [
                ],
                "nbformat": 4,
                "nbformat_minor": 2
            },
            "indentAmount": " "
        };

        metadataUtilities.sortAndMerge(destination, source);

        expect(destination).to.deep.equal({
            custom:
            {
                cells: [],
                metadata:
                {
                    kernelspec:
                    {
                        display_name: '.NET (C#)',
                        language: 'C#',
                        name: '.net-csharp'
                    },
                    polyglot_notebook:
                    {
                        kernelInfo:
                        {
                            defaultKernelName: 'powershell',
                            items:
                                [{ aliases: [], languageName: null, name: '.NET' },
                                { aliases: ['C#', 'c#'], languageName: 'C#', name: 'csharp' },
                                { aliases: ['F#', 'f#'], languageName: 'F#', name: 'fsharp' },
                                { aliases: [], languageName: 'HTML', name: 'html' },
                                { aliases: ['js'], languageName: 'JavaScript', name: 'javascript' },
                                { aliases: [], languageName: 'Mermaid', name: 'mermaid' },
                                { aliases: [], name: 'powershell' },
                                { aliases: ['powershell'], languageName: 'PowerShell', name: 'pwsh' },
                                { aliases: [], languageName: null, name: 'value' },
                                { aliases: ['frontend'], languageName: null, name: 'vscode' },
                                { aliases: [], name: 'webview' }]
                        }
                    }
                },
                nbformat: 4,
                nbformat_minor: 2
            },
            indentAmount: ' '
        });
    });

    it('raw metadata can be merged', () => {
        const baseMetadata: { [key: string]: any } = {
            custom: {
                name: 'some value'
            },
            polyglot_notebook: {
                this_will: 'be replaced'
            }
        };
        const metadataWithNewValues: { [key: string]: any } = {
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
        expect(resultMetadata).to.deep.equal({
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
});