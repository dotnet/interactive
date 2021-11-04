// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Journey
{
    public class ChallengeDefinition
    {
        public string Name { get; }
        public IReadOnlyList<SubmitCode> Setup { get; }
        public IReadOnlyList<SendEditableCode> Contents { get; }
        public IReadOnlyList<SubmitCode> EnvironmentSetup { get; }

        public ChallengeDefinition(string name, IReadOnlyList<SubmitCode> setup, IReadOnlyList<SendEditableCode> contents, IReadOnlyList<SubmitCode> environmentSetup)
        {
            Name = name;
            Setup = setup;
            Contents = contents;
            EnvironmentSetup = environmentSetup;
        }

        public Challenge ToChallenge()
        {
            return new Challenge(Setup, Contents, EnvironmentSetup, Name);
        }
    }
}
