// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Microsoft.DotNet.Interactive.CSharpProject.Transformations;
using Workspace = Microsoft.DotNet.Interactive.CSharpProject.Protocol.Workspace;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    internal static class DiagnosticsExtractor
    {
        public static async Task<IReadOnlyCollection<SerializableDiagnostic>> ExtractSerializableDiagnosticsFromDocument(
            BufferId bufferId,
            Budget budget,
            Document selectedDocument,
            Workspace workspace)
        {
            var semanticModel = await selectedDocument.GetSemanticModelAsync();
            return ExtractSerializableDiagnosticsFromSemanticModel(bufferId, budget, semanticModel, workspace);
        }

        public static IReadOnlyCollection<SerializableDiagnostic> ExtractSerializableDiagnosticsFromSemanticModel(
            BufferId bufferId,
            Budget budget,
            SemanticModel semanticModel,
            Workspace workspace)
        {
            var diagnostics = workspace.MapDiagnostics(bufferId, semanticModel.GetDiagnostics().ToArray(), budget);
            return diagnostics.DiagnosticsInActiveBuffer;
        }
    }
}