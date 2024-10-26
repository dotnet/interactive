// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.App;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public class PSqlConnectionTests : IDisposable
{
    private static CompositeKernel CreateKernel()
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
        var csharpKernel = new CSharpKernel().UseNugetDirective().UseValueSharing();

        var kernel = new CompositeKernel
        {
            new SqlDiscoverabilityKernel(),
            csharpKernel,
            new KeyValueStoreKernel()
        };

        kernel.DefaultKernelName = csharpKernel.Name;

        PSqlKernelExtension.Load(kernel);

        return kernel;
    }


    [PSqlFact]
    public async Task It_can_connect_and_query_data()
    {
        var connectionString = PSqlFactAttribute.GetConnectionStringForTests();
        using var kernel = CreateKernel();
        var connect = $"#!connect psql --kernel-name adventureworks \"{connectionString}\"";
        var result = await kernel.SubmitCodeAsync(connect);
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync("""
            #!sql-adventureworks
            SELECT * FROM Person.Person LIMIT 100;
            """);

        result.Events.Should().NotContainErrors();
        result.Events.Should()
            .ContainSingle<DisplayedValueProduced>(fvp => fvp.Value is DataExplorer<TabularDataResource>)
            .Which
            .FormattedValues.Select(fv => fv.MimeType)
            .Should()
            .BeEquivalentTo(HtmlFormatter.MimeType, CsvFormatter.MimeType);
    }

    [PSqlFact]
    public async Task It_returns_error_if_query_is_not_valid()
    {
        var connectionString = PSqlFactAttribute.GetConnectionStringForTests();
        using var kernel = CreateKernel();
        var connect = $"#!connect psql --kernel-name adventureworks \"{connectionString}\"";
        var result = await kernel.SubmitCodeAsync(connect);
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync("""
            #!sql-adventureworks
            SELECT not_known_column FROM Person.Person LIMIT 100;
            """);

        result.Events.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Contain("column \"not_known_column\" does not exist");
    }

    public void Dispose()
    {
        DataExplorer.ResetToDefault();
        Formatter.ResetToDefault();
    }
}