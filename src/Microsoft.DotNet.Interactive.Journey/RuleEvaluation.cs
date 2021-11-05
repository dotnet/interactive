// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Journey
{
    [TypeFormatterSource(typeof(RuleEvaluationFormatterSource))]
    public class RuleEvaluation
    {

        public RuleEvaluation(Outcome outcome, string name = null, string reason = null, object hint = null)
        {
            Name = name;
            Hint = hint;
            Outcome = outcome;
            if (string.IsNullOrWhiteSpace(reason))
            {
                Reason = outcome switch
                {
                    Outcome.Success => "All tests passed.",
                    Outcome.PartialSuccess => "Some tests passed.",
                    Outcome.Failure => "Incorrect solution.",
                    _ => throw new NotImplementedException()
                };
            }
            else
            {
                Reason = reason;
            }
        }

        public string Name { get; }

        public Outcome Outcome { get; }

        public string Reason { get; }

        public object Hint { get; }

        public bool Passed => Outcome == Outcome.Success;

        private PocketView FormatAsHtml()
        {
            var outcomeDivStyle = Outcome switch
            {
                Outcome.Success => "background:#49B83461; padding:.75em; border-color:#49B83461",
                Outcome.PartialSuccess => "background:#FF00008A; padding:.75em; border-color:#FF00008A",
                Outcome.Failure => "background:#FF00008A; padding:.75em; border-color:#FF00008A",
                _ => throw new NotImplementedException()
            };

            var outcomeMessage = Outcome switch
            {
                Outcome.Success => "Success",
                Outcome.PartialSuccess => "Partial Success",
                Outcome.Failure => "Failure",
                _ => throw new NotImplementedException()
            };

            var elements = new List<PocketView>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                PocketView header = summary[style: outcomeDivStyle](b(outcomeMessage));

                elements.Add(header);

            }
            else
            {
                PocketView header = summary[style: outcomeDivStyle]($"[ {Name} ]: ", b(outcomeMessage));

                elements.Add(header);
            }

            if (!string.IsNullOrWhiteSpace(Reason))
            {
                elements.Add(div(Reason));
            }

            if (Hint is not null)
            {
                var hintElement = div[@class: "hint"](b("Hint: "), Hint);
                elements.Add(hintElement);
            }

            PocketView report = details[@class: "ruleEvaluation", style: "padding:.5em"](elements);

            return report;
        }

        private class RuleEvaluationFormatterSource : ITypeFormatterSource
        {
            public IEnumerable<ITypeFormatter> CreateTypeFormatters()
            {
                return new ITypeFormatter[] {
                    new HtmlFormatter<RuleEvaluation>((evaluation, context) =>
                    {
                        var view = evaluation.FormatAsHtml();
                        view.WriteTo(context);
                    })
                };
            }
        }
    }
}