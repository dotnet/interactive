// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.Stream)]
    public class Stream : PubSubMessage
    {
        [JsonIgnore]
        public static string StandardError { get; } = "stderr";

        [JsonIgnore]
        public static string StandardOutput { get; } = "stdout";

        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("text")]
        public string Text { get; }

        public Stream(string name, string text)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            Name = name;
            Text = text;
        }

        public static Stream StdErr(string text)
        {
            return new Stream(StandardError, text);
        }

        public static Stream StdOut(string text)
        {
            return new Stream(StandardOutput, text);
        }
    }
}