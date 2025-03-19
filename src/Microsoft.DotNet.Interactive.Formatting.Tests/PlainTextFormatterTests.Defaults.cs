// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public sealed partial class PlainTextFormatterTests
{
    [TestClass]
    public class Defaults : FormatterTestBase
    {
        [TestMethod]
        public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type()
        {
            typeof(List<string>).ToDisplayString()
                                .Should().Be("System.Collections.Generic.List<System.String>");
            new List<string>().GetType().ToDisplayString()
                              .Should().Be("System.Collections.Generic.List<System.String>");
        }

        [TestMethod]
        public void Default_formatter_for_Type_displays_generic_parameter_name_for_multiple_parameter_generic_type()
        {
            typeof(Dictionary<string, IEnumerable<int>>).ToDisplayString()
                                                        .Should().Be(
                                                            "System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.IEnumerable<System.Int32>>");
        }

        [TestMethod]
        public void Default_formatter_for_Type_displays_generic_parameter_names_for_open_generic_types()
        {
            typeof(IList<>).ToDisplayString()
                           .Should().Be("System.Collections.Generic.IList<T>");
            typeof(IDictionary<,>).ToDisplayString()
                                  .Should().Be("System.Collections.Generic.IDictionary<TKey,TValue>");
        }

        [TestMethod]
        public void Custom_formatter_for_Type_can_be_registered()
        {
            Formatter.Register<Type>(t => t.GUID.ToString());

            GetType().ToDisplayString()
                     .Should().Be(GetType().GUID.ToString());
        }
        
        [TestMethod]
        public void Exception_StackTrace_is_included_by_default()
        {
            string msg;
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            try
            {
                throw ex;
            }
            catch (Exception thrownException)
            {
                msg = thrownException.ToDisplayString();
            }

            msg.Should()
               .Contain($"at {typeof(PlainTextFormatterTests)}.{nameof(Defaults)}.{MethodInfo.GetCurrentMethod().Name}");
        }

        [TestMethod]
        public void Exception_Type_is_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            var msg = ex.ToDisplayString();

            msg.Should().Contain("InvalidOperationException");
        }

        [TestMethod]
        public void Exception_Message_is_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            var msg = ex.ToDisplayString();

            msg.Should().Contain("oh noes!");
        }

        [TestMethod]
        public void Exception_InnerExceptions_are_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException("oh my.", new DataException("oops!")));

            ex.ToDisplayString()
              .Should()
              .Contain("NullReferenceException");
            ex.ToDisplayString()
              .Should()
              .Contain("DataException");
        }

        [TestMethod]
        public void When_ResetToDefault_is_called_then_default_formatters_are_immediately_reregistered()
        {
            var widget = new Widget { Name = "hola!" };

            var defaultValue = widget.ToDisplayString();

            Formatter.Register<Widget>(e => "hello!");

            widget.ToDisplayString().Should().NotBe(defaultValue);

            Formatter.ResetToDefault();

            widget.ToDisplayString().Should().Be(defaultValue);
        }
    }
}