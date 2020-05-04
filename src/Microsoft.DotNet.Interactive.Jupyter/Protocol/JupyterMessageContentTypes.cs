// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class JupyterMessageContentTypes
    {
        public const string ExecuteRequest = "execute_request";

        public const string ExecuteReply = "execute_reply";

        public const string ExecuteResult = "execute_result";

        public const string ExecuteInput = "execute_input";

        public const string KernelInfoRequest = "kernel_info_request";

        public const string KernelInfoReply = "kernel_info_reply";

        public const string KernelShutdownRequest = "shutdown_request";

        public const string KernelShutdownReply = "shutdown_reply";

        public const string CompleteRequest = "complete_request";

        public const string CompleteReply = "complete_reply";

        public const string IsCompleteRequest = "is_complete_request";

        public const string IsCompleteReply = "is_complete_reply";

        public const string Status = "status";

        public const string Stream = "stream";

        public const string Error = "error";

        public const string DisplayData = "display_data";

        public const string UpdateDisplayData = "update_display_data";

        public const string InspectRequest = "inspect_request";

        public const string InspectReply = "inspect_reply";

        public const string HistoryRequest = "history_request";

        public const string HistoryReply = "history_reply";

        public const string ClearOutput = "clear_output";

        public const string InputRequest = "input_request";

        public const string InputReply = "input_reply";

        public const string CommOpen = "comm_open";

        public const string CommClose = "comm_close";

        public const string CommMsg = "comm_msg";

        public const string CommInfoRequest = "comm_info_request";

        public const string CommInfoReply = "comm_info_reply";

        public const string InterruptRequest = "interrupt_request";

        public const string InterruptReply = "interrupt_reply";
    }
}
