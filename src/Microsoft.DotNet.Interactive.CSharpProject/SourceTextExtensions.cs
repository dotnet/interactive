// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Buffer = Microsoft.DotNet.Interactive.CSharpProject.Buffer;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;

namespace Microsoft.DotNet.Interactive.CSharpProject.MLS.Project
{

    public static class SourceTextExtensions
    {
        private const string FSharpRegionStart = "//#region";
        private const string FSharpRegionEnd = "//#endregion";

        public static IEnumerable<(BufferId bufferId, TextSpan span, TextSpan outerSpan)> ExtractRegions(this SourceText code, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension)
            {
                case ".cs":
                case ".csx":
                    return ExtractRegionsCSharp(code, fileName);
                case ".fs":
                case ".fsx":
                    return ExtractRegionsFSharp(code, fileName);
                default:
                    throw new InvalidOperationException($"Unsupported file extension '{extension}'");
            }
        }

        private static IEnumerable<(BufferId bufferId, TextSpan span, TextSpan outerSpan)> ExtractRegionsCSharp(SourceText code, string fileName)
        {
            IEnumerable<(SyntaxTrivia startRegion, SyntaxTrivia endRegion, BufferId bufferId)> FindRegions(SyntaxNode syntaxNode)
            {
                var nodesWithRegionDirectives =
                    from node in syntaxNode.DescendantNodesAndTokens()
                    where node.HasLeadingTrivia
                    from leadingTrivia in node.GetLeadingTrivia()
                    where leadingTrivia.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                          leadingTrivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                    select node;

                var stack = new Stack<SyntaxTrivia>();
                var processedSpans = new HashSet<TextSpan>();

                foreach (var nodeWithRegionDirective in nodesWithRegionDirectives)
                {
                    var triviaList = nodeWithRegionDirective.GetLeadingTrivia();

                    foreach (var currentTrivia in triviaList)
                    {
                        if (processedSpans.Add(currentTrivia.FullSpan))
                        {
                            if (currentTrivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
                            {
                                stack.Push(currentTrivia);
                            }
                            else if (currentTrivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia) && stack.Count > 0)
                            {
                                var start = stack.Pop();
                                var regionName = start.ToFullString().Replace("#region", string.Empty).Trim();
                                yield return (start, currentTrivia, new BufferId(fileName, regionName));
                            }
                        }
                    }
                }
            }

            var sourceCodeText = code.ToString();
            var root = CSharpSyntaxTree.ParseText(sourceCodeText).GetRoot();

            foreach (var (startRegion, endRegion, label) in FindRegions(root))
            {
                var innerStart = startRegion.GetLocation().SourceSpan.End;

                var innerLength = endRegion.GetLocation().SourceSpan.Start -
                             startRegion.GetLocation().SourceSpan.End;

                var innerLoc = new TextSpan(innerStart, innerLength);

                var outerStart = startRegion.GetLocation().SourceSpan.Start;
                var outerLength = endRegion.GetLocation().SourceSpan.End -
                                  startRegion.GetLocation().SourceSpan.Start;
                var outerLoc = new TextSpan(outerStart, outerLength);

                yield return (label, innerLoc, outerLoc);
            }
        }

        private static IEnumerable<(BufferId bufferId, TextSpan span, TextSpan outerSpan)> ExtractRegionsFSharp(SourceText code, string fileName)
        {
            var extractedRegions = new List<(BufferId, TextSpan, TextSpan)>();
            var text = code.ToString();
            int regionTagStartIndex = text.IndexOf(FSharpRegionStart);
            while (regionTagStartIndex >= 0)
            {
                var regionLabelStartIndex = regionTagStartIndex + FSharpRegionStart.Length;
                var regionLabelEndIndex = text.IndexOf('\n', regionTagStartIndex);
                var regionLabel = text.Substring(regionLabelStartIndex, regionLabelEndIndex - regionLabelStartIndex).Trim();
                var regionTagEndIndex = text.IndexOf(FSharpRegionEnd, regionTagStartIndex);
                if (regionTagEndIndex >= 0)
                {
                    var regionEndTagLastIndex = regionTagEndIndex + FSharpRegionEnd.Length;

                    var contentStart = regionLabelEndIndex + 1; // swallow newline

                    var newlineBeforeEndRegionTag = text.LastIndexOf('\n', regionTagEndIndex);
                    var endRegionIndentOffset = regionTagEndIndex - newlineBeforeEndRegionTag;
                    var contentEnd = regionTagEndIndex - endRegionIndentOffset;

                    var contentSpan = new TextSpan(contentStart, contentEnd - contentStart);
                    var regionSpan = new TextSpan(regionTagStartIndex, regionEndTagLastIndex - regionTagStartIndex);
                    extractedRegions.Add((new BufferId(fileName, regionLabel), contentSpan, regionSpan));

                    regionTagStartIndex = text.IndexOf(FSharpRegionStart, regionTagEndIndex);
                }
                else
                {
                    break;
                }
            }

            return extractedRegions;
        }

        public static IEnumerable<Buffer> ExtractBuffers(this SourceText code, string fileName)
        {
            var extractedBuffers = new List<Buffer>();
            foreach ((var bufferId, var contentSpan, var regionSpan) in ExtractRegions(code, fileName))
            {
                var content = code.ToString(contentSpan);
                content = content.FormatSourceCode(fileName);
                extractedBuffers.Add(new Buffer(bufferId, content));
            }

            return extractedBuffers;
        }

        public static string FormatSourceCode(this string sourceCode, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension)
            {
                case ".cs":
                case ".csx":
                    return FormatSourceCodeCSharp(sourceCode);
                case ".fs":
                case ".fsx":
                    return FormatSourceCodeFSharp(sourceCode);
                default:
                    throw new InvalidOperationException($"Unsupported file extension '{extension}'");
            }
        }

        private static string FormatSourceCodeCSharp(string sourceCode)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode.Trim(), new CSharpParseOptions(kind: SourceCodeKind.Script));
            var cw = new AdhocWorkspace();
            var formattedCode = Formatter.Format(tree.GetRoot(), cw);
            return formattedCode.ToFullString();
        }

        private static string FormatSourceCodeFSharp(string sourceCode)
        {
            // dedent lines the number of spaces before the first non-space character
            var dedentedCode = sourceCode.TrimStart(' ');
            var dedentLevel = sourceCode.Length - dedentedCode.Length;
            var lines = sourceCode.Split('\n');
            var dedentedLines = lines.Select(l => l.Length > dedentLevel ? l.Substring(dedentLevel) : string.Empty);
            var formattedCode = string.Join("\n", dedentedLines);
            return formattedCode;
        }
    }
}
