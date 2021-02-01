// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class SignatureHelpProduced : KernelEvent
    {
        public SignatureHelpProduced(RequestSignatureHelp command, IReadOnlyList<SignatureInformation> signatures, int activeSignature, int activeParameter)
            : base(command)
        {
            if (signatures?.Count >= 1)
            {
                // validate
                if (activeSignature < 0 || activeSignature >= signatures?.Count)
                {
                    throw new ArgumentOutOfRangeException("Active signature must be a valid index.", nameof(activeSignature));
                }

                if (activeParameter < 0 || (signatures[activeSignature].Parameters.Count > 0 && activeParameter >= signatures[activeSignature].Parameters.Count))
                {
                    throw new ArgumentOutOfRangeException("Active parameter must be a valid index.", nameof(activeParameter));
                }
            }
            else
            {
                if (activeSignature != 0)
                {
                    throw new ArgumentOutOfRangeException("When no signatures are provided, the active signature must be 0.");
                }

                if (activeParameter != 0)
                {
                    throw new ArgumentOutOfRangeException("When no signatures are provided, the active signature must be 0.");
                }
            }

            Signatures = signatures;
            ActiveSignature = activeSignature;
            ActiveParameter = activeParameter;
        }

        public static SignatureHelpProduced Empty(RequestSignatureHelp command) => new SignatureHelpProduced(command, null, 0, 0);

        public IReadOnlyList<SignatureInformation> Signatures { get; }

        public int ActiveSignature { get; }

        public int ActiveParameter { get; }
    }
}
