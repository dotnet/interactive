// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.ML;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataFrameKernelExtension
    {
        public static Task LoadAsync(Kernel rootKernel)
        {
            RegisterFormatters();

            rootKernel.VisitSubkernelsAndSelf(kernel =>
            {
                if (kernel is CSharpKernel cSharpKernel)
                {
                    var showCodeParameter = new KernelDirectiveParameter("--show-code")
                    {
                        Description = "Display the C# code for the generated DataFrame types",
                        Flag = true
                    };

                    var variableNameParameter = new KernelDirectiveParameter("--variable-name")
                    {
                        Description = "The name of the variable to replace",
                        AllowImplicitName = true
                    };

                    variableNameParameter.AddCompletions(_ =>
                    {
                        var completionItems = cSharpKernel.ScriptState
                                                          .Variables
                                                          .Where(v => v.Value is DataFrame)
                                                          .Select(v => new CompletionItem(v.Name, WellKnownTags.Parameter));
                        return Task.FromResult(completionItems);
                    });

                    var directive = new KernelActionDirective("#!linqify")
                    {
                        Description = "Replaces the specified Microsoft.Data.Analysis.DataFrame with a derived type for LINQ access to the contained data"
                    };

                    directive.Parameters.Add(showCodeParameter);
                    directive.Parameters.Add(variableNameParameter);

                    cSharpKernel.AddDirective<Linqify>(directive, Linqify);

                    async Task Linqify(
                        Linqify command,
                        KernelInvocationContext context)
                    {
                        if (cSharpKernel.TryGetValue<DataFrame>(command.VariableName, out var dataFrame))
                        {
                            var code = BuildTypedDataFrameCode(
                                dataFrame,
                                command.VariableName);

                            if (command.ShowCode)
                            {
                                context.Display(code);
                            }

                            cSharpKernel.TryGetValue(command.VariableName, out DataFrame oldFrame);

                            await cSharpKernel.SendAsync(new SubmitCode(code));

                            cSharpKernel.TryGetValue(command.VariableName, out DataFrame newFrame);

                            foreach (var column in oldFrame.Columns)
                            {
                                newFrame.Columns.Add(column);
                            }
                        }
                    }
                }
            });

            KernelInvocationContext.Current?.Display(
                new HtmlString("""
                                <details><summary>Create strongly-typed dataframes using<code>#!linqify</code>.</summary>
                                    <p>The <code>#!linqify</code> magic command replaces a <a href="https://www.nuget.org/packages/Microsoft.Data.Analysis/"><code>Microsoft.Data.Analysis.DataFrame</code></a> variable with a generated, strongly-typed data frame, allowing the use of LINQ operations over the contained data.</p>
                                    </details>
                                """),
                "text/html");

            return Task.CompletedTask;
        }

        private static void RegisterFormatters()
        {
            Formatter.Register<IDataView>((dataView, writer) =>
            {
                var tabularData = dataView.ToTabularJsonString();
                writer.Write(tabularData.ToString());
            }, TabularDataResourceFormatter.MimeType);
        }

        public static string BuildTypedDataFrameCode(
            DataFrame sourceDataFrame,
            string variableName)
        {
            var sb = new StringBuilder();

            var frameTypeName = $"DataFrame_From_{variableName}";
            var frameRowTypeName = $"DataFrameRow_From_{variableName}";

            sb.Append($@"
public class {frameTypeName} : {typeof(DataFrame).FullName}, IEnumerable<{frameRowTypeName}>
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
    private readonly {typeof(DataFrameRow).FullName} _sourceRow;
    
    public {frameRowTypeName}({typeof(DataFrameRow).FullName} sourceRow)
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

        private static string MakeValidMethodName(string value)
        {
            value = Regex.Replace(value, @"^[0-9]", "_");
            value = Regex.Replace(value, @"([^a-zA-Z]+)", "_");
            return value;
        }
    }

    public class Linqify : KernelCommand
    {
        public bool ShowCode { get; set; }
        public string VariableName { get; set; }
    }
}

namespace Microsoft.ML
{

    public static class DataViewExtensions
    {
        private static T GetValue<T>(ValueGetter<T> valueGetter)
        {
            T value = default;
            valueGetter(ref value);
            return value;
        }

        public static TabularDataResource ToTabularDataResource(this IDataView source)
        {
            var fields = source.Schema.ToDictionary(column => column.Name, column => column.Type.RawType);
            var data = new List<IEnumerable<KeyValuePair<string, object>>>();

            var cursor = source.GetRowCursor(source.Schema);

            while (cursor.MoveNext())
            {
                var rowObj = new List<KeyValuePair< string, object>>();

                foreach (var column in source.Schema)
                {
                    var type = column.Type.RawType;
                    var getGetterMethod = cursor.GetType()
                        .GetMethod(nameof(cursor.GetGetter))
                        .MakeGenericMethod(type);

                    var valueGetter = getGetterMethod.Invoke(cursor, new object[] { column });

                    object value = GetValue((dynamic)valueGetter);

                    if (value is ReadOnlyMemory<char>)
                    {
                        value = value.ToString();
                    }

                    rowObj.Add( new KeyValuePair<string, object>( column.Name, value));
                }

                data.Add(rowObj);
            }

            var schema = new TableSchema();

            foreach (var (fieldName, fieldValue) in fields)
            {
                schema.Fields.Add(new TableSchemaFieldDescriptor(fieldName, fieldValue.ToTableSchemaFieldType()));
            }

            return new TabularDataResource(schema, data);
        }

        public static TabularDataResourceJsonString ToTabularJsonString(this IDataView source)
        {
            var tabularDataResource = source.ToTabularDataResource();
            return tabularDataResource.ToJsonString();
        }
    }
}