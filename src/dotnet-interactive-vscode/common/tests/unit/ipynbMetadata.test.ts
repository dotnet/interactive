// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { DotNetCellMetadata, getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, LanguageInfoMetadata, withDotNetKernelMetadata } from '../../ipynbUtilities';

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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata.name).to.equal('csharp');
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
                const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
                expect(languageInfoMetadata.name).to.equal(expectedResult);
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata.name).to.equal('see-sharp');
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata.name).to.equal(undefined);
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata.name).to.equal(undefined);
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata).to.deep.equal({
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata).to.deep.equal({
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
            const languageInfoMetadata = getLanguageInfoMetadata(documentMetadata);
            expect(languageInfoMetadata).to.deep.equal({
                name: undefined
            });
        });
    });

    describe('kernelspec metadata', () => {
        it(`sets kernelspec data when document metadata is undefined`, () => {
            const documentMetadata = undefined;
            const newDocumentMetadata = withDotNetKernelMetadata(documentMetadata);
            expect(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                        }
                    }
                }
            });
        });

        it(`sets kernelspec data when not present`, () => {
            const documentMetadata = {
                custom: {
                }
            };
            const newDocumentMetadata = withDotNetKernelMetadata(documentMetadata);
            expect(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                        }
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
            const newDocumentMetadata = withDotNetKernelMetadata(documentMetadata);
            expect(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
                            some_existing_key: 'some existing value',
                        }
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
            const newDocumentMetadata = withDotNetKernelMetadata(documentMetadata);
            expect(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
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
            const newDocumentMetadata = withDotNetKernelMetadata(documentMetadata);
            expect(newDocumentMetadata).to.deep.equal({
                custom: {
                    metadata: {
                        kernelspec: {
                            display_name: '.NET (C#)',
                            language: 'C#',
                            name: '.net-csharp',
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
            const dotnetMetadata = getDotNetMetadata(cellMetadata);
            expect(dotnetMetadata.language).to.equal('see-sharp');
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
            const dotnetMetadata = getDotNetMetadata(cellMetadata);
            expect(dotnetMetadata.language).to.equal(undefined);
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
            const dotnetMetadata = getDotNetMetadata(cellMetadata);
            expect(dotnetMetadata.language).to.equal(undefined);
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
            const dotnetMetadata = getDotNetMetadata(cellMetadata);
            expect(dotnetMetadata.language).to.equal(undefined);
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
            const dotnetMetadata = getDotNetMetadata(cellMetadata);
            expect(dotnetMetadata.language).to.equal(undefined);
        });
    });

    describe('cell language selection', () => {
        it(`sets the cell language first from cell text`, () => {
            const cellText = '#!javascript\r\nalert(1+1);';
            const cellMetadata: DotNetCellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.javascript');
        });

        it(`does not use the cell text for the language if a non-language specifier is on the first line; cell metadata is used`, () => {
            const cellText = '#!about\r\n1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.pwsh');
        });

        it(`sets the cell language second from cell metadata if cell text doesn't specify a language`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: 'pwsh',
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.pwsh');
        });

        it(`sets the cell language third from document metadata if cell reported language is unsupported`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: undefined,
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'python';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.fsharp');
        });

        it(`cell metadata is undefined, cell reported language is used if it's a dotnet language`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: undefined,
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: 'fsharp',
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.csharp');
        });

        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is, but make it a dotnet-interactive langugae`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: undefined,
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.csharp');
        });

        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is; supported`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: undefined,
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'dotnet-interactive.csharp';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('dotnet-interactive.csharp');
        });

        it(`cell metadata is undefined, document metadata is undefined, fall back to what the cell thinks it is; unsupported`, () => {
            const cellText = '1+1';
            const cellMetadata: DotNetCellMetadata = {
                language: undefined,
            };
            const documentMetadata: LanguageInfoMetadata = {
                name: undefined,
            };
            const cellReportedLanguage = 'ruby';
            const cellLanguage = getCellLanguage(cellText, cellMetadata, documentMetadata, cellReportedLanguage);
            expect(cellLanguage).to.equal('ruby');
        });
    });
});
