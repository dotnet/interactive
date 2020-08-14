using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector
{
    public sealed class InspectionOptions
    {
        public enum LanguageVersion
        {
            CSHARP_1,
            CSHARP_2,
            CSHARP_3,
            CSHARP_4,
            CSHARP_5,
            CSHARP_6,
            CSHARP_7, CSHARP_7_1, CSHARP_7_2, CSHARP_7_3,
            CSHARP_8,
            CSHARP_DEFAULT,
            CSHARP_LATEST, CSHARP_LATEST_MAJOR,
            CSHARP_PREVIEW
        };
        public SourceCodeKind Kind { get; set; } = SourceCodeKind.Regular;
        public LanguageVersion CompilationLanguage { get; set; } = LanguageVersion.CSHARP_PREVIEW;
        public LanguageVersion DecompilationLanguage { get; set; } = LanguageVersion.CSHARP_PREVIEW;
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Debug;
        public Platform Platform { get; set; }
    }
}