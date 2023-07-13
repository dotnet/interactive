// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal enum HttpTokenKind
{
    /*
                              GET https://example.com/{something}
    Word                      --- -----   ------- ---  ---------
    Whitespace                   -
    Punctuation                        ---       -   --         -
    NewLine                                                      _
    */
    Word,
    Whitespace,
    Punctuation,
    NewLine,
    Missing
}