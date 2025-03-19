// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dummy;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class PlainTextFormatterTests
{
    [TestClass]
    public class Objects : FormatterTestBase
    {
        [TestMethod]
        public void Null_reference_types_are_indicated()
        {
            string value = null;

            value.ToDisplayString().Should().Be(Formatter.NullString);
        }

        [TestMethod]
        public void Null_nullables_are_indicated()
        {
            int? nullable = null;

            var output = nullable.ToDisplayString();

            output.Should().Be(Formatter.NullString);
        }

        [TestMethod]
        public void It_emits_the_property_names_and_values_for_a_specific_type()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Widget>();

            var writer = new StringWriter();
            formatter.Format(new Widget { Name = "Bob" }, writer);

            var s = writer.ToString();

            Console.WriteLine(s);

            s.Should().Contain("Name: Bob");
        }
        
        [TestMethod]
        public void When_Zero_properties_available_to_choose_just_ToString_is_used()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<ClassWithNoPropertiesAndCustomToString>();

            var writer = new StringWriter();
            formatter.Format(new ClassWithNoPropertiesAndCustomToString(), writer);

            var s = writer.ToString();
            s.Should().Be($"{typeof(ClassWithNoPropertiesAndCustomToString)} custom ToString value");
        }
        
        [TestMethod]
        [DataRow(typeof(Boolean), "False")]
        [DataRow(typeof(Byte), "0")]
        [DataRow(typeof(Decimal), "0")]
        [DataRow(typeof(Double), "0")]
        [DataRow(typeof(Guid), "00000000-0000-0000-0000-000000000000")]
        [DataRow(typeof(Int16), "0")]
        [DataRow(typeof(Int32), "0")]
        [DataRow(typeof(Int64), "0")]
        [DataRow(typeof(Single), "0")]
        [DataRow(typeof(UInt16), "0")]
        [DataRow(typeof(UInt32), "0")]
        [DataRow(typeof(UInt64), "0")]
        public void It_does_not_expand_properties_of_scalar_types(Type type, string expected)
        {
            var value = Activator.CreateInstance(type);

            value.ToDisplayString().Should().Be(expected);
        }

        [TestMethod]
        public void It_expands_properties_of_structs()
        {
            var id = new EntityId("the typename", "the id");

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(id.GetType());

            var formatted = id.ToDisplayString(formatter);

            formatted.Should()
                     .Contain("TypeName: the typename")
                     .And
                     .Contain("Id: the id");
        }

        [TestMethod]
        public void Anonymous_types_are_automatically_fully_formatted()
        {
            var ints = new[] { 3, 2, 1 };

            var obj = new { ints, count = ints.Length };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(obj.GetType());

            var output = obj.ToDisplayString(formatter);

            output.Should().Match(@"<>f__AnonymousType*<Int32[],Int32>
    ints: [ 3, 2, 1 ]
    count: 3".ReplaceLineEndings());
        }

        [TestMethod]
        public void Formatter_expands_properties_of_ExpandoObjects()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "socks";
            expando.Parts = null;

            var formatter = PlainTextFormatter.GetPreferredFormatterFor<ExpandoObject>();

            var expandoString = ((object)expando).ToDisplayString(formatter);

            expandoString.Should().Be(@"Name: socks
Parts: <null>".ReplaceLineEndings());
        }

        [TestMethod]
        public void When_a_property_throws_it_does_not_prevent_other_properties_from_being_written()
        {
            var log = new SomePropertyThrows().ToDisplayString();

            log.Should().Contain("Ok:");
            log.Should().Contain("Fine:");
            log.Should().Contain("PerfectlyFine:");
        }

        [TestMethod]
        public void When_a_property_throws_then_then_exception_is_written_in_place_of_the_property_and_indented()
        {
            var log = new SomePropertyThrows().ToDisplayString();

            log.Should().Match(
                """
                SomePropertyThrows
                      Fine: Fine
                      NotOk: System.Exception: not ok
                      at Microsoft.DotNet.Interactive.Formatting.Tests.SomePropertyThrows.get_NotOk()*
                      at lambda_method*(Closure, SomePropertyThrows)
                      at Microsoft.DotNet.Interactive.Formatting.MemberAccessor`1.GetValueOrException(T instance)*
                      Ok: ok
                      PerfectlyFine: PerfectlyFine
                """.ReplaceLineEndings());

            /*
Xunit.Sdk.XunitException: Expected log to match "

SomePropertyThrows
    Fine: Fine
    NotOk: System.Exception: not ok
    at Microsoft.DotNet.Interactive.Formatting.Tests.SomePropertyThrows.get_NotOk()*
    at lambda_method*(Closure, SomePropertyThrows)
    at Microsoft.DotNet.Interactive.Formatting.MemberAccessor`1.GetValueOrException(T instance)*
    Ok: ok
    PerfectlyFine: PerfectlyFine", but "
    
    SomePropertyThrows
      Fine: Fine
      NotOk: System.Exception: not ok
      at Microsoft.DotNet.Interactive.Formatting.Tests.SomePropertyThrows.get_NotOk() in C:\dev\interactive\src\Microsoft.DotNet.Interactive.Formatting.Tests\TestClasses.cs:line 41
      at lambda_method3(Closure, SomePropertyThrows)
      at Microsoft.DotNet.Interactive.Formatting.MemberAccessor`1.GetValueOrException(T instance) in C:\dev\interactive\src\Microsoft.DotNet.Interactive.Formatting\MemberAccessor{T}.cs:line 57
      Ok: ok
      PerfectlyFine: PerfectlyFine" does not.
   at FluentAssertions.Execution.XUnit2TestFramework.Throw(String message) in /_/Src/FluentAssertions/Execution/XUnit2TestFramework.cs:line 35
   at FluentAssertions.Execution.TestFrameworkProvider.Throw(String message) in /_/Src/FluentAssertions/Execution/TestFrameworkProvider.cs:line 34
   at FluentAssertions.Execution.DefaultAssertionStrategy.HandleFailure(String message) in /_/Src/FluentAssertions/Execution/DefaultAssertionStrategy.cs:line 25
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc) in /_/Src/FluentAssertions/Execution/AssertionScope.cs:line 274
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc) in /_/Src/FluentAssertions/Execution/AssertionScope.cs:line 246
   at FluentAssertions.Execution.AssertionScope.FailWith(String message, Object[] args) in /_/Src/FluentAssertions/Execution/AssertionScope.cs:line 296
   at FluentAssertions.Primitives.StringWildcardMatchingValidator.ValidateAgainstMismatch() in /_/Src/FluentAssertions/Primitives/StringWildcardMatchingValidator.cs:line 22
   at FluentAssertions.Primitives.StringValidator.Validate() in /_/Src/FluentAssertions/Primitives/StringValidator.cs:line 46
   at FluentAssertions.Primitives.StringAssertions`1.Match(String wildcardPattern, String because, Object[] becauseArgs) in /_/Src/FluentAssertions/Primitives/StringAssertions.cs:line 220
   at Microsoft.DotNet.Interactive.Formatting.Tests.PlainTextFormatterTests.Objects.When_a_property_throws_then_then_exception_is_written_in_place_of_the_property_and_indented() in C:\dev\interactive\src\Microsoft.DotNet.Interactive.Formatting.Tests\PlainTextFormatterTests.cs:line 209
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)


    */
        }
        
        [TestMethod]
        public void Recursive_formatter_calls_do_not_cause_exceptions()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new() { Widget = widget } };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(widget.GetType());

            widget.Invoking(w => w.ToDisplayString(formatter)).Should().NotThrow();
        }

        [TestMethod]
        public void Formatter_does_not_expand_string()
        {
            var widget = new Widget
            {
                Name = "hello"
            };
            widget.Parts = new List<Part> { new() { Widget = widget } };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Widget>();

            // this should not throw
            var s = widget.ToDisplayString(formatter);

            s.Should()
             .Contain("hello")
             .And
             .NotContain("{ h },{ e }");
        }

        [TestMethod]
        public void Static_fields_are_not_written()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Widget>();
            new Widget().ToDisplayString(formatter)
                        .Should().NotContain(nameof(SomethingAWithStaticProperty.StaticField));
        }

        [TestMethod]
        public void Static_properties_are_not_written()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Widget>();
            new Widget().ToDisplayString(formatter)
                        .Should().NotContain(nameof(SomethingAWithStaticProperty.StaticProperty));
        }

        [TestMethod]
        public void It_expands_fields_of_objects()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<SomeStruct>();
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);
            var id = new SomeStruct
            {
                DateField = today,
                DateProperty = tomorrow
            };

            var output = id.ToDisplayString(formatter);

            output.Should().Contain("DateField: ");
            output.Should().Contain("DateProperty: ");
        }
        
        [TestMethod]
        public void Tuple_values_are_formatted_on_one_line_when_all_scalar()
        {
            var tuple = Tuple.Create(123, "Hello");

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(tuple.GetType());

            var formatted = tuple.ToDisplayString(formatter);

            Console.WriteLine(formatted);

            formatted.Should().Be("( 123, Hello )");
        }

        [TestMethod]
        public void ValueTuple_values_are_formatted_on_one_line_when_all_scalar()
        {
            var tuple = (123, "Hello");

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(tuple.GetType());

            var formatted = tuple.ToDisplayString(formatter);

            formatted.Should().Be("( 123, Hello )");
        }

        [TestMethod]
        public void Tuple_values_are_formatted_as_multi_line_when_not_all_scalar()
        {
            var tuple = Tuple.Create(123, "Hello", Enumerable.Range(1, 3));

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(tuple.GetType());

            var formatted = tuple.ToDisplayString(formatter).Replace("\r", "");

            formatted.Should().Be(@"  - 123
  - Hello
  - [ 1, 2, 3 ]".Replace("\r", ""));
        }

        [TestMethod]
        public void ValueTuple_values_are_formatted_as_multi_line_when_not_all_scalar()
        {
            var tuple = (123, "Hello", Enumerable.Range(1, 3));

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(tuple.GetType());

            var formatted = tuple.ToDisplayString(formatter).Replace("\r", "");

            formatted.Should().Be(@"  - 123
  - Hello
  - [ 1, 2, 3 ]".Replace("\r", ""));
        }

        [TestMethod]
        public void Enums_are_formatted_using_their_names()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(FileAccess));

            var writer = new StringWriter();

            formatter.Format(FileAccess.ReadWrite, writer);

            writer.ToString().Should().Be("ReadWrite");
        }

        [TestMethod]
        public void TimeSpan_is_not_destructured()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(TimeSpan));

            var writer = new StringWriter();

            var timespan = 25.Milliseconds();

            formatter.Format(timespan, writer);

            writer.ToString().Should().Be(timespan.ToString());
        }

        [TestMethod]
        public void PlainTextFormatter_returns_plain_for_BigInteger()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(BigInteger));

            var writer = new StringWriter();

            var instance = BigInteger.Parse("78923589327589332402359");

            formatter.Format(instance, writer);

            writer.ToString()
                  .Should()
                  .Be("78923589327589332402359");
        }

        [TestMethod]
        public void Properties_that_are_complex_objects_are_shown_indented()
        {
            var obj = new
            {
                PropertyA = "A",
                PropertyB = new
                {
                    PropertyB1 = "B.1",
                    PropertyB2 = "B.2"
                },
                PropertyC = "C"
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(obj.GetType());

            var writer = new StringWriter();
            formatter.Format(obj, writer);

            var formatted = writer.ToString();

            formatted.Should().Match(
                """
                <>f__AnonymousType*<String,<>f__AnonymousType*<String,String>,String>
                    PropertyA: A
                    PropertyB: <>f__AnonymousType*<String,String>
                      PropertyB1: B.1
                      PropertyB2: B.2
                    PropertyC: C
                """.ReplaceLineEndings());
        }

        [TestMethod]
        public void Sequences_of_complex_objects_are_shown_indented()
        {
            var obj = new
            {
                PropertyA = "A",
                PropertyB = new[]
                {
                    new
                    {
                        IntProperty = 1,
                        StringProperty = "one"
                    },
                    new
                    {
                        IntProperty = 2,
                        StringProperty = "two"
                    }
                },
                PropertyC = "C"
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(obj.GetType());

            var writer = new StringWriter();
            formatter.Format(obj, writer);

            writer.ToString().Should().Match("""
                <>f__AnonymousType*<String,<>f__AnonymousType*<Int32,String>[],String>
                    PropertyA: A
                    PropertyB: <>f__AnonymousType*<Int32,String>[]
                      - IntProperty: 1
                        StringProperty: one
                      - IntProperty: 2
                        StringProperty: two
                    PropertyC: C
                """.ReplaceLineEndings());
        }

        [TestMethod]
        [Ignore("TODO")]
        public void Complex_objects_within_a_sequence_are_indented()
        {
            var obj = new object[]
            {
                1,
                new
                {
                    PropertyA = "A",
                    PropertyB = "B"
                },
                3
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(obj.GetType());

            var writer = new StringWriter();
            formatter.Format(obj, writer);
            
            writer.ToString().Should().Match(@"
Object[]
  - 1
  - PropertyA: A
    PropertyB: B
  - 3".ReplaceLineEndings());
        }
    }
}