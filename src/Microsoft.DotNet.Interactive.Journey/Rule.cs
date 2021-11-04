// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Journey
{
    public class Rule
    {
        public string Name { get; }
        private readonly Func<RuleContext, Task> evaluateRuleContextHandler;

        public Rule(Func<RuleContext, Task> ruleConstraints, string name = null)
        {
            Name = name;
            evaluateRuleContextHandler = ruleConstraints;
        }
        internal async Task Evaluate(RuleContext context)
        {
            await evaluateRuleContextHandler.Invoke(context);
        }
    }
}