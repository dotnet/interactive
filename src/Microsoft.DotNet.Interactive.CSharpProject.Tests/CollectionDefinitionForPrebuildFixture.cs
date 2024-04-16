// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[CollectionDefinition(nameof(PrebuildFixture), DisableParallelization = true)]
public class CollectionDefinitionForPrebuildFixture : ICollectionFixture<PrebuildFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}