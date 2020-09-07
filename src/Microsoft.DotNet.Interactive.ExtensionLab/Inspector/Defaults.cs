using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler;

using Microsoft.Diagnostics.Runtime;

using LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector
{
    internal static class Defaults
    {
        internal const string DefaultIndent = "    ";
        internal const string InternalAssemblyName = "InsightsAssembly";

        internal static readonly Pool<ClrRuntime> DefaultRuntimePool = new Pool<ClrRuntime>(() => {
            using var currentProcess = Process.GetCurrentProcess();
            var dataTarget = DataTarget.AttachToProcess(currentProcess.Id, uint.MaxValue, AttachFlag.Passive);
            return dataTarget.ClrVersions.Single(c => c.Flavor == ClrFlavor.Core).CreateRuntime();
        });

        internal static readonly string[] DefaultCSharpImports =
        {
            "System",
            "System.IO",
            "System.Text",
            "System.Collections",
            "System.Collections.Generic",
            "System.Threading.Tasks",
            "System.Linq"
        };

        internal static readonly IReadOnlyDictionary<InspectionOptions.LanguageVersion, LanguageVersion> CompilationLanguageVersionMapping =
            new Dictionary<InspectionOptions.LanguageVersion, LanguageVersion>() {
                    { InspectionOptions.LanguageVersion.CSHARP_1, LanguageVersion.CSharp1 },
                    { InspectionOptions.LanguageVersion.CSHARP_2, LanguageVersion.CSharp2 },
                    { InspectionOptions.LanguageVersion.CSHARP_3, LanguageVersion.CSharp3 },
                    { InspectionOptions.LanguageVersion.CSHARP_4, LanguageVersion.CSharp4 },
                    { InspectionOptions.LanguageVersion.CSHARP_5, LanguageVersion.CSharp5 },
                    { InspectionOptions.LanguageVersion.CSHARP_6, LanguageVersion.CSharp6 },
                    { InspectionOptions.LanguageVersion.CSHARP_7, LanguageVersion.CSharp7 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_1, LanguageVersion.CSharp7_1 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_2, LanguageVersion.CSharp7_2 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_3, LanguageVersion.CSharp7_3 },
                    { InspectionOptions.LanguageVersion.CSHARP_8, LanguageVersion.CSharp8 },
                    { InspectionOptions.LanguageVersion.CSHARP_DEFAULT, LanguageVersion.Default },
                    { InspectionOptions.LanguageVersion.CSHARP_LATEST, LanguageVersion.Latest },
                    { InspectionOptions.LanguageVersion.CSHARP_LATEST_MAJOR, LanguageVersion.LatestMajor },
                    { InspectionOptions.LanguageVersion.CSHARP_PREVIEW, LanguageVersion.Preview }
            };

        internal static readonly IReadOnlyDictionary<InspectionOptions.LanguageVersion, ICSharpCode.Decompiler.CSharp.LanguageVersion> DecompilationLanguageVersionMapping =
            new Dictionary<InspectionOptions.LanguageVersion, ICSharpCode.Decompiler.CSharp.LanguageVersion>() {
                    { InspectionOptions.LanguageVersion.CSHARP_1, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp1 },
                    { InspectionOptions.LanguageVersion.CSHARP_2, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp2 },
                    { InspectionOptions.LanguageVersion.CSHARP_3, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp3 },
                    { InspectionOptions.LanguageVersion.CSHARP_4, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp4 },
                    { InspectionOptions.LanguageVersion.CSHARP_5, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp5 },
                    { InspectionOptions.LanguageVersion.CSHARP_6, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp6 },
                    { InspectionOptions.LanguageVersion.CSHARP_7, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp7 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_1, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp7_1 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_2, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp7_2 },
                    { InspectionOptions.LanguageVersion.CSHARP_7_3, ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp7_3 },
                    // C#8, Default, LatestMajor do not exist in the ICSharpCode.Decompiler, using "Latest".
                    { InspectionOptions.LanguageVersion.CSHARP_8, ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest },
                    { InspectionOptions.LanguageVersion.CSHARP_DEFAULT, ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest },
                    { InspectionOptions.LanguageVersion.CSHARP_LATEST, ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest },
                    { InspectionOptions.LanguageVersion.CSHARP_LATEST_MAJOR, ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest },
                    { InspectionOptions.LanguageVersion.CSHARP_PREVIEW, ICSharpCode.Decompiler.CSharp.LanguageVersion.Preview }
            };
        internal static LanguageVersion GetCompilationLanguageVersion(InspectionOptions.LanguageVersion internalLanguageVersion) => CompilationLanguageVersionMapping[internalLanguageVersion];
        internal static  ICSharpCode.Decompiler.CSharp.LanguageVersion GetDecompilationLanguageVersion(InspectionOptions.LanguageVersion internalLanguageVersion) => DecompilationLanguageVersionMapping[internalLanguageVersion];
    }
}
