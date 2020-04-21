// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                new TextDocument(requestHoverText.DocumentIdentifier),
                Position.FromLinePosition(requestHoverText.Position));
        }

        public Commands.RequestHoverText ToCommand()
        {
            return new Commands.RequestHoverText(
                TextDocument.Uri,
                Position.ToLinePosition());
        }
    }
}
