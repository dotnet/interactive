// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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