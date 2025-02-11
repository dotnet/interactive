// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.PowerShell.Tests;

public class SecretManagerTests
{
    [Theory]
    [InlineData("SECRET_NAME", "s33kr1t!!!")]
    [InlineData("SECRET NAME with a space", "s33kr1t!!!")]
    [InlineData("SecretJson",
                """
                {
                    "what": "how about some JSON?",
                    "howMuch": 1
                }
                """)]
    public void It_can_register_and_retrieve_a_secret(string secretName, string value)
    {
        using var kernel = new PowerShellKernel();

        var secretManager = new SecretManager(kernel);

        // make the secret name unique across runs
        secretName += DateTime.UtcNow.Ticks;

        secretManager.SetValue(name: secretName, value: value);

        var success = secretManager.TryGetValue(secretName, out var secret);

        using var _ = new AssertionScope();

        success.Should().BeTrue();
        secret.Should().Be(value);
    }

    [Fact]
    public async Task Temporary_variables_are_cleaned_up_after_retrieving_secrets()
    {
        using var kernel = new PowerShellKernel();

        var secretManager = new SecretManager(kernel);

        var secretName = nameof(Temporary_variables_are_cleaned_up_after_retrieving_secrets) + DateTime.UtcNow.Ticks;
        var value = "s33kr1t!!!";

        secretManager.SetValue(name: secretName, value: value);

        var valueInfosResultBefore = await kernel.SendAsync(new RequestValueInfos());

        secretManager.TryGetValue(secretName, out _);

        var valueInfosResultAfter = await kernel.SendAsync(new RequestValueInfos());

        var variableNamesAfter = valueInfosResultAfter.Events
                                                      .OfType<ValueInfosProduced>()
                                                      .Single()
                                                      .ValueInfos
                                                      .Select(i => i.Name);
        var variableNamesBefore = valueInfosResultBefore.Events
                                                        .OfType<ValueInfosProduced>()
                                                        .Single()
                                                        .ValueInfos
                                                        .Select(i => i.Name);
        variableNamesAfter.Should().BeEquivalentTo(variableNamesBefore);
    }
}