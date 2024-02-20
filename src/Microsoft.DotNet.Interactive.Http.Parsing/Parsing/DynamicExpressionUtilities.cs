using Microsoft.DotNet.Interactive.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Http.Parsing
{
#nullable enable
    internal static class DynamicExpressionUtilites
    {

        const string DateTime = "$datetime";
        const string LocalDateTime = "$localDatetime";
        const string OffsetRegex = """(?:\s+(?<offset>[-+]?[^\s]+)\s+(?<option>[^\s]+))?""";
        const string TypeRegex = """(?:\s+(?<type>rfc1123|iso8601|'.+'|".+"))?""";

        internal static Regex guidPattern = new Regex(@$"^\$guid$", RegexOptions.Compiled);
        internal static Regex dateTimePattern = new Regex(@$"^\{DateTime}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        internal static Regex localDateTimePattern = new Regex(@$"^\{LocalDateTime}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        internal static Regex randomIntPattern = new Regex(@$"^\$randomInt(?:\s+(?<arguments>-?[^\s]+)){{0,2}}$", RegexOptions.Compiled);
        internal static Regex timestampPattern = new Regex($@"^\$timestamp{OffsetRegex}$", RegexOptions.Compiled);

        internal static HttpBindingResult<object?> ResolveExpressionBinding(HttpExpressionNode node, string expression)
        {
            var guidMatches = guidPattern.Matches(expression);
            if (guidMatches.Count == 1)
            {
                return node.CreateBindingSuccess(Guid.NewGuid().ToString());
            }

            if (expression.Contains(DateTime))
            {
                var dateTimeMatches = dateTimePattern.Matches(expression);
                if (dateTimeMatches.Count == 1)
                {
                    return GetDateTime(node, DateTime, expression, dateTimeMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, DateTime));
            }

            if (expression.Contains(LocalDateTime))
            {
                var localDateTimeMatches = localDateTimePattern.Matches(expression);
                if (localDateTimeMatches.Count == 1)
                {
                    return GetDateTime(node, LocalDateTime, expression, localDateTimeMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, LocalDateTime));
            }

            if (expression.Contains("$timestamp"))
            {
                var timestampMatches = timestampPattern.Matches(expression);
                if (timestampMatches.Count == 1)
                {
                    return GetTimestamp(node, expression, timestampMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectTimestampFormat(expression));
            }

            if (expression.Contains("$randomInt"))
            {
                var randomIntMatches = randomIntPattern.Matches(expression);
                if (randomIntMatches.Count == 1)
                {
                    return GetRandomInt(node, expression, randomIntMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectRandomIntFormat(expression));
            }

            return node.CreateBindingFailure(HttpDiagnostics.UnableToEvaluateExpression(expression));
        }


        private static HttpBindingResult<object?> GetTimestamp(HttpExpressionNode node, string expressionText, Match match)
        {
            if (match.Groups.Count == 3)
            {
                var currentDateTimeOffset = DateTimeOffset.UtcNow;

                if (string.Equals(expressionText, "$timestamp", StringComparison.InvariantCulture))
                {
                    return node.CreateBindingSuccess(currentDateTimeOffset.ToUnixTimeSeconds().ToString());
                }

                if (match.Groups["offset"].Success && match.Groups["option"].Success)
                {

                    var offsetString = match.Groups["offset"].Value;
                    if (int.TryParse(offsetString, out int offset))
                    {
                        var optionString = match.Groups["option"].Value;

                        if (currentDateTimeOffset.TryAddOffset(offset, optionString, out var newDateTimeOffset))
                        {
                            expressionText = newDateTimeOffset.Value.ToUnixTimeSeconds().ToString();
                            return node.CreateBindingSuccess(expressionText);
                        }
                        else
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidOption(expressionText, optionString));
                        }
                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidOffset(expressionText, offsetString));
                    }
                }

            }
            return node.CreateBindingFailure(HttpDiagnostics.IncorrectTimestampFormat(expressionText));

        }

        private static HttpBindingResult<object?> GetDateTime(HttpExpressionNode node, string dateTimeType, string expressionText, Match match)
        {
            if (match.Groups.Count == 4)
            {
                var currentDateTimeOffset = DateTimeOffset.UtcNow;
                if (match.Groups["offset"].Success && match.Groups["option"].Success)
                {
                    var offsetString = match.Groups["offset"].Value;
                    if (int.TryParse(offsetString, out int offset))
                    {
                        var optionString = match.Groups["option"].Value;
                        if (currentDateTimeOffset.TryAddOffset(offset, optionString, out var newDateTimeOffset))
                        {
                            currentDateTimeOffset = newDateTimeOffset.Value;
                        }
                        else
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidOption(expressionText, optionString));
                        }
                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidOffset(expressionText, offsetString));
                    }
                }
                string format;
                var formatProvider = Thread.CurrentThread.CurrentUICulture;
                var type = match.Groups["type"];

                string text;
                if (string.IsNullOrWhiteSpace(type.Value))
                {
                    text = currentDateTimeOffset.ToString();
                }
                else
                {
                    if (string.Equals(type.Value, "rfc1123", StringComparison.OrdinalIgnoreCase))
                    {
                        // For RFC1123, we want to be sure to use the invariant culture,
                        // since we are potentially overriding the format for local date time
                        // we should explicitly set the format provider to invariant culture
                        formatProvider = CultureInfo.InvariantCulture;
                        format = "r";
                    }
                    else if (string.Equals(type.Value, "iso8601", StringComparison.OrdinalIgnoreCase))
                    {
                        format = "o";
                    }
                    else
                    {
                        // This substring exists to strip out the double quotes that are expected in a custom format
                        format = type.Value.Substring(1, type.Value.Length - 2);
                    }

                    text = currentDateTimeOffset.ToString(format, formatProvider);
                }

                if (DateTimeOffset.TryParse(text, out _))
                {
                    return node.CreateBindingSuccess(text);
                }
            }
            return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expressionText, dateTimeType));
        }

        private static HttpBindingResult<object?> GetRandomInt(HttpExpressionNode node, string text, Match match)
        {
            if (TryParseArgumentsFromMatch(text, match, out var min, out var max, out var diagnostic))
            {
                Random random = new();
                if (!min.HasValue && !max.HasValue)
                {
                    text = random.Next().ToString();
                }
                else if (!min.HasValue && max.HasValue)
                {
                    text = random.Next(max.Value).ToString();
                }
                else if (min.HasValue && max.HasValue)
                {
                    text = random.Next(min.Value, max.Value).ToString();
                }

                return node.CreateBindingSuccess(text);
            }
            else
            {
                return node.CreateBindingFailure(diagnostic);
            }

            bool TryParseArgumentsFromMatch(string expression, Match match, out int? min, out int? max, [NotNullWhen(false)] out HttpDiagnosticInfo? diagnostic)
            {
                if (match.Success)
                {
                    var group = match.Groups["arguments"];
                    if (group.Captures.Count == 0)
                    {
                        max = null;
                        min = null;
                        diagnostic = null;
                        return true;
                    }
                    else if (group.Captures.Count == 1)
                    {
                        min = null;
                        string maxValueString = group.Captures[0].Value;
                        return TryParseInteger(maxValueString, expression, out max, out diagnostic);

                    }
                    else if (group.Captures.Count == 2)
                    {
                        string minValueString = group.Captures[0].Value;

                        if (!TryParseInteger(minValueString, expression, out min, out diagnostic))
                        {
                            max = null;
                            return false;
                        }

                        string maxValueString = group.Captures[1].Value;

                        if (!TryParseInteger(maxValueString, expression, out max, out diagnostic))
                        {
                            min = null;
                            return false;
                        }

                        if (min > max)
                        {
                            diagnostic = HttpDiagnostics.RandomIntMinMustNotBeGreaterThanMax(expression, min.Value.ToString(), max.Value.ToString());
                            min = null;
                            max = null;
                            return false;
                        }

                        return true;
                    }
                }


                min = null;
                max = null;
                diagnostic = HttpDiagnostics.IncorrectRandomIntFormat(expression);
                return false;

                bool TryParseInteger(string valueString, string expression, [NotNullWhen(true)] out int? value, [NotNullWhen(false)] out HttpDiagnosticInfo? diagnostic)
                {
                    if (int.TryParse(valueString, out var result))
                    {
                        value = result;
                        diagnostic = null;
                        return true;
                    }
                    else
                    {
                        value = null;
                        diagnostic = HttpDiagnostics.InvalidRandomIntArgument(expression, valueString);
                        return false;
                    }
                }
            }
        }
    }
}
