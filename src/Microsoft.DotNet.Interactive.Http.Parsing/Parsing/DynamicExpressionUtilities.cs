﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Http.Parsing
{
#nullable enable
    internal static class DynamicExpressionUtilites
    {
        const string DateTimeMacroName = "$datetime";
        const string LocalDateTimeMacroName = "$localDatetime";
        const string OffsetRegex = """(?:\s+(?<offset>[-+]?[^\s]+)\s+(?<option>[^\s]+))?""";
        const string TypeRegex = """(?:\s+(?<type>rfc1123|iso8601|'.+'|".+"))?""";

        internal static Regex guidPattern = new Regex(@$"^\$guid$", RegexOptions.Compiled);
        internal static Regex dateTimePattern = new Regex(@$"^\{DateTimeMacroName}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        internal static Regex localDateTimePattern = new Regex(@$"^\{LocalDateTimeMacroName}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        internal static Regex randomIntPattern = new Regex(@$"^\$randomInt(?:\s+(?<arguments>-?[^\s]+)){{0,2}}$", RegexOptions.Compiled);
        internal static Regex timestampPattern = new Regex($@"^\$timestamp{OffsetRegex}$", RegexOptions.Compiled);

        private delegate DateTimeOffset GetDateTimeOffsetDelegate(bool isLocal);
        private static GetDateTimeOffsetDelegate GetDateTimeOffset = DefaultGetDateTimeOffset;

        private static DateTimeOffset DefaultGetDateTimeOffset(bool isLocal)
        {
            return isLocal ? DateTimeOffset.Now : DateTimeOffset.UtcNow;
        }

        // For Unit Tests, pass in a known date time, use it for all time related funcs, and then reset to default time handling
        internal static HttpBindingResult<object?> ResolveExpressionBinding(HttpExpressionNode node, Func<DateTimeOffset> dateTimeFunc, string expression)
        {
            try
            {
                GetDateTimeOffset = delegate (bool _) { return dateTimeFunc(); };
                return ResolveExpressionBinding(node, expression);
            }
            finally
            {
                GetDateTimeOffset = DefaultGetDateTimeOffset;
            }
        }

        internal static HttpBindingResult<object?> ResolveExpressionBinding(HttpExpressionNode node, string expression)
        {
            var guidMatches = guidPattern.Matches(expression);
            if (guidMatches.Count == 1)
            {
                return node.CreateBindingSuccess(Guid.NewGuid().ToString());
            }

            if (expression.Contains(DateTimeMacroName))
            {
                var dateTimeMatches = dateTimePattern.Matches(expression);
                if (dateTimeMatches.Count == 1)
                {
                    return GetDateTime(node, DateTimeMacroName, GetDateTimeOffset(isLocal: false), expression, dateTimeMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, DateTimeMacroName));
            }

            if (expression.Contains(LocalDateTimeMacroName))
            {
                var localDateTimeMatches = localDateTimePattern.Matches(expression);
                if (localDateTimeMatches.Count == 1)
                {
                    return GetDateTime(node, LocalDateTimeMacroName, GetDateTimeOffset(isLocal: false), expression, localDateTimeMatches[0]);
                }

                return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, LocalDateTimeMacroName));
            }

            if (expression.Contains("$timestamp"))
            {
                var timestampMatches = timestampPattern.Matches(expression);
                if (timestampMatches.Count == 1)
                {
                    return GetTimestamp(node, GetDateTimeOffset(isLocal: false), expression, timestampMatches[0]);
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


        private static HttpBindingResult<object?> GetTimestamp(HttpExpressionNode node, DateTimeOffset currentDateTimeOffset, string expressionText, Match match)
        {
            if (match.Groups.Count == 3)
            {
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

        private static HttpBindingResult<object?> GetDateTime(HttpExpressionNode node, string dateTimeType, DateTimeOffset currentDateTimeOffset, string expressionText, Match match)
        {
            if (match.Groups.Count == 4)
            {
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

                // $datetime and $localDatetime MUST have either rfc1123, iso8601 or some other parameter.
                // $datetime or $localDatetime alone should result in a binding error.
                if (type is not null && !string.IsNullOrWhiteSpace(type.Value))
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

                    try
                    {
                        string text = currentDateTimeOffset.ToString(format, formatProvider);
                        return node.CreateBindingSuccess(text);
                    }
                    catch(FormatException)
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeCustomFormat(format));
                    }
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
