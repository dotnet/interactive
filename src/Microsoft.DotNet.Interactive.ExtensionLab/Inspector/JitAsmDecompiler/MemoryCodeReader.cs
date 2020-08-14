using System;
using System.Runtime.CompilerServices;

using Iced.Intel;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler
{
    internal sealed class MemoryCodeReader : CodeReader
    {
        private readonly IntPtr _startPointer;
        private readonly uint _length;
        private uint _offset;

        public MemoryCodeReader(IntPtr startPointer, uint length)
        {
            _startPointer = startPointer;
            _length = length;
        }

        public override unsafe int ReadByte()
        {
            if (_offset >= _length)
                return -1;

            var @byte = Unsafe.Read<byte>((_startPointer + (int)_offset).ToPointer());
            _offset += 1;
            return @byte;
        }
    }
}
