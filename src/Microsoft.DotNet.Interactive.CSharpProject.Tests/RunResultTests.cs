// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Pocket;
using Recipes;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class RunResultTests
{
    [TestMethod]
    public void Disposable_RunResult_features_are_disposed_when_RunResult_is_disposed()
    {
        var wasDisposed = false;

        var result = new RunResult(true);

        result.AddFeature(new DisposableFeature(  Disposable.Create(() => wasDisposed = true)));

        result.Dispose();

        wasDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void Features_can_add_object_properties_to_serialized_RunResult_by_implementing_IAugmentRunResult()
    {
        var result = new RunResult(true);

        result.AddFeature(new TestFeature<TestClass>("object", new TestClass("a", 1)));

        var json = result.ToJson();

        var obj = json.FromJsonTo<JObject>().Property("object").Value.ToObject<TestClass>();

        obj.StringProperty.Should().Be("a");
        obj.IntProperty.Should().Be(1);
    }

    [TestMethod]
    public void Features_can_add_string_properties_to_serialized_RunResult_by_implementing_IAugmentRunResult()
    {
        var result = new RunResult(true);

        result.AddFeature(new TestFeature<string>("string", "here i am!"));

        var json = result.ToJson().FromJsonTo<dynamic>();

        var scalar = (string) json.@string;

        scalar.Should().Be("here i am!");
    }

    [TestMethod]
    public void Features_can_add_int_properties_to_serialized_RunResult_by_implementing_IAugmentRunResult()
    {
        var result = new RunResult(true);

        result.AddFeature(new TestFeature<int>("int", 123));

        var json = result.ToJson().FromJsonTo<dynamic>();

        var scalar = (int) json.@int;

        scalar.Should().Be(123);
    }

    [TestMethod]
    public void Features_can_add_array_properties_to_serialized_RunResult_by_implementing_IAugmentRunResult()
    {
        var result = new RunResult(true);

        result.AddFeature(new TestFeature<string[]>("array", ["one", "two", "three"]));

        var json = result.ToJson().FromJsonTo<JObject>();

        var array = json.Property("array").Value.ToObject<string[]>();

        array.Should().Equal("one", "two", "three");
    }

    public class TestClass
    {
        public TestClass(string stringProperty, int intProperty)
        {
            StringProperty = stringProperty;
            IntProperty = intProperty;
        }

        public string StringProperty { get; }
        public int IntProperty { get; }
    }

    private class TestFeature<T> : IRunResultFeature
    {
        private readonly string name;
        private readonly T value;

        public TestFeature(string name, T value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name => value.GetType().Name;

        public void Apply(FeatureContainer result)
        {
            result.AddProperty(name, value);
        }
    }

    public class DisposableFeature : IRunResultFeature, IDisposable
    {
        private readonly IDisposable disposable;

        public string Name => nameof(DisposableFeature);

        public DisposableFeature(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            disposable?.Dispose();
        }

        public void Apply(FeatureContainer result)
        {
        }
    }
}