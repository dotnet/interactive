// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Pocket;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public abstract class FeatureContainer : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Dictionary<string, object> _features = new(StringComparer.OrdinalIgnoreCase);
    private List<(string, object)> _featureProperties;

    public IReadOnlyDictionary<string, object> Features => _features;

    public void Dispose() => _disposables.Dispose();

    public void AddFeature(IRunResultFeature feature)
    {
        if (feature is IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        _features.Add(feature.Name, feature);
    }

    public List<(string Name, object Value)> FeatureProperties => _featureProperties ??= new List<(string, object)>(); // TODO: (FeatureProperties) replace tuple with a class

    public void AddProperty(string name, object value) => FeatureProperties.Add((name, value));
}