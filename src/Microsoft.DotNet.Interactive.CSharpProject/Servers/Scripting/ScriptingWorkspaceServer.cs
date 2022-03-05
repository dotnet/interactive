// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.MLS.Project;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Pocket;
using Microsoft.DotNet.Interactive.CSharpProject.Transformations;
using Workspace = Microsoft.DotNet.Interactive.CSharpProject.Protocol.Workspace;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Recipes;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Scripting
{
    public class ScriptingWorkspaceServer : ICodeRunner
    {
        private readonly BufferInliningTransformer _transformer = new BufferInliningTransformer();
        private static readonly Regex _diagnosticFilter = new Regex(@"^(?<location>\(\d+,\d+\):)\s*(?<level>\S+)\s*(?<code>[A-Z]{2}\d+:)(?<message>.+)", RegexOptions.Compiled);

        public ScriptingWorkspaceServer()
        {
        }

        public async Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            var workspace = request.Workspace;
            budget = budget ?? new Budget();

            using (var console = await ConsoleOutput.Capture())
            {
                workspace = await _transformer.TransformAsync(workspace);

                if (workspace.Files.Length != 1)
                {
                    throw new ArgumentException($"{nameof(workspace)} should have exactly one source file.");
                }

                var options = CreateOptions(workspace);

                ScriptState<object> state = null;
                Exception userException = null;

                var buffer = new StringBuilder(workspace.GetSourceFiles().Single().Text.ToString());

                try
                {
                    state = await Run(buffer, options, budget);

                    if (state != null &&
                        console.IsEmpty())
                    {
                        state = await EmulateConsoleMainInvocation(state, buffer, options, budget);
                    }

                    budget.RecordEntry(UserCodeCompletedBudgetEntryName);
                }
                catch (Exception ex)
                {
                    userException = ex;
                }

                budget.RecordEntryAndThrowIfBudgetExceeded();

                var diagnostics = await ExtractDiagnostics(
                                      workspace,
                                      request.ActiveBufferId,
                                      options);

                var output =
                    console.StandardOutput == ""
                        ? Array.Empty<string>()
                        : console.StandardOutput
                                 .Replace("\r\n", "\n")
                                 .Split(new[] { '\n' });

                output = ProcessOutputLines(output,
                                            diagnostics.DiagnosticsInActiveBuffer.GetCompileErrorMessages());

                var result = new RunResult(
                    succeeded: !userException.IsConsideredRunFailure(),
                    output: output,
                    exception: (userException ?? state?.Exception).ToDisplayString(),
                    diagnostics: diagnostics.DiagnosticsInActiveBuffer,
                    requestId: request.RequestId);

                return result;
            }
        }

        private string[] ProcessOutputLines(string[] output, string[] errormessages)
        {
            output = output.Where(IsNotDiagnostic).ToArray();

            if (errormessages.All(string.IsNullOrWhiteSpace))
            {
                return output;
            }

            return output.Concat(errormessages).ToArray();
        }

        private bool IsNotDiagnostic(string line) => !_diagnosticFilter.IsMatch(line);

        private static ScriptOptions CreateOptions(Workspace request) =>
            ScriptOptions.Default
                         .AddReferences(GetReferenceAssemblies())
                         .AddImports(WorkspaceUtilities.DefaultUsings.Concat(request.Usings));

        private async Task<(IReadOnlyCollection<SerializableDiagnostic> DiagnosticsInActiveBuffer, IReadOnlyCollection<SerializableDiagnostic> AllDiagnostics)> ExtractDiagnostics(
            Workspace workspace,
            BufferId activeBufferId,
            ScriptOptions options)
        {
            workspace = await _transformer.TransformAsync(workspace);
            var sourceFile = workspace.GetSourceFiles().Single();
            var code = sourceFile.Text.ToString();
            var compilation = CSharpScript.Create(code, options).GetCompilation();
            return workspace.MapDiagnostics(
                activeBufferId,
                compilation.GetDiagnostics());
        }

        private static Task<ScriptState<object>> Run(
            StringBuilder buffer,
            ScriptOptions options,
            Budget budget) =>
            Task.Run(() =>
                         CSharpScript.RunAsync(
                             buffer.ToString(),
                             options))
                .CancelIfExceeds(budget, () => null);

        private static Assembly[] GetReferenceAssemblies() =>
            new[]
            {
                typeof(object).GetTypeInfo().Assembly,
                typeof(Enumerable).GetTypeInfo().Assembly,
                typeof(Console).GetTypeInfo().Assembly
            };

        private static async Task<ScriptState<object>> EmulateConsoleMainInvocation(
            ScriptState<object> state,
            StringBuilder buffer,
            ScriptOptions options,
            Budget budget)
        {
            var script = state.Script;
            var compiled = script.Compile();

            if (compiled.FirstOrDefault(d => d.Descriptor.Id == "CS7022") != null &&
                EntryPointType() is IMethodSymbol entryPointMethod)
            {
                // e.g. warning CS7022: The entry point of the program is global script code; ignoring 'Program.Main()' entry point.

                // add a line of code to call Main using reflection
                buffer.AppendLine(
                    $@"
typeof({entryPointMethod.ContainingType.Name})
    .GetMethod(""Main"",
               System.Reflection.BindingFlags.Static |
               System.Reflection.BindingFlags.NonPublic |
               System.Reflection.BindingFlags.Public)
    .Invoke(null, {ParametersForMain()});");

                state = await Run(buffer, options, budget);
            }

            return state;

            IMethodSymbol EntryPointType() =>
                EntryPointFinder.FindEntryPoint(
                    script.GetCompilation().GlobalNamespace);

            string ParametersForMain() => entryPointMethod.Parameters.Any()
                                              ? "new object[]{ new string[0] }"
                                              : "null";
        }

        public static string UserCodeCompletedBudgetEntryName = "UserCodeCompleted";
    }
}
