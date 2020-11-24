/*
Copyright (c) 2016-2017, Andrey Shchekin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
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
        public string SourceFileName => "";

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

        public bool TryGetName(MethodDefinitionHandle method, int index, out string name)
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
