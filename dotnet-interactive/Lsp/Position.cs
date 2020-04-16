// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public struct Position
    {
        /// <summary>
        /// Line position in a document (zero-based).
        /// </summary>
        [JsonRequired]
        public int Line { get; set; }

        /// <summary>
        /// Character offset on a line in a document (zero-based).
        /// </summary>
        [JsonRequired]
        public int Character { get; set; }

        public Position(int line, int character)
        {
            Line = line;
            Character = character;
        }

        public static Position FromLanguageServicePosition(LanguageService.Position position)
        {
            return new Position(position.Line, position.Character);
        }

        public LanguageService.Position ToLanguageServicePosition()
        {
            return new LanguageService.Position(Line, Character);
        }
    }
}
