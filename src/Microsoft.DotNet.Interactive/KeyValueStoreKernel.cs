// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.ValueSharing;
using static Microsoft.DotNet.Interactive.ChooseKeyValueStoreKernelDirective;

namespace Microsoft.DotNet.Interactive
{
    public class KeyValueStoreKernel :
        Kernel,
        ISupportGetValue,
        ISupportSetClrValue,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "value";

        private readonly ConcurrentDictionary<string, object> _values = new();
        private ChooseKeyValueStoreKernelDirective _chooseKernelDirective;
        private (bool hadValue, object previousValue, object newValue)? _lastOperation;

        public KeyValueStoreKernel(string name = DefaultKernelName) : base(name)
        {
        }

        public Task SetValueAsync(string name, object value, Type declaredType = null)
        {
            _values[name] = value;
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<KernelValueInfo> GetValueInfos() =>
            _values.Select(e => new KernelValueInfo(e.Key, typeof(string))).ToArray();

        public bool TryGetValue<T>(string name, out T value)
        {
            if (_values.TryGetValue(name, out var obj) &&
                obj is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        // todo: change to ChooseKeyValueStoreKernelDirective after removing NetStandardc2.0 dependency
        public override ChooseKernelDirective ChooseKernelDirective =>
            _chooseKernelDirective ??= new(this);

        public async Task HandleAsync(
            SubmitCode command,
            KernelInvocationContext context)
        {
            var parseResult = command.KernelChooserParseResult;

            var value = command.LanguageNode.Text.Trim();

            var options = ValueDirectiveOptions.Create(parseResult, _chooseKernelDirective);

            await StoreValueAsync(command, context, options, value);
        }

        internal override bool AcceptsUnknownDirectives => true;

        internal async Task TryStoreValueFromOptionsAsync(
            KernelInvocationContext context,
            ValueDirectiveOptions options)
        {
            string newValue = null;
            var loadedFromOptions = false;

            if (options.FromFile is { } file)
            {
                newValue = File.ReadAllText(file.FullName);
                loadedFromOptions = true;
            }
            else if (options.FromUrl is { } uri)
            {
                var client = new HttpClient();
                var response = await client.GetAsync(uri, context.CancellationToken);
                newValue = await response.Content.ReadAsStringAsync();
                loadedFromOptions = true;
            }
            else if (options.FromValue is { } value)
            {
                newValue = value;
                loadedFromOptions = true;
            }

            if (loadedFromOptions)
            {
                var hadValue = TryGetValue(options.Name, out object previousValue);

                _lastOperation = (hadValue, previousValue, newValue);

                await StoreValueAsync(newValue, options, context);
            }
            else
            {
                _lastOperation = null;
            }
        }

        private async Task StoreValueAsync(
            KernelCommand command, 
            KernelInvocationContext context, 
            ValueDirectiveOptions options,
            string value = null)
        {
            if (options.FromFile is { })
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    UndoSetValue();
                    context.Fail(command,
                                 message: "The --from-file option cannot be used in combination with a content submission.");
                }
                
            }
            else if (options.FromUrl is { })
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    UndoSetValue();
                    context.Fail(command,
                                 message: "The --from-url option cannot be used in combination with a content submission.");
                }
            }
            else
            {
                await StoreValueAsync(value, options, context);
            }

            _lastOperation = default;

            void UndoSetValue()
            {
                if (_lastOperation is {})
                {
                    if (_lastOperation?.hadValue == true)
                    {
                        // restore value
                        _values[options.Name] = _lastOperation?.previousValue;
                    }
                    else
                    {
                        // remove entry
                        _values.TryRemove(options.Name, out _);
                    }

                    _lastOperation = null;
                }
            }
        }

        private async Task StoreValueAsync(
            string value,
            ValueDirectiveOptions options,
            KernelInvocationContext context)
        {
            await SetValueAsync(options.Name, value);

            if (options.MimeType is { } mimeType)
            {
                context.DisplayAs(value, mimeType);
            }
        }
    }
}