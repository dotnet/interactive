//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    internal class CommandUtils
    {
        internal readonly static CmdletInfo OutStringCmdletInfo = new CmdletInfo("Out-String", typeof(OutStringCommand));
        internal readonly static CmdletInfo WriteInformationCmdletInfo = new CmdletInfo("Write-Information", typeof(WriteInformationCommand));
    
        internal readonly static object BoxedTrue = (object)true;
        internal readonly static object BoxedFalse = (object)false;
    }
}
