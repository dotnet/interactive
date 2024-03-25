// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

public static class InstrumentationLineMapper
{
    public static async Task<(AugmentationMap, VariableLocationMap)> MapLineLocationsRelativeToViewportAsync(
        AugmentationMap augmentationMap,
        VariableLocationMap locations,
        Document document,
        Viewport viewport = null)
    {
        if (viewport == null)
        {
            return (augmentationMap, locations);
        }

        var text = await document.GetTextAsync();
        var viewportSpan = viewport.Region.ToLinePositionSpan(text);

        var mappedAugmentations = MapAugmentationsToViewport();
        var mappedLocations = MapVariableLocationsToViewport();

        return (mappedAugmentations, mappedLocations);

        AugmentationMap MapAugmentationsToViewport()
        {
            var augmentations = augmentationMap.Data.Values
                .Where(augmentation => viewportSpan.ContainsLine((int) augmentation.CurrentFilePosition.Line))
                .Select(augmentation => MapAugmentationToViewport(augmentation, viewportSpan));

            return new AugmentationMap(augmentations.ToArray());
        }

        VariableLocationMap MapVariableLocationsToViewport()
        {
            var variableLocationDictionary = locations.Data.ToDictionary(
                kv => kv.Key,
                kv =>
                {
                    var variableLocations = kv.Value;
                    return new HashSet<VariableLocation>(variableLocations
                        .Where(loc => viewportSpan.ContainsLine(loc.StartLine) &&
                                      viewportSpan.ContainsLine(loc.EndLine))
                        .Select(location => MapVariableLocationToViewport(location, viewportSpan)));
                },
                SymbolEqualityComparer.Default
            );

            return new VariableLocationMap
            {
                Data = variableLocationDictionary
            };
        }
    }

    private static long CalculateOffset(long line, LinePositionSpan viewportSpan)
    {
        var firstLineInViewport = viewportSpan.Start.Line + 1;
        return line - firstLineInViewport;
    }

    private static Augmentation MapAugmentationToViewport(Augmentation input, LinePositionSpan viewportSpan) => input.withPosition(
        new FilePosition
        {
            Line = CalculateOffset(input.CurrentFilePosition.Line, viewportSpan),
            Character = input.CurrentFilePosition.Character,
            File = input.CurrentFilePosition.File
        }
    );

    private static VariableLocation MapVariableLocationToViewport(
        VariableLocation input,
        LinePositionSpan viewportSpan) => new(
        input.Variable,
        (int)CalculateOffset(input.StartLine, viewportSpan),
        (int)CalculateOffset(input.EndLine, viewportSpan),
        input.StartColumn,
        input.EndColumn
    );

    public static IEnumerable<Viewport> FilterActiveViewport(IEnumerable<Viewport> viewports, BufferId activeBufferId)
    {
        return viewports.Where(viewport => viewport.Destination.Name == activeBufferId.FileName && viewport.BufferId == activeBufferId.ToString());
    }
}