// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataFrameTypeGeneratorExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel rootKernel)
        {
            rootKernel.VisitSubkernelsAndSelf(kernel =>
            {
                if (kernel is CSharpKernel cSharpKernel)
                {
                    var command = new Command("#!linqify", "Replaces the specified Microsoft.Data.Analysis.DataFrame with a derived type for LINQ access to the contained data")
                    {
                        new Option<bool>("--show-code", "Display the C# code for the generated DataFrame types"),
                        new Argument<string>("variable-name", "The name of the variable to replace")
                            .AddSuggestions(match => cSharpKernel.ScriptState
                                                                 .Variables
                                                                 .Where(v => v.Value is DataFrame)
                                                                 .Select(v => v.Name))
                    };
                            
                    cSharpKernel.AddDirective(command);

                    command.Handler = CommandHandler.Create<string, bool, KernelInvocationContext>(CompileStuff);

                    async Task CompileStuff(
                        string variableName,
                        bool showCode,
                        KernelInvocationContext context)
                    {
                        if (cSharpKernel.TryGetVariable<DataFrame>(variableName, out var dataFrame))
                        {
                            var code = BuildTypedDataFrameCode(
                                dataFrame,
                                variableName);

                            if (showCode)
                            {
                                await context.DisplayAsync(code);
                            }

                            cSharpKernel.TryGetVariable(variableName, out DataFrame oldFrame);

                            await cSharpKernel.SendAsync(new SubmitCode(code));

                            cSharpKernel.TryGetVariable(variableName, out DataFrame newFrame);

                            foreach (var column in oldFrame.Columns)
                            {
                                newFrame.Columns.Add(column);
                            }
                        }
                    }
                }
            });

            return Task.CompletedTask;
        }

        public string BuildTypedDataFrameCode(
            DataFrame sourceDataFrame,
            string variableName)
        {
            var sb = new StringBuilder();

            var frameTypeName = $"DataFrame_From_{variableName}";
            var frameRowTypeName = $"DataFrameRow_From_{variableName}";

            sb.Append($@"
public class {frameTypeName} : DataFrame, IEnumerable<{frameRowTypeName}>
{{
    public {frameTypeName}()
    {{
    }}

    public IEnumerator<{frameRowTypeName}> GetEnumerator() =>
        Rows.Select(row => new {frameRowTypeName}(row)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}}

public class {frameRowTypeName}
{{
    private readonly DataFrameRow _sourceRow;
    
    public {frameRowTypeName}(DataFrameRow sourceRow)
    {{
        _sourceRow = sourceRow;
    }}

{RowProperties()}

}}

var {variableName} = new {frameTypeName}();
");

            string RowProperties() =>
                string.Join(Environment.NewLine,
                            sourceDataFrame
                                .Columns
                                .Select((col, i) => $"    public {col.DataType.FullName} {col.Name} => ({col.DataType.FullName}) _sourceRow[{i}];"));

            return sb.ToString();
        }
    }
}