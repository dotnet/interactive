using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.CSharpCompiler
{
    [Serializable]
    public class CSharpCompilationFailedException : Exception
    {
        private const string DefaultMessage = "C# Inspector failed";
        public IEnumerable<Diagnostic> Diagnostics = Enumerable.Empty<Diagnostic>();

        public CSharpCompilationFailedException(IEnumerable<Diagnostic> diagnostics, string message = DefaultMessage)
        {
            if (diagnostics is null)
                throw new ArgumentNullException(nameof(diagnostics));

            this.Diagnostics = diagnostics;
        }
        protected CSharpCompilationFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
}
