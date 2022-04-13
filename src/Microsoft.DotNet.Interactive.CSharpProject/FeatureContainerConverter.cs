// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject;

internal abstract class FeatureContainerConverter<T> : JsonConverter where T : FeatureContainer
{
    protected abstract void AddProperties(T result, JObject o);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is T result)
        {
            var o = new JObject();

            AddProperties(result, o);

            foreach (var feature in result.Features.Values.OfType<IRunResultFeature>())
            {
                feature.Apply(result);
            }

            foreach (var property in result.FeatureProperties.OrderBy(p => p.Name))
            {
                var jToken = JToken.FromObject(property.Value, serializer);
                o.Add(new JProperty(property.Name, jToken));
            }


            o.WriteTo(writer);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
        throw new NotImplementedException();

    public override bool CanRead => false;

    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType) => objectType == typeof(RunResult);
}