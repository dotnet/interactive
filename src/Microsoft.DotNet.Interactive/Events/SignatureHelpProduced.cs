// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class SignatureHelpProduced : KernelEvent
    {
        public SignatureHelpProduced(RequestSignatureHelp command, IReadOnlyList<SignatureInformation> signatures, int activeSignatureIndex, int activeParameterIndex)
            : base(command)
        {
            if (signatures?.Count >= 1)
            {
                // validate
                if (activeSignatureIndex < 0 || activeSignatureIndex >= signatures?.Count)
                {
                    throw new ArgumentOutOfRangeException("Active signature must be a valid index.", nameof(activeSignatureIndex));
                }

                if (activeParameterIndex < 0 || (signatures[activeSignatureIndex].Parameters.Count > 0 && activeParameterIndex >= signatures[activeSignatureIndex].Parameters.Count))
                {
                    throw new ArgumentOutOfRangeException("Active parameter must be a valid index.", nameof(activeParameterIndex));
                }
            }
            else
            {
                if (activeSignatureIndex != 0)
                {
                    throw new ArgumentOutOfRangeException("When no signatures are provided, the active signature index must be 0.");
                }

                if (activeParameterIndex != 0)
                {
                    throw new ArgumentOutOfRangeException("When no parameters are provided, the active parameter index must be 0.");
                }
            }

            Signatures = signatures;
            ActiveSignatureIndex = activeSignatureIndex;
            ActiveParameterIndex = activeParameterIndex;
        }

        public static SignatureHelpProduced Empty(RequestSignatureHelp command) => new SignatureHelpProduced(command, null, 0, 0);

        public IReadOnlyList<SignatureInformation> Signatures { get; }

        public int ActiveSignatureIndex { get; }

        public int ActiveParameterIndex { get; }
    }
}
