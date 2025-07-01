// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class FrontendEnvironmentHandlingTests
{
    private class TestCommand : KernelCommand
    {
    }

    private (KernelInvocationContext, HtmlNotebookFrontendEnvironment) CreateTestHtmlFrontendAndContext()
    {
        var testCommand = new TestCommand();
        testCommand.SetToken("token-abcd");
        var context = KernelInvocationContext.GetOrCreateAmbientContext(testCommand);
        var httpFrontend = new HtmlNotebookFrontendEnvironment(new Uri("http://12.12.12.12:4242"));
        return (context, httpFrontend);
    }

    [Fact]
    public void HttpNotebookFrontendEnvironment_wraps_submitted_js_content()
    {
        var (context, httpFrontend) = CreateTestHtmlFrontendAndContext();
        var events = context.KernelEvents.ToSubscribedList();
        _ = httpFrontend.ExecuteClientScript("console.log('test');", context);
        events
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeEquivalentToPreferringRuntimeMemberTypes(new FormattedValue("text/html", @"<script type=""text/javascript"">
if (typeof window.createDotnetInteractiveClient === typeof Function) {
    window.createDotnetInteractiveClient('http://12.12.12.12:4242/').then(async function (interactive) {
        const console = interactive.getConsole('token-abcd');
        const notebookScope = getDotnetInteractiveScope('http://12.12.12.12:4242/');
        try {

await Object.getPrototypeOf(async function() {}).constructor(
    ""interactive"",
    ""console"",
    ""notebookScope"",
    ""console.log(\u0027test\u0027);""
)(
    interactive,
    console,
    notebookScope
);

        } catch (err) {
            interactive.failCommand(err, 'token-abcd');
        } finally {
            await interactive.waitForAllEventsToPublish('token-abcd');
            await interactive.markExecutionComplete('token-abcd');
        }
    });
}
</script>".EnforceNewLine()));
    }

    [Fact]
    public async Task HttpNotebookFrontendEnvironment_does_not_complete_if_uri_is_not_specified()
    {
        var testCommand = new TestCommand();
        testCommand.SetToken("token-abcd");
        var context = KernelInvocationContext.GetOrCreateAmbientContext(testCommand);
        var httpFrontend = new HtmlNotebookFrontendEnvironment(TimeSpan.FromSeconds(2));

        Func<Task> executeTask = () => httpFrontend.ExecuteClientScript("console.log('test');", context);

        await executeTask
            .Should()
            .ThrowAsync<TimeoutException>();
    }

    [Fact]
    public void HttpNotebookFrontendEnvironment_can_complete_if_the_uri_is_supplied_after_execution_is_invoked()
    {
        var testCommand = new TestCommand();
        testCommand.SetToken("token-abcd");
        var context = KernelInvocationContext.GetOrCreateAmbientContext(testCommand);
        var events = context.KernelEvents.ToSubscribedList();
        var httpFrontend = new HtmlNotebookFrontendEnvironment(TimeSpan.FromSeconds(2));

        _ = httpFrontend.ExecuteClientScript("console.log('test');", context);

        httpFrontend.SetApiUri(new Uri("http://test-uri"));

        events
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle()
            .Which
            .Value
            .Should()
            .ContainAll(
                "http://test-uri",
                @"console.log(\u0027test\u0027);"
            );
    }

    [Fact]
    public void HttpNotebookFrontendEnvironment_can_publish_events_with_the_appropriate_token()
    {
        var (context, httpFrontend) = CreateTestHtmlFrontendAndContext();
        var events = context.KernelEvents.ToSubscribedList();
        _ = httpFrontend.ExecuteClientScript("console.log('test');", context);
        httpFrontend.PublishEventFromCommand(context.Command.GetOrCreateToken(), command => new ReturnValueProduced("content", command, new[] { new FormattedValue("text/plain", "content") }));
        events
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Command
            .GetOrCreateToken()
            .Should()
            .Be(context.Command.GetOrCreateToken());
    }

    [Fact]
    public void HttpNotebookFrontendEnvironment_returns_the_ReferenceEquals_command_based_on_the_token()
    {
        var (context, httpFrontend) = CreateTestHtmlFrontendAndContext();
        var events = context.KernelEvents.ToSubscribedList();
        _ = httpFrontend.ExecuteClientScript("console.log('test');", context);
        KernelCommand suppliedCommand = default;
        httpFrontend.PublishEventFromCommand(context.Command.GetOrCreateToken(), command =>
        {
            suppliedCommand = command;
            return new ReturnValueProduced("content", command, new[] { new FormattedValue("text/plain", "content") });
        });
        suppliedCommand
            .Should()
            .BeSameAs(context.Command);
    }

    [Fact]
    public async Task HttpNotebookFrontendEnvironment_completes_after_MarkExecutionComplete_has_been_called()
    {
        var (context, httpFrontend) = CreateTestHtmlFrontendAndContext();
        var executeTask = httpFrontend.ExecuteClientScript("console.log('test');", context);
        await Task.Delay(100); // yield to give the execution task an opportunity to schedule

        executeTask
            .IsCompleted
            .Should()
            .BeFalse();

        httpFrontend.MarkExecutionComplete(context.Command.GetOrCreateToken());
        await executeTask;
    }
}