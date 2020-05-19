// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public static class AssertionsExtensions
    {
        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<PubSubMessage> should,
            Func<T, bool> where = null)
            where T : PubSubMessage
        {
            T subject;

            if (where == null)
            {
                should.ContainSingle(e => e is T);

                subject = should.Subject
                    .OfType<T>()
                    .Single();
            }
            else
            {
                should.ContainSingle(e => e is T && where((T)e));

                subject = should.Subject
                    .OfType<T>()
                    .Where(where)
                    .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }
        
        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<ReplyMessage> should,
            Func<T, bool> where = null)
            where T : ReplyMessage
        {
            T subject;

            if (where == null)
            {
                should.ContainSingle(e => e is T);

                subject = should.Subject
                    .OfType<T>()
                    .Single();
            }
            else
            {
                should.ContainSingle(e => e is T && where((T)e));

                subject = should.Subject
                    .OfType<T>()
                    .Where(where)
                    .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }
    }
}