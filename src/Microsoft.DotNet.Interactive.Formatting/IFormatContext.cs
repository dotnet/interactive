using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public interface IFormatContext
    {
        /// <summary>Represents the approximate proportion of output width remaining to this format operation.</summary>
        bool IsNestedInTable { get; }
    }

    internal class FormatContext : IFormatContext
    {
        public FormatContext() { IsNestedInTable = false; }
        public FormatContext(bool isNestedInTable) { IsNestedInTable = isNestedInTable; }
        public bool IsNestedInTable { get; }
    }

    public static class FormatContextExtensions 
    {
        public static IFormatContext NestedInTable(this IFormatContext context) => new FormatContext(true);

        /// <summary>Invoke the formatter, creating a new format context</summary>
        public static void Format(this ITypeFormatter formatter, object instance, TextWriter writer)
        {
            formatter.Format(new FormatContext(), instance, writer);
        }

    }
}
