// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

public class HtmlNotebookFrontendEnvironment : BrowserFrontendEnvironment
{
    private readonly TimeSpan _getApiUriTimeout;
    private readonly TaskCompletionSource<Uri> _completionSource;
    private readonly Dictionary<string, (KernelInvocationContext Context, TaskCompletionSource CompletionSource)> _tokenToInvocationContext;
       
    public HtmlNotebookFrontendEnvironment(TimeSpan? apiUriTimeout = null)
    {
        RequiresAutomaticBootstrapping = true;
        _completionSource = new TaskCompletionSource<Uri>();
        _tokenToInvocationContext = new Dictionary<string, (KernelInvocationContext, TaskCompletionSource)>();
        _getApiUriTimeout = apiUriTimeout ?? TimeSpan.FromSeconds(30);
    }

    public HtmlNotebookFrontendEnvironment(Uri apiUri) : this()
    {
        SetApiUri(apiUri);
    }

    public bool RequiresAutomaticBootstrapping { get; set; }

    public void SetApiUri(Uri apiUri)
    {
        _completionSource.TrySetResult(apiUri);
    }

    public Task<Uri> GetApiUriAsync()
    {
        return _completionSource.Task;
    }

    public override async Task ExecuteClientScript(string code, KernelInvocationContext context)
    {
        var commandToken = context.Command.GetOrCreateToken();
        var apiUriTask = GetApiUriAsync();
        var completedTask = await Task.WhenAny(apiUriTask, Task.Delay(_getApiUriTimeout));
        if (completedTask != apiUriTask)
        {
            throw new TimeoutException("Timeout resolving the kernel's HTTP endpoint. Please try again.");
        }

        var apiUri = apiUriTask.Result;
        var codePrelude = $@"
if (typeof window.createDotnetInteractiveClient === typeof Function) {{
    window.createDotnetInteractiveClient('{apiUri.AbsoluteUri}').then(async function (interactive) {{
        const console = interactive.getConsole('{commandToken}');
        const notebookScope = getDotnetInteractiveScope('{apiUri.AbsoluteUri}');
        try {{

await Object.getPrototypeOf(async function() {{}}).constructor(
    ""interactive"",
    ""console"",
    ""notebookScope"",
    """.Replace("\r\n", "\n");
        var codePostlude = $@"""
)(
    interactive,
    console,
    notebookScope
);

        }} catch (err) {{
            interactive.failCommand(err, '{commandToken}');
        }} finally {{
            await interactive.waitForAllEventsToPublish('{commandToken}');
            await interactive.markExecutionComplete('{commandToken}');
        }}
    }});
}}
".Replace("\r\n", "\n");
        var wrappedCode = $"{codePrelude}{HttpUtility.JavaScriptStringEncode(code)}{codePostlude}";
        var executionCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _tokenToInvocationContext[commandToken] = (context, executionCompletionSource);
        IHtmlContent content = PocketViewTags.script[type: "text/javascript"](wrappedCode.ToHtmlContent());
        var formattedValues = FormattedValue.CreateManyFromObject(content);
        context.Publish(new DisplayedValueProduced(content, context.Command, formattedValues));
        await executionCompletionSource.Task;
    }

    public void PublishEventFromCommand(string commandToken, Func<KernelCommand, KernelEvent> eventEnvelopeCreator)
    {
        if (_tokenToInvocationContext.TryGetValue(commandToken, out var item))
        {
            var kernelCommand = item.Context.Command;
            var kernelEvent = eventEnvelopeCreator.Invoke(kernelCommand);
            item.Context.Publish(kernelEvent);
        }
    }

    public void MarkExecutionComplete(string token)
    {
        if (_tokenToInvocationContext.TryGetValue(token, out var item))
        {
            item.CompletionSource.SetResult();
        }

        _tokenToInvocationContext.Remove(token);
    }
}