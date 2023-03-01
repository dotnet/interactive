// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.CSharpProject.Transformations;

namespace Microsoft.DotNet.Interactive.CSharpProject;

internal static class DiagnosticsExtractor
{
    public static async Task<IReadOnlyCollection<SerializableDiagnostic>> ExtractSerializableDiagnosticsFromDocument(
        BufferId bufferId,
        Document selectedDocument,
        Workspace workspace)
    {
        var semanticModel = await selectedDocument.GetSemanticModelAsync();
        return ExtractSerializableDiagnosticsFromSemanticModel(bufferId, semanticModel, workspace);
    }

    public static IReadOnlyCollection<SerializableDiagnostic> ExtractSerializableDiagnosticsFromSemanticModel(
        BufferId bufferId,
        SemanticModel semanticModel,
        Workspace workspace)
    {
        var diagnostics = workspace.MapDiagnostics(bufferId, semanticModel.GetDiagnostics().ToArray());
        return diagnostics.DiagnosticsInActiveBuffer;
    }
}