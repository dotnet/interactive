// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.DotNet.Interactive.Http
{
    public class FileProvider : IFileProvider, IDisposable
    {
        private readonly EmbeddedFileProvider _root;
        private readonly IDisposable _eventSubscription;
        private readonly ConcurrentDictionary<string, EmbeddedFileProvider> _providers = new ConcurrentDictionary<string, EmbeddedFileProvider>();

        public FileProvider(Kernel kernel, Assembly rootProvider)
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));

            _root = new EmbeddedFileProvider(rootProvider ?? typeof(FileProvider).Assembly);
            _eventSubscription = kernel.KernelEvents
                .OfType<KernelExtensionLoaded>()
                .Subscribe(@event => RegisterExtension(@event.KernelExtension));
        }

        private void RegisterExtension(IKernelExtension kernelExtension)
        {
            if (kernelExtension is IStaticContentSource source)
            {
                var name = source.Name;
                _providers.GetOrAdd(name, key => new EmbeddedFileProvider(source.GetType().Assembly));
            }
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var (provider, path) = GetProviderAndPath(subpath);
            return provider.GetFileInfo(path);
        }

        private (IFileProvider provider, string path) GetProviderAndPath(string subpath)
        {
            IFileProvider provider = _root;
            var path = subpath;
            var parts = subpath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts[0] == "extensions")
            {
                provider = SelectProvider();
                path = ProcessPath();
            }

            return (provider, path);

            string ProcessPath()
            {

                return string.Join("/", parts.Skip(2));
            }

            IFileProvider SelectProvider()
            {
                var name = parts[1];
                if (!_providers.TryGetValue(name, out var embeddedFileProvider))
                {
                    throw new StaticContentSourceNotFoundException(name);
                }
                return embeddedFileProvider;
            }
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var (provider, path) = GetProviderAndPath(subpath);
            return provider.GetDirectoryContents(path);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }

        public void Dispose()
        {
            _eventSubscription.Dispose();
        }
    }
}