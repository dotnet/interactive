using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.DebugInfo;

using Decompiler = ICSharpCode.Decompiler;


namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector
{
    internal sealed class PdbDebugInfoProvider : IDebugInfoProvider, IDisposable
    {
        private readonly MetadataReaderProvider _readerProvider;
        private readonly MetadataReader _reader;

        public string Description => "";

        public PdbDebugInfoProvider(Stream symbolStream)
        {
            if (symbolStream is null)
                throw new ArgumentNullException(nameof(symbolStream));

            this._readerProvider = MetadataReaderProvider.FromPortablePdbStream(symbolStream);
            this._reader = _readerProvider.GetMetadataReader();
        }
        public IList<Decompiler.DebugInfo.SequencePoint> GetSequencePoints(MethodDefinitionHandle method)
        {
            var debugInfo = this._reader.GetMethodDebugInformation(method);

            var points = debugInfo.GetSequencePoints();
            var result = new List<Decompiler.DebugInfo.SequencePoint>();
            foreach (var point in points)
            {
                result.Add(new Decompiler.DebugInfo.SequencePoint
                {
                    Offset = point.Offset,
                    StartLine = point.StartLine,
                    StartColumn = point.StartColumn,
                    EndLine = point.EndLine,
                    EndColumn = point.EndColumn,
                    DocumentUrl = "_"
                });
            }
            return result;
        }

        public IList<Variable> GetVariables(MethodDefinitionHandle method)
        {
            var variables = new List<Variable>();
            foreach (var local in EnumerateLocals(method))
                variables.Add(new Variable(local.Index, this._reader.GetString(local.Name)));

            return variables;
        }

        public bool TryGetName(MethodDefinitionHandle method, int index, out string? name)
        {
            name = null;
            foreach (var local in EnumerateLocals(method))
            {
                if (local.Index == index)
                {
                    name = this._reader.GetString(local.Name);
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<LocalVariable> EnumerateLocals(MethodDefinitionHandle method)
        {
            foreach (var scopeHandle in _reader.GetLocalScopes(method))
            {
                var scope = this._reader.GetLocalScope(scopeHandle);
                foreach (var variableHandle in scope.GetLocalVariables())
                    yield return this._reader.GetLocalVariable(variableHandle);
            }
        }

        public void Dispose() => this._readerProvider.Dispose();
    }
}
