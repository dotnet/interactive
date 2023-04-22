// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.FSharp.ScriptHelpers;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.FSharp.Core;

using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public class AsyncContextTests
{
    [Fact]
    public async Task async_context_is_available_in_csharp_scripting2()
    {
        AsyncContext.TryEstablish(out var contextId);

        var result = await CSharpScript.RunAsync(@$"
#r ""{ typeof(AsyncContext).Assembly.Location}""
using {typeof(AsyncContext).Namespace};
AsyncContext.Id
");
        result.Exception.Should().BeNull();
        result.ReturnValue.Should().Be(contextId);
    }

    [Fact]
    public void async_context_is_available_in_fsharp_scripting2()
    {
        AsyncContext.TryEstablish(out var contextId);

        var fSharpScript = new FSharpScript(
            new FSharpOption<string[]>(new[] { "/langversion:preview", "/usesdkrefs-" }),
            new FSharpOption<bool>(true),
            new FSharpOption<LangVersion>(LangVersion.Preview));

        var result = fSharpScript.Eval(@$"
#r """"""{ typeof(AsyncContext).Assembly.Location}""""""
open {typeof(AsyncContext).Namespace}
AsyncContext.Id
",
            new FSharpOption<CancellationToken>(CancellationToken.None));


        var ex = result.Item2;
        ex.Should().BeEmpty();
        var res = result.Item1.ResultValue.Value.ReflectionValue;
        res.Should().Be(contextId);
    }
}