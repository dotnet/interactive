using System;
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
            if (path.StartsWith("extensions/"))
            {
                throw new NotImplementedException();
            }

            return path;
        }

        private IFileProvider SelectProvider(string path)
        {
            if (path.StartsWith("extensions/"))
            {
                throw new NotImplementedException();
            }

            return _root;
        }

        public void Dispose()
        {
            _eventSubscription.Dispose();
        }
    }
}