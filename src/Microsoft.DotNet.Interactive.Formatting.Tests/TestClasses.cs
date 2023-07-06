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

        public Node(string id = null)
        {
            _id = id;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public IEnumerable<Node> Nodes { get; set; }

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

namespace Dummy
{
    public class ClassNotInSystemNamespace
    {
    }

    public class ClassWithNoPropertiesAndCustomToString
    {
        public override string ToString()
        {
            return $"{base.ToString()} custom ToString value";
        }
    }

    public class ClassWithManyProperties
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

    public class ClassWithManyPropertiesAndCustomToString
    {
        public override string ToString()
        {
            return $"{base.ToString()} custom ToString value";
        }

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

    public class ClassWithToStringThatThrows
    {
        public override string ToString()
        {
            throw new Exception("oops!");
        }
    }

    public class ClassImplementingIDictionary_of_int_string : IDictionary<int, string>
    {
        string IDictionary<int, string>.this[int key]
        {
            get => "one";
            set => throw new NotImplementedException();
        }

        ICollection<int> IDictionary<int, string>.Keys => new[] { 1 };

        ICollection<string> IDictionary<int, string>.Values => new[] { "one" };

        int ICollection<KeyValuePair<int, string>>.Count => 1;

        bool ICollection<KeyValuePair<int, string>>.IsReadOnly => true;

        void IDictionary<int, string>.Add(int key, string value)
        {
        }

        void ICollection<KeyValuePair<int, string>>.Add(KeyValuePair<int, string> item)
        {
        }

        void ICollection<KeyValuePair<int, string>>.Clear()
        {
        }

        bool ICollection<KeyValuePair<int, string>>.Contains(KeyValuePair<int, string> item)
        {
            return (item.Key == 1 && item.Value == "one");
        }

        bool IDictionary<int, string>.ContainsKey(int key)
        {
            return key == 1;
        }

        void ICollection<KeyValuePair<int, string>>.CopyTo(KeyValuePair<int, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<int, string>> IEnumerable<KeyValuePair<int, string>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<int, string>>)new[] { new KeyValuePair<int, string>(1, "one") }).GetEnumerator();
        }

        bool IDictionary<int, string>.Remove(int key)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<int, string>>.Remove(KeyValuePair<int, string> item)
        {
            return false;
        }

        bool IDictionary<int, string>.TryGetValue(int key, out string value)
        {
            value = "one";
            return (key == 1);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)new[] { new KeyValuePair<int, string>(1, "one") }).GetEnumerator();
        }
    }

    public class ClassWithPropertiesThatIsAlsoIEnumerable : IEnumerable
    {
        private readonly IEnumerable _values;

        public ClassWithPropertiesThatIsAlsoIEnumerable(IEnumerable values)
        {
            _values = values;
        }

        public string Property { get; set; } 

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    public class ClassWithPropertiesThatIsAlsoIEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public ClassWithPropertiesThatIsAlsoIEnumerable(IEnumerable<T> values)
        {
            _values = values;
        }

        public string Property { get; set; } 

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.GetEnumerator();
        public IEnumerator GetEnumerator() => _values.GetEnumerator();
    }
}