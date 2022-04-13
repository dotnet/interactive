// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    [JsonConverter(typeof(BufferIdConverter))]
    public class BufferId
    {
        private const string ReplaceInjectionModifier = "[replace]";
        private const string BeforeInjectionModifier = "[before]";
        private const string AfterInjectionModifier = "[after]";

        public BufferId(string fileName, string regionName = null)
        {
            FileName = fileName ?? "";
            RegionName = regionName;
        }

        public string FileName { get; }

        public string RegionName { get; }

        public override bool Equals(object obj)
        {
            var other = obj as BufferId;
            return other != null &&
                   FileName == other.FileName &&
                   RegionName == other.RegionName;
        }

        public override int GetHashCode()
        {
            var hashCode = 1013130118;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RegionName);
            return hashCode;
        }

        public static bool operator ==(BufferId left, BufferId right) => Equals(left, right);

        public static bool operator !=(BufferId left, BufferId right) => !Equals(left, right);

        public override string ToString() => string.IsNullOrWhiteSpace(RegionName)
                                                 ? FileName
                                                 : $"{FileName}@{RegionName}";

        public static BufferId Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Empty;
            }

            var parts = value.Split('@');

            return new BufferId(parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : null);
        }

        public static implicit operator BufferId(string value)
        {
            return Parse(value);
        }

        private static string RemoveInjectionModifiers(string regionName)
        {
            return string.IsNullOrWhiteSpace(regionName)
                ? regionName
                : regionName
                    .Replace(ReplaceInjectionModifier, string.Empty)
                    .Replace(BeforeInjectionModifier, string.Empty)
                    .Replace(AfterInjectionModifier, string.Empty);
        }
        public BufferId GetNormalized()
        {
            return new BufferId(FileName, RemoveInjectionModifiers(RegionName));
        }

        internal BufferInjectionPoints GetInjectionPoint()
        {
            if (string.IsNullOrWhiteSpace(RegionName))
            {
                return BufferInjectionPoints.Replace;
            }

            if (RegionName.Contains(BeforeInjectionModifier))
            {
                return BufferInjectionPoints.Before;
            }

            if (RegionName.Contains(AfterInjectionModifier))
            {
                return BufferInjectionPoints.After;
            }

            return BufferInjectionPoints.Replace;
        }
        public static BufferId Empty { get; } = new BufferId("");

        internal class BufferIdConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return Parse(reader?.Value?.ToString());
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(BufferId);
            }
        }
    }
}
