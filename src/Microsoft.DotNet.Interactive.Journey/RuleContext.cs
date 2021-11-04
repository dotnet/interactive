// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Journey
{
    public class RuleContext
    {
        public string Name { get; }
        public Challenge Challenge => _challengeContext.Challenge;
        public string SubmittedCode { get; }
        public IEnumerable<KernelEvent> EventsProduced { get;  }
        public bool Passed { get; private set; }

        private readonly ChallengeContext _challengeContext;

        public RuleContext(ChallengeContext challengeContext, string submittedCode = null, IEnumerable<KernelEvent> events = null, string name = "")
        {
            _challengeContext = challengeContext;
            SubmittedCode = submittedCode;
            EventsProduced = events;
            Name = name;
        }

        public void Fail(string reason = null, object hint = null)
        {
            Passed = false;
            _challengeContext.Evaluation.SetRuleOutcome(Name, Outcome.Failure, reason, hint);
        }

        public void Pass(string reason = null, object hint = null)
        {
            Passed = true;
            _challengeContext.Evaluation.SetRuleOutcome(Name, Outcome.Success, reason, hint);
        }

        public void PartialPass(string reason = null, object hint = null)
        {
            _challengeContext.Evaluation.SetRuleOutcome(Name, Outcome.PartialSuccess, reason, hint);
        }
    }
}
