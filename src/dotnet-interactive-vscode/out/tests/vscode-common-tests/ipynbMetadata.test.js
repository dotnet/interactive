"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
const chai_1 = require("chai");
const ipynbUtilities_1 = require("../../src/vscode-common/ipynbUtilities");
describe('ipynb metadata tests', () => {
    describe('document metadata', () => {
        it(`document metadata can be read when present`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: 'csharp'
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata.name).to.equal('csharp');
        });
        it(`document metadata well-known values are transformed`, () => {
            const wellKnownLanguagePairs = [
                ['C#', 'csharp'],
                ['F#', 'fsharp'],
                ['PowerShell', 'pwsh']
            ];
            for (const languagePair of wellKnownLanguagePairs) {
                const languageName = languagePair[0];
                const expectedResult = languagePair[1];
                const documentMetadata = {
                    custom: {
                        metadata: {
                            language_info: {
                                name: languageName
                            }
                        }
                    }
                };
                const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
                (0, chai_1.expect)(languageInfoMetadata.name).to.equal(expectedResult);
            }
        });
        it(`document metadata non-special-cased language returns as self`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: 'see-sharp'
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata.name).to.equal('see-sharp');
        });
        it(`document metadata with undefined language also comes back as undefined`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: undefined
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata.name).to.equal(undefined);
        });
        it(`document metadata with non-string value comes back as undefined`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name: 42
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata.name).to.equal(undefined);
        });
        it(`document metadata is default when not the correct shape 1`, () => {
            const documentMetadata = {
                custom: {
                    metadata_but_not_the_correct_shape: {
                        language_info: {
                            name: 'csharp'
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata).to.deep.equal({
                name: undefined
            });
        });
        it(`document metadata is default when not the correct shape 2`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info_but_not_the_correct_shape: {
                            name: 'csharp'
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata).to.deep.equal({
                name: undefined
            });
        });
        it(`document metadata is default when not the correct shape 3`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        language_info: {
                            name_but_not_the_correct_shape: 'csharp'
                        }
                    }
                }
            };
            const languageInfoMetadata = (0, ipynbUtilities_1.getLanguageInfoMetadata)(documentMetadata);
            (0, chai_1.expect)(languageInfoMetadata).to.deep.equal({
                name: undefined
            });
        });
    });
    describe('kernelspec metadata', () => {
        it(`sets kernelspec data when document metadata is undefined`, () => {
            const documentMetadata = undefined;
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                        },
                        language_info: {
                            file_extension: '.cs',
                            mimetype: 'text/x-csharp',
                            name: 'C#',
                            pygments_lexer: 'csharp',
                            version: '9.0',
                        },
                    }
                }
            });
        });
        it(`sets kernelspec data when not present`, () => {
            const documentMetadata = {
                custom: {}
            };
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                        },
                        language_info: {
                            file_extension: '.cs',
                            mimetype: 'text/x-csharp',
                            name: 'C#',
                            pygments_lexer: 'csharp',
                            version: '9.0',
                        },
                    }
                }
            });
        });
        it(`merges kernelspec data with existing`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        kernelspec: {
                            some_existing_key: 'some existing value'
                        }
                    }
                }
            };
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                            some_existing_key: 'some existing value',
                        },
                        language_info: {
                            file_extension: '.cs',
                            mimetype: 'text/x-csharp',
                            name: 'C#',
                            pygments_lexer: 'csharp',
                            version: '9.0',
                        },
                    }
                }
            });
        });
        it(`preserves other metadata while setting kernelspec`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            };
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                        },
                        language_info: {
                            file_extension: '.cs',
                            mimetype: 'text/x-csharp',
                            name: 'C#',
                            pygments_lexer: 'csharp',
                            version: '9.0',
                        },
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            });
        });
        it(`preserves other metadata when kernelspec is already present`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        kernelspec: {
                            name: 'this will be overwritten',
                            some_existing_key: 'some existing value'
                        },
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            };
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                            some_existing_key: 'some existing value'
                        },
                        language_info: {
                            file_extension: '.cs',
                            mimetype: 'text/x-csharp',
                            name: 'C#',
                            pygments_lexer: 'csharp',
                            version: '9.0',
                        },
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            });
        });
        it(`preserves original kernelspec when it is already present and is a .net kernelspec`, () => {
            const documentMetadata = {
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: ".NET (PowerShell)",
                            language: "PowerShell",
                            name: ".net-powershell"
                        },
                        language_info: {
                            file_extension: ".ps1",
                            mimetype: "text/x-powershell",
                            name: "PowerShell",
                            pygments_lexer: "powershell",
                            version: "7.0"
                        },
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            };
            const newDocumentMetadata = (0, ipynbUtilities_1.withDotNetKernelMetadata)(documentMetadata);
            (0, chai_1.expect)(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: ".NET (PowerShell)",
                            language: "PowerShell",
                            name: ".net-powershell"
                        },
                        language_info: {
                            file_extension: ".ps1",
                            mimetype: "text/x-powershell",
                            name: "PowerShell",
                            pygments_lexer: "powershell",
                            version: "7.0"
                        },
                        some_custom_metadata: {
                            key1: 'value 1'
                        }
                    },
                    some_other_custom_data: {
                        key2: 'value 2'
                    }
                }
            });
        });
    });
    describe('cell metadata', () => {
        it(`cell metadata can be read when present`, () => {
            const cellMetadata = {
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language: 'see-sharp'
                        }
                    }
                }
            };
            const dotnetMetadata = (0, ipynbUtilities_1.getDotNetMetadata)(cellMetadata);
            (0, chai_1.expect)(dotnetMetadata.language).to.equal('see-sharp');
        });
        it(`cell metadata is empty when not the correct shape 1`, () => {
            const cellMetadata = {
                custom: {
                    metadata_but_not_the_correct_shape: {
                        dotnet_interactive: {
                            language: 'see-sharp'
                        }
                    }
                }
            };
            const dotnetMetadata = (0, ipynbUtilities_1.getDotNetMetadata)(cellMetadata);
            (0, chai_1.expect)(dotnetMetadata.language).to.equal(undefined);
        });
        it(`cell metadata is empty when not the correct shape 2`, () => {
            const cellMetadata = {
                custom: {
                    metadata: {
                        dotnet_interactive_but_not_the_correct_shape: {
                            language: 'see-sharp'
                        }
                    }
                }
            };
            const dotnetMetadata = (0, ipynbUtilities_1.getDotNetMetadata)(cellMetadata);
            (0, chai_1.expect)(dotnetMetadata.language).to.equal(undefined);
        });
        it(`cell metadata is empty when not the correct shape 3`, () => {
            const cellMetadata = {
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language_but_not_the_correct_shape: 'see-sharp'
                        }
                    }
                }
            };
            const dotnetMetadata = (0, ipynbUtilities_1.getDotNetMetadata)(cellMetadata);
            (0, chai_1.expect)(dotnetMetadata.language).to.equal(undefined);
        });
        it(`cell metadata returns empty info if shape isn't correct`, () => {
            const cellMetadata = {
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language: 42 // not a string
                        }
                    }
                }
            };
            const dotnetMetadata = (0, ipynbUtilities_1.getDotNetMetadata)(cellMetadata);
            (0, chai_1.expect)(dotnetMetadata.language).to.equal(undefined);
        });
        it(`cell metadata is added when not present`, () => {
            const existingCellMetadata = {};
            const updatedCellMetadata = (0, ipynbUtilities_1.withDotNetCellMetadata)(existingCellMetadata, 'pwsh');
            (0, chai_1.expect)(updatedCellMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language: 'pwsh'
                        }
                    }
                }
            });
        });
        it(`existing cell metadata is preserved when updated`, () => {
            const existingCellMetadata = {
                number: 42,
                custom: {
                    anotherNumber: 43,
                    metadata: {
                        stillAnotherNumber: 44,
                        dotnet_interactive: {
                            aReallyDeepNumber: 45,
                            language: 'not-pwsh'
                        }
                    }
                }
            };
            const updatedCellMetadata = (0, ipynbUtilities_1.withDotNetCellMetadata)(existingCellMetadata, 'pwsh');
            (0, chai_1.expect)(updatedCellMetadata).to.deep.equal({
                number: 42,
                custom: {
                    anotherNumber: 43,
                    metadata: {
                        stillAnotherNumber: 44,
                        dotnet_interactive: {
                            aReallyDeepNumber: 45,
                            language: 'pwsh'
                        }
                    }
                }
            });
        });
    });
    describe('cell language selection', () => {
        it(`sets the cell language first from cell text`, () => {
            const cellText = '#!javascript\r\nalert(1+1);';
            const cellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.javascript');
        });
        it(`does not use the cell text for the language if a non-language specifier is on the first line; cell metadata is used`, () => {
            const cellText = '#!about\r\n1+1';
            const cellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.pwsh');
        });
        it(`sets the cell language second from cell metadata if cell text doesn't specify a language`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.pwsh');
        });
        it(`sets the cell language third from document metadata if cell reported language is unsupported`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: undefined,
            };
            const documentMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'python';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.fsharp');
        });
        it(`cell metadata is undefined, cell reported language is used if it's a dotnet language`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: undefined,
            };
            const documentMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.csharp');
        });
        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is, but make it a dotnet-interactive langugae`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: undefined,
            };
            const documentMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.csharp');
        });
        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is; supported`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: undefined,
            };
            const documentMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('dotnet-interactive.csharp');
        });
        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is; unsupported`, () => {
            const cellText = '1+1';
            const cellMetadata = {
                language: undefined,
            };
            const documentMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'ruby';
            const cellLanguage = (0, ipynbUtilities_1.getCellLanguage)(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            (0, chai_1.expect)(cellLanguage).to.equal('ruby');
        });
    });
});
//# sourceMappingURL=ipynbMetadata.test.js.map