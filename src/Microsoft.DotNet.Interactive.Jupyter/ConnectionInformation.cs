// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ConnectionInformation
    {
        [JsonProperty("stdin_port")]
        public int StdinPort { get; set; }

        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("control_port")]
        public int ControlPort { get; set; }

        [JsonProperty("hb_port")]
        public int HBPort { get; set; }

        [JsonProperty("signature_scheme")]
        public string SignatureScheme { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("shell_port")]
        public int ShellPort { get; set; }

        [JsonProperty("transport")]
        public string Transport { get; set; }

        [JsonProperty("iopub_port")]
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
                JsonConvert.DeserializeObject<ConnectionInformation>(fileContent);

            return connectionInformation;
        }
    }
}
