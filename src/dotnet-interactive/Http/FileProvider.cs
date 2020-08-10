// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.DotNet.Interactive.App.Http
{
    public class FileProvider : IFileProvider, IDisposable
    {
        private readonly EmbeddedFileProvider _root;
        private readonly IDisposable _eventSubscription;
        private readonly ConcurrentDictionary<string, EmbeddedFileProvider> _providers = new ConcurrentDictionary<string, EmbeddedFileProvider>();

        public FileProvider(Kernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));

            _root = new EmbeddedFileProvider(typeof(FileProvider).Assembly);
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
            var provider = SelectProvider(subpath);
            var path = ProcessPath(subpath);
            return provider.GetFileInfo(path);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var provider = SelectProvider(subpath);
            var path = ProcessPath(subpath);
            return provider.GetDirectoryContents(path);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }

        private string ProcessPath(string path)
        {
            path = path.TrimStart('/');
            if (path.StartsWith("extensions/"))
            {
                return string.Join("/", path.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries).Skip(2));
            }

            return path;
        }

        private IFileProvider SelectProvider(string path)
        {
            path = path.TrimStart('/');
            if (path.StartsWith("extensions/"))
            {
                var name = path.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries)[1];
                if (!_providers.TryGetValue(name, out var provider))
                {
                    throw new StaticContentSourceNotFoundException(name);
                }
                return provider;
            }

            return _root;
        }

        public void Dispose()
        {
            _eventSubscription.Dispose();
        }
    }
}