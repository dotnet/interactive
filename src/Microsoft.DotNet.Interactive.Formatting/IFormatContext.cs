using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public interface IFormatContext
    {
        /// <summary>Represents the approximate proportion of output width remaining to this format operation.</summary>
        bool IsNestedTable { get; }
    }

    internal class FormatContext : IFormatContext
    {
        public FormatContext() { IsNestedTable = false; }
        public FormatContext(bool isNestedTable) { IsNestedTable = isNestedTable; }
        public bool IsNestedTable { get; }
    }

    public static class FormatContextExtensions 
    {
        public static IFormatContext WithIsNestedTable(this IFormatContext context) => new FormatContext(true);

        /// <summary>Invoke the formatter, creating a new format context</summary>
        public static void Format(this ITypeFormatter formatter, object instance, TextWriter writer)
        {
            formatter.Format(new FormatContext(), instance, writer);
        }

    }
}
