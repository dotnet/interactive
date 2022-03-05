// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation
{
    public class InstrumentationSyntaxVisitorTests
    {
        private async Task<AugmentationMap> GetAugmentationMapAsync(string source, IEnumerable<TextSpan> regions = null)
        {
            var document = Sources.GetDocument(source, true);

            var semanticModel = await document.GetSemanticModelAsync();

            if (regions == null)
            {
                return new InstrumentationSyntaxVisitor(document, semanticModel).Augmentations;
            }
            else
            {
                return new InstrumentationSyntaxVisitor(document, semanticModel, regions).Augmentations;
            }
        }

        [Fact]
        public async Task Instrumentation_Is_Not_Produced_When_There_Are_No_Statements()
        {
            var augmentations = (await GetAugmentationMapAsync(Sources.empty)).Data;
            Assert.Empty(augmentations);
        }

        [Fact]
        public async Task Instrumentation_Is_Empty_When_There_Is_No_State()
        {
            var augmentations = (await GetAugmentationMapAsync(Sources.simple)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations);
            Assert.Empty(augmentations[0].Fields);
            Assert.Empty(augmentations[0].Locals);
            Assert.Empty(augmentations[0].Parameters);
        }

        [Fact]
        public async Task Single_Statement_Is_Instrumented_In_Single_Statement_Program()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.simple)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations);
            Assert.Equal(@"Console.WriteLine(""Entry Point"");", augmentations[0].AssociatedStatement.ToString());
        }

        [Fact]
        public async Task Multiple_Statements_Are_Instrumented_In_Multiple_Statement_Program()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withLocalsAndParams)).Data.Values.ToList();

            //assert
            Assert.Equal(2, augmentations.Count);
            Assert.Equal(@"int a = 0;", augmentations[0].AssociatedStatement.ToString());
            Assert.Equal(@"Console.WriteLine(""Entry Point"");", augmentations[1].AssociatedStatement.ToString());
        }

        [Fact]
        public async Task Only_Requested_Statements_Are_Instrumented_When_Regions_Are_Supplied()
        {
            //arrange
            var regions = new List<TextSpan> { new TextSpan(169, 84) };

            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withMultipleMethodsAndComplexLayout, regions)).Data.Values.ToList();

            //assert
            Assert.Equal(2, augmentations.Count);
            Assert.Equal(@"Console.WriteLine(""Entry Point"");", augmentations[0].AssociatedStatement.ToString());
            Assert.Equal(@"var p = new Program();", augmentations[1].AssociatedStatement.ToString());
        }

        [Fact]
        public async Task Only_Requested_Statements_Are_Instrumented_When_Non_Contiguous_Regions_Are_Supplied()
        {
            //arrange
            var regions = new List<TextSpan> { new TextSpan(156, 35), new TextSpan(625, 32) };

            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withMultipleMethodsAndComplexLayout, regions)).Data.Values.ToList();

            //assert
            Assert.Equal(2, augmentations.Count);
            Assert.Equal(@"Console.WriteLine(""Entry Point"");", augmentations[0].AssociatedStatement.ToString());
            Assert.Equal(@"Console.WriteLine(""Instance"");", augmentations[1].AssociatedStatement.ToString());
        }

        [Fact]
        public async Task Locals_Are_Captured()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withLocalsAndParams)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations[1].Locals);
            Assert.Contains(augmentations[1].Locals, l => l.Name == "a");
        }

        [Fact]
        public async Task Locals_Are_Captured_After_Being_Assigned()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withNonAssignedLocals)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations[3].Locals);
            Assert.Equal(2, augmentations[4].Locals.Count());
            Assert.Contains(augmentations[4].Locals, l => l.Name == "s");
        }

        [Fact]
        public async Task Locals_Are_Not_Captured_Before_Being_Assigned()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withNonAssignedLocals)).Data.Values.ToList();

            //assert
            Assert.Empty(augmentations[1].Locals);
            Assert.Single(augmentations[2].Locals);
            Assert.Contains(augmentations[2].Locals, l => l.Name == "a");
        }

        [Fact]
        public async Task Locals_Are_Captured_Based_On_Scope()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withMultipleMethodsAndComplexLayout)).Data.Values.ToList();

            //assert
            Assert.NotEmpty(augmentations[6].Locals);
            Assert.Contains(augmentations[6].Locals, l => l.Name == "j");
            Assert.DoesNotContain(augmentations[6].Locals, l => l.Name == "k");
        }

        [Fact]
        public async Task RangeVariables_Are_Captured_As_Locals_Inside_Loops()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withMultipleMethodsAndComplexLayout)).Data.Values.ToList();

            //assert
            Assert.NotEmpty(augmentations[7].Locals);
            Assert.Contains(augmentations[7].Locals, l => l.Name == "i");
            Assert.DoesNotContain(augmentations[5].Locals, l => l.Name == "i");
            Assert.DoesNotContain(augmentations[6].Locals, l => l.Name == "i");
        }

        [Fact]
        public async Task ForEachVariables_Are_Captured_As_Locals_Inside_Loops()
        {
            //act
            var augmentations = (await GetAugmentationMapAsync(Sources.withMultipleMethodsAndComplexLayout)).Data.Values.ToList();

            //assert
            Assert.NotEmpty(augmentations[8].Locals);
            Assert.Contains(augmentations[8].Locals, l => l.Name == "number");
            Assert.DoesNotContain(augmentations[6].Locals, l => l.Name == "i");
            Assert.DoesNotContain(augmentations[7].Locals, l => l.Name == "number");
        }

        [Fact]
        public async Task Parameters_Are_Captured()
        {
            var augmentations = (await GetAugmentationMapAsync(Sources.withLocalsAndParams)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations[0].Parameters);
            Assert.Contains(augmentations[0].Parameters, p => p.Name == "args");
        }

        [Fact]
        public async Task Static_Fields_Are_Captured_In_Static_Methods()
        {
            //arrange
            var document = Sources.GetDocument(Sources.withStaticAndNonStaticField, true);
            InstrumentationSyntaxVisitor visitor = new InstrumentationSyntaxVisitor(document, await document.GetSemanticModelAsync());

            //act
            var augmentations = visitor.Augmentations.Data.Values.ToList();

            //assert
            Assert.Single(augmentations[0].Fields);
            Assert.Contains(augmentations[0].Fields, f => f.Name == "a");
        }

        [Fact]
        public async Task Static_Fields_Are_Captured_In_Instance_Methods()
        {
            //arrange
            var augmentations = (await GetAugmentationMapAsync(Sources.withStaticAndNonStaticField)).Data.Values.ToList();

            //assert
            Assert.NotEmpty(augmentations[1].Fields);
            Assert.Contains(augmentations[1].Fields, f => f.Name == "a");
        }

        [Fact]
        public async Task Instance_Fields_Are_Captured_In_Instance_Methods()
        {
            //arrange
            var augmentations = (await GetAugmentationMapAsync(Sources.withStaticAndNonStaticField)).Data.Values.ToList();

            //assert
            Assert.NotEmpty(augmentations[1].Fields);
            Assert.Contains(augmentations[1].Fields, f => f.Name == "b");
        }

        [Fact]
        public async Task Instance_Fields_Are_Not_Captured_In_Static_Methods()
        {
            //arrange
            var augmentations = (await GetAugmentationMapAsync(Sources.withStaticAndNonStaticField)).Data.Values.ToList();

            //assert
            Assert.Single(augmentations[0].Fields);
            Assert.DoesNotContain(augmentations[0].Fields, f => f.Name == "b");
        }
    }
}
