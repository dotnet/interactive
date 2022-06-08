// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class Widget
    {
        public Widget()
        {
            Name = "Default";
        }

        public string Name { get; set; }

        public List<Part> Parts { get; set; }
    }

    public class InheritedWidget : Widget
    {
    }

    public class Part
    {
        public string PartNumber { get; set; }
        public Widget Widget { get; set; }
    }

    public struct SomeStruct
    {
        public DateTime DateField;
        public DateTime DateProperty { get; set; }
    }

    public class SomePropertyThrows
    {
        public string Fine => "Fine";

        public string NotOk => throw new Exception("not ok");

        public string Ok => "ok";

        public string PerfectlyFine => "PerfectlyFine";
    }

    public struct EntityId
    {
        public EntityId(string typeName, string id) : this()
        {
            TypeName = typeName;
            Id = id;
        }

        public string TypeName { get; }
        public string Id { get; }
    }

    public class Node
    {
        private string _id;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public IEnumerable<Node> Nodes { get; set; }

        public Node[] NodesArray { get; set; }

        internal string InternalId => Id;
    }

    public class LinkedListNode
    {
        public LinkedListNode Next { get; set; }
    }

    public sealed class FeatureCollection : IEnumerable<IFeature>
    {
        private readonly List<IFeature> _features = new();
        public void Add(IFeature feature)
        {
            _features.Add(feature);
        }
        
        public IEnumerator<IFeature> GetEnumerator()
        {
            return _features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _features).GetEnumerator();
        }
    }

    public interface IFeature
    {
        Geometry Geometry { get; set; }
    }

    public class Feature : IFeature
    {
        private CustomGeometry _geometry;

        public Geometry Geometry
        {
            get => _geometry;
            set => _geometry = value as CustomGeometry;
        }
    }

    public abstract class Geometry
    {

    }

    public class CustomGeometry : Geometry
    {

    }
    public class GeometryCollection : Geometry, IEnumerable<Geometry>
    {
        private readonly List<Geometry> _list = new();

        public void Add(Geometry geometry)
        {
            _list.Add(geometry);
        }

        public IEnumerator<Geometry> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _list).GetEnumerator();
        }
    }

    public class SomethingWithLotsOfProperties
    {
        public DateTime DateProperty { get; set; }
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public Uri UriProperty { get; set; }
    }

    public class SomethingAWithStaticProperty
    {
        public static string StaticProperty { get; set; }

        public static string StaticField;
    }
}

namespace Dummy
{
    public class DummyNotInSystemNamespace
    {
    }

    public class DummyWithNoProperties
    {
    }

    public class DummyClassWithManyProperties
    {
        public int X1 { get; } = 1;
        public int X2 { get; } = 2;
        public int X3 { get; } = 3;
        public int X4 { get; } = 4;
        public int X5 { get; } = 5;
        public int X6 { get; } = 6;
        public int X7 { get; } = 7;
        public int X8 { get; } = 8;
        public int X9 { get; } = 9;
        public int X10 { get; } = 10;
        public int X11 { get; } = 11;
        public int X12 { get; } = 12;
        public int X13 { get; } = 13;
        public int X14 { get; } = 14;
        public int X15 { get; } = 15;
        public int X16 { get; } = 16;
        public int X17 { get; } = 17;
        public int X18 { get; } = 18;
        public int X19 { get; } = 19;
        public int X20 { get; } = 20;
        public int X21 { get; } = 21;
    }
}