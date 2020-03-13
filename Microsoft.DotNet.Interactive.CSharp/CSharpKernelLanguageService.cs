// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.LanguageService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public partial class CSharpKernel
    {
        private readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        });

        public override Task<LspResponse> LspMethod(string methodName, JObject request)
        {
            LspResponse result;
            switch (methodName)
            {
                case "textDocument/hover":
                    // https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
                    var hoverParams = request.ToObject<HoverParams>(_jsonSerializer);
                    result = TextDocumentHover(hoverParams);
                    break;
                default:
                    result = null;
                    break;
            }

            return Task.FromResult(result);
        }

        public TextDocumentHoverResponse TextDocumentHover(HoverParams hoverParams)
        {
            return new TextDocumentHoverResponse()
            {
                Contents = new MarkupContent()
                {
                    Kind = MarkupKind.Markdown,
                    Value = $"textDocument/hover at position ({hoverParams.Position.Line}, {hoverParams.Position.Character}) with `markdown`",
                },
            };
        }
    }
}
