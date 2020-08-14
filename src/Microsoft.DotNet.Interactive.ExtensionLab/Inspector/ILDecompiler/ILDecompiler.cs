using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.ILDecompiler
{
    internal static class ILDecompiler
    {
        internal static string Decompile(PEFile assembly, IDebugInfoProvider debugInfoProvider)
        {
            var ilDecompilation = new PlainTextOutput() { IndentationString = Defaults.DefaultIndent };

            var ilDecompiler = new ReflectionDisassembler(ilDecompilation, CancellationToken.None)
            {
                DebugInfo = debugInfoProvider,
                DetectControlStructure = true,
                ExpandMemberDefinitions = true,
                ShowMetadataTokens = true,
                ShowSequencePoints = true
            };

            ilDecompiler.WriteModuleContents(assembly);

            return ilDecompilation.ToString();
        }
    }
}
