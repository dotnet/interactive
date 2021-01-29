// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ConnectionInformation
    {
        [JsonPropertyName("stdin_port")]
        public int StdinPort { get; set; }

        [JsonPropertyName("ip")]
        public string IP { get; set; }

        [JsonPropertyName("control_port")]
        public int ControlPort { get; set; }

        [JsonPropertyName("hb_port")]
        public int HBPort { get; set; }

        [JsonPropertyName("signature_scheme")]
        public string SignatureScheme { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("shell_port")]
        public int ShellPort { get; set; }

        [JsonPropertyName("transport")]
        public string Transport { get; set; }

        [JsonPropertyName("iopub_port")]
        public int IOPubPort { get; set; }

        public static ConnectionInformation Load(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!file.Exists)
            {
                throw new FileNotFoundException($"Cannot locate {file.FullName}");
            }

            var fileContent = File.ReadAllText(file.FullName);

            var connectionInformation =
                JsonSerializer.Deserialize<ConnectionInformation>(fileContent);

            return connectionInformation;
        }
    }
}
