using System;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class FormatContext
    {
        public FormatContext() { ContentThreshold = 1.0;  }

        /// <summary>Indicates the requested proportion of information to show in this context.</summary>
        public double ContentThreshold { get; internal set; }

        /// <summary>Indicates a request for other formatters to reduce their information content.</summary>
        public FormatContext ReduceContent(double proportion) => 
            new FormatContext() { ContentThreshold = ContentThreshold * (Math.Max(0.0,Math.Min(1.0,proportion))) };

        /// <summary>Indicates a typical setting to reduce content in inner positions of a table.</summary>
        /// <remarks>When this reduction is applied, further nested tables and property expansions are avoided.</summary>
        public const double NestedInTable = 0.2;
    }
}
