using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive
{
    public class ProjectFile
    {
        public string Path { get; }
        public string Content { get; }

        public ProjectFile(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }
}
