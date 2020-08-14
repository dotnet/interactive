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
