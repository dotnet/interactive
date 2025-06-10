// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive;

public partial class Kernel
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<Type>> _implementedCommandHandlerTypes = new();

    public static Kernel Current => KernelInvocationContext.Current.HandlingKernel;

    public static Kernel Root => KernelInvocationContext.Current.HandlingKernel?.RootKernel;

    public static DisplayedValue display(
        object value,
        params string[] mimeTypes)
    {
        return value.Display(mimeTypes);
    }

    public static IHtmlContent HTML(string content) => content.ToHtmlContent();

    /// <summary>
    /// Gets input from the user.
    /// </summary>
    /// <param name="prompt">The prompt to show.</param>
    /// <param name="typeHint">The type hint for the input, for example text or password.</param>
    /// <returns>The user input value.</returns>
    public static async Task<string> GetInputAsync(
        string prompt = "",
        string typeHint = "text")
    {
        return await GetInputAsync(prompt, false, typeHint);
    }

    public static async Task<IDictionary<string, string>> GetInputsAsync(
        IEnumerable<InputDescription> inputDescriptions)
    {
        if (inputDescriptions is null || inputDescriptions.Count() is 0)
        {
            throw new ArgumentException("You must provide at least one input description.");
        }

        var command = new RequestInputs();

        foreach (var inputDescription in inputDescriptions)
        {
            command.Inputs.Add(inputDescription);
        }

        var result = await Root.SendAsync(command, CancellationToken.None);

        if (result.Events.OfType<InputsProduced>().SingleOrDefault() is { } inputsProduced)
        {
            return inputsProduced.Values;
        }

        throw new OperationCanceledException("Input request canceled.");
    }

    public static async Task<PasswordString> GetPasswordAsync(string prompt = "")
    {
        var password = await GetInputAsync(prompt, true);
        return new PasswordString(password);
    }

    private static async Task<string> GetInputAsync(
        string prompt,
        bool isPassword,
        string typeHint = "text")
    {
        var command = new RequestInput(
            prompt,
            inputTypeHint: isPassword ? "password" : typeHint);

        var result = await Root.SendAsync(command, CancellationToken.None);

        if (result.Events.Last() is CommandFailed failedEvent)
        {
            throw new Exception(failedEvent.Message);
        }

        var inputProduced = result.Events
            .OfType<InputProduced>()
            .FirstOrDefault();

        return inputProduced?.Value;
    }

    public static void CSS(string content) =>
        // From https://stackoverflow.com/questions/524696/how-to-create-a-style-tag-with-javascript
        Javascript($$"""
                     var css = `{{content}}`,
                     head = document.head || document.getElementsByTagName('head')[0],
                     style = document.createElement('style');
         
                     head.appendChild(style);
         
                     style.type = 'text/css';
                     if (style.styleSheet) {
                       style.styleSheet.cssText = css;
                     } else {
                       style.appendChild(document.createTextNode(css));
                     }
                     """);

    public static void Javascript(string scriptContent)
    {
        PocketView value =
            script[type: "text/javascript"](
                HTML(
                    scriptContent));

        var formatted = new FormattedValue(
            HtmlFormatter.MimeType,
            value.ToString());

        var context = KernelInvocationContext.Current;

        var kernel = context.HandlingKernel;

        Task.Run(async () =>
        {
            var displayValue = new DisplayValue(formatted);
            displayValue.SetParent(context.Command, true);
            await kernel.SendAsync(displayValue);
        }).Wait(context.CancellationToken);
    }

    private static IReadOnlyCollection<Type> GetImplementedCommandHandlerTypesFor(Type kernelType) =>
        _implementedCommandHandlerTypes.GetOrAdd(
            kernelType,
            t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKernelCommandHandler<>))
                .SelectMany(i => i.GenericTypeArguments)
                .ToArray());

    private static void OnDisplayedValue(DisplayedValue displayedValue)
    {
        if (KernelInvocationContext.Current is { } context)
        {
            context.Display(displayedValue);
        }
        else
        {
            var formattedValue = displayedValue.FormattedValues.FirstOrDefault(v => v.MimeType == PlainTextFormatter.MimeType) ??
                                 displayedValue.FormattedValues.FirstOrDefault();

            if (formattedValue?.Value is { } value)
            {
                Console.WriteLine(value);
            }
        }
    }

    protected delegate Task SetValueAsyncDelegate(
        string name,
        object value,
        Type declaredType = null);
}