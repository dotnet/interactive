// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.ML;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataFrameKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel rootKernel)
        {
            RegisterFormatters();

            rootKernel.VisitSubkernelsAndSelf(kernel =>
            {
                if (kernel is CSharpKernel cSharpKernel)
                {
                    var command = new Command("#!linqify", "Replaces the specified Microsoft.Data.Analysis.DataFrame with a derived type for LINQ access to the contained data")
                    {
                        new Option<bool>("--show-code", "Display the C# code for the generated DataFrame types"),
                        new Argument<string>("variable-name", "The name of the variable to replace")
                            .AddSuggestions((_,match) => cSharpKernel.ScriptState
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
                                context.Display(code);
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

            KernelInvocationContext.Current?.Display(
                $@"Added the `#!linqify` magic command.",
                "text/markdown");

            return Task.CompletedTask;
        }

        private void RegisterFormatters()
        {
            Formatter.Register<IDataView>((dataView, writer) =>
            {
                var tabularData = dataView.ToTabularJsonString();
                writer.Write(tabularData.ToString());
            }, TabularDataFormatter.MimeType);
        }

        public string BuildTypedDataFrameCode(
            DataFrame sourceDataFrame,
            string variableName)
        {
            var sb = new StringBuilder();

            var frameTypeName = $"DataFrame_From_{variableName}";
            var frameRowTypeName = $"DataFrameRow_From_{variableName}";

            sb.Append($@"
public class {frameTypeName} : Microsoft.Data.Analysis.DataFrame, IEnumerable<{frameRowTypeName}>
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
    private readonly Microsoft.Data.Analysis.DataFrameRow _sourceRow;
    
    public {frameRowTypeName}(Microsoft.Data.Analysis.DataFrameRow sourceRow)
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
                                .Select((col, i) => $"    public {col.DataType.FullName} {MakeValidMethodName(col.Name)} => ({col.DataType.FullName}) _sourceRow[{i}];"));

            return sb.ToString();
        }

        private string MakeValidMethodName(string value)
        {
            value = Regex.Replace(value, @"^[0-9]", "_");
            value = Regex.Replace(value, @"([^a-zA-Z]+)", "_");
            return value;
        }
    }
}

namespace Microsoft.ML
{
    public static class DataViewExtensions
    {
        public static void Explore(this IDataView source)
        {
            KernelInvocationContext.Current.Display(
                source.ToTabularJsonString(),
                HtmlFormatter.MimeType);
        }

        private static T GetValue<T>(ValueGetter<T> valueGetter)
        {
            T value = default;
            valueGetter(ref value);
            return value;
        }

        public static TabularJsonString ToTabularJsonString(this IDataView source)
        {
            var fields = source.Schema.ToDictionary(column => column.Name, column => column.Type.RawType);
            var data = new JArray();

            var cursor = source.GetRowCursor(source.Schema);

            while (cursor.MoveNext())
            {
                var rowObj = new JObject();

                foreach (var column in source.Schema)
                {
                    var type = column.Type.RawType;
                    var getGetterMethod = cursor.GetType()
                                                .GetMethod(nameof(cursor.GetGetter))
                                                .MakeGenericMethod(type);

                    var valueGetter = getGetterMethod.Invoke(cursor, new object[] { column });

                    object value = GetValue((dynamic) valueGetter);

                    if (value is ReadOnlyMemory<char>)
                    {
                        value = value.ToString();
                    }

                    var fromObject = JToken.FromObject(value);

                    rowObj.Add(column.Name, fromObject);
                }

                data.Add(rowObj);
            }

            var tabularData = TabularJsonString.Create(fields, data);

            return tabularData;
        }
    }
}