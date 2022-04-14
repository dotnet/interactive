// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class WorkspaceRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; }

        public string RunArgs { get; }

        public Workspace Workspace { get; }

        public BufferId ActiveBufferId { get; }

        public WorkspaceRequest(
            Workspace workspace,
            BufferId activeBufferId = null,
            int? position = null,
            string requestId = null,
            string runArgs = "")
        {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

            RequestId = requestId;
            RunArgs = runArgs;

            if (activeBufferId != null)
            {
                ActiveBufferId = activeBufferId;
            }
            else if (workspace.Buffers.Length == 1)
            {
                ActiveBufferId = workspace.Buffers[0].Id;
            }

            if (position != null)
            {
                var buffer = Workspace.GetBufferWithSpecifiedIdOrSingleBufferIfThereIsOnlyOne(ActiveBufferId);
                buffer.Position = position.Value;
            }
        }
    }
}
