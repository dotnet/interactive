// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public class PSqlConnectionTests : IDisposable
{
    private CompositeKernel CreateKernel()
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
        var csharpKernel = new CSharpKernel().UseNugetDirective().UseValueSharing();

        // TODO: remove SQLKernel it is used to test current patch
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

        result.Events
              .Should()
              .NotContainErrors();

        result = await kernel.SubmitCodeAsync("""
            #!sql-adventureworks
            SELECT * FROM Person.Person LIMIT 100;
            """);

        result.Events.Should()
              .NotContainErrors()
              .And
              .ContainSingle<DisplayedValueProduced>(e =>
                e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType));

        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>(e =>
                e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType));
    }

    public void Dispose()
    {
        DataExplorer.ResetToDefault();
        Formatter.ResetToDefault();
    }
}