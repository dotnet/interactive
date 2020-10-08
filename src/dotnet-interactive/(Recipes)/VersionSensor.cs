using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Recipes
{
#if !RecipesProject
    [DebuggerStepThrough]
#endif
    internal partial class VersionSensor
    {
        private static readonly Lazy<BuildInfo> buildInfo = new Lazy<BuildInfo>(() =>
        {
            var assembly = typeof(VersionSensor).GetTypeInfo().Assembly;

            var info = new BuildInfo
            {
                AssemblyName = assembly.GetName().Name,
                AssemblyInformationalVersion = assembly
                                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                            .InformationalVersion,
                AssemblyVersion = assembly.GetName().Version.ToString(),
                BuildDate = new FileInfo(new Uri(assembly.CodeBase).LocalPath).CreationTimeUtc.ToString("o")
            };

            AssignServiceVersionTo(info);

            return info;
        });

        public static BuildInfo Version()
        {
            return buildInfo.Value;
        }

        public class BuildInfo
        {
            public string AssemblyVersion { get; set; }
            public string BuildDate { get; set; }
            public string AssemblyInformationalVersion { get; set; }
            public string AssemblyName { get; set; }
            public string ServiceVersion { get; set; }
        }

        static partial void AssignServiceVersionTo(BuildInfo buildInfo);
    }
}
