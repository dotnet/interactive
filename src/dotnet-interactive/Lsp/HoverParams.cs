// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.LanguageService;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class HoverParams
    {
        [JsonRequired]
        public TextDocument TextDocument { get; }

        [JsonRequired]
        public Position Position { get; }

        public HoverParams(TextDocument textDocument, Position position)
        {
            TextDocument = textDocument;
            Position = position;
        }

        public static HoverParams FromHoverCommand(Commands.RequestHoverText requestHoverText)
        {
            return new HoverParams(
                TextDocument.FromDocumentContents(requestHoverText.Code),
                Position.FromLinePosition(requestHoverText.Position));
        }

        public Commands.RequestHoverText ToCommand()
        {
            if (TextDocument.Uri.TryDecodeDocumentFromDataUri(out var code))
            {
                return new Commands.RequestHoverText(code, Position.ToLinePosition());
            }

            return null;
        }
    }
}
