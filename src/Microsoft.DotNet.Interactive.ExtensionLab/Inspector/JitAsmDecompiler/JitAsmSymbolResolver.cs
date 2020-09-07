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
using Iced.Intel;

using Microsoft.Diagnostics.Runtime;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler
{
    internal sealed class JitAsmSymbolResolver : ISymbolResolver
    {
        private readonly ClrRuntime _runtime;
        private readonly ulong _currentMethodAddress;
        private readonly uint _currentMethodLength;

        public JitAsmSymbolResolver(ClrRuntime runtime, ulong currentMethodAddress, uint currentMethodLength)
        {
            _runtime = runtime;
            _currentMethodAddress = currentMethodAddress;
            _currentMethodLength = currentMethodLength;
        }

        public bool TryGetSymbol(in Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol)
        {
            if (address >= _currentMethodAddress && address < _currentMethodAddress + _currentMethodLength)
            {
                // relative offset reference
                symbol = new SymbolResult(address, "L" + (address - _currentMethodAddress).ToString("x4"));
                return true;
            }

            var method = _runtime.GetMethodByAddress(address);
            if (method == null)
            {
                symbol = default;
                return false;
            }

            symbol = new SymbolResult(address, method.GetFullSignature());
            return true;
        }
    }
}
