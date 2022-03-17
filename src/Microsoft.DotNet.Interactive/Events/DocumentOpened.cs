using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class DocumentOpened : KernelEvent
    {
        public string Path { get; }
        public string RegionName { get; }
        public string Content { get; }

        public DocumentOpened(OpenDocument command, string path, string regionName, string content)
            : base(command)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            RegionName = regionName;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}
