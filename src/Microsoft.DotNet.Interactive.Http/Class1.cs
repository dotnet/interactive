using System;

namespace Microsoft.DotNet.Interactive.Http
{
    public class Server : IDisposable
    {
        private readonly Kernel _kernel;

        public Server(Kernel kernel)
        {
            this.kernel = kernel;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
