// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { DotNetCellMetadata, getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, LanguageInfoMetadata, withDotNetMetadata } from '../../ipynbUtilities';

describe('ipynb metadata tests', () => {

    //------------------------------------------------------- document metadata
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

    //----------------------------------------------------------- cell metadata
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

    it(`cell metadata can be set when empty`, () => {
        const existingMetadata = {
        };
        const dotnetMetadata = {
            language: 'see-sharp'
        };
        const newMetadata = withDotNetMetadata(existingMetadata, dotnetMetadata);
        expect(newMetadata).to.deep.equal({
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'see-sharp'
                    }
                }
            }
        });
    });

    it(`cell metadata can overwrite old values`, () => {
        const existingMetadata = {
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'eff-sharp'
                    }
                }
            }
        };
        const dotnetMetadata = {
            language: 'see-sharp'
        };
        const newMetadata = withDotNetMetadata(existingMetadata, dotnetMetadata);
        expect(newMetadata).to.deep.equal({
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'see-sharp'
                    }
                }
            }
        });
    });

    it(`cell metadata doesn't overwrite other values in dotnet_interactive namespace`, () => {
        const existingMetadata = {
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'eff-sharp',
                        version: 42
                    }
                }
            }
        };
        const dotnetMetadata = {
            language: 'see-sharp'
        };
        const newMetadata = withDotNetMetadata(existingMetadata, dotnetMetadata);
        expect(newMetadata).to.deep.equal({
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'see-sharp',
                        version: 42
                    }
                }
            }
        });
    });

    it(`cell metadata doesn't overwrite other values or namespaces`, () => {
        const existingMetadata = {
            custom: {
                metadata: {
                    not_dotnet_interactive: {
                        some_key: 'some_value'
                    }
                }
            }
        };
        const dotnetMetadata = {
            language: 'see-sharp'
        };
        const newMetadata = withDotNetMetadata(existingMetadata, dotnetMetadata);
        expect(newMetadata).to.deep.equal({
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'see-sharp'
                    },
                    not_dotnet_interactive: {
                        some_key: 'some_value'
                    }
                }
            }
        });
    });

    it(`cell metadata doesn't remove values in non-standard locations`, () => {
        const existingMetadata = {
            something_non_standard: {
                really_odd: 'not sure what this is'
            }
        };
        const dotnetMetadata = {
            language: 'see-sharp'
        };
        const newMetadata = withDotNetMetadata(existingMetadata, dotnetMetadata);
        expect(newMetadata).to.deep.equal({
            something_non_standard: {
                really_odd: 'not sure what this is'
            },
            custom: {
                metadata: {
                    dotnet_interactive: {
                        language: 'see-sharp'
                    }
                }
            }
        });
    });

    //------------------------------------------------- cell language selection
    it(`cell language is first determined from cell metadata`, () => {
        const cellMetadata: DotNetCellMetadata = {
            language: 'pwsh',
        };
        const documentMetadata: LanguageInfoMetadata = {
            name: 'fsharp',
        };
        const cellLanguage = getCellLanguage(cellMetadata, documentMetadata, 'csharp');
        expect(cellLanguage).to.equal('dotnet-interactive.pwsh');
    });

    it(`cell language is second determined from document metadata`, () => {
        const cellMetadata: DotNetCellMetadata = {
            language: undefined,
        };
        const documentMetadata: LanguageInfoMetadata = {
            name: 'fsharp',
        };
        const cellLanguage = getCellLanguage(cellMetadata, documentMetadata, 'csharp');
        expect(cellLanguage).to.equal('dotnet-interactive.fsharp');
    });

    it(`cell language is ultimately determined from the fallback value`, () => {
        const cellMetadata: DotNetCellMetadata = {
            language: undefined,
        };
        const documentMetadata: LanguageInfoMetadata = {
            name: undefined,
        };
        const cellLanguage = getCellLanguage(cellMetadata, documentMetadata, 'csharp');
        expect(cellLanguage).to.equal('dotnet-interactive.csharp');
    });
});
