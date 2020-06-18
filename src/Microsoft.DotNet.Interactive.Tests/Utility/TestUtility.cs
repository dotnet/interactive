// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Server;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public static class AssertionExtensions
    {
        public static GenericCollectionAssertions<T> AllSatisfy<T>(
            this GenericCollectionAssertions<T> assertions,
            Action<T> assert)
        {
            using var _ = new AssertionScope();

            foreach (var item in assertions.Subject)
            {
                assert(item);
            }

            return assertions;
        }

        public static void BeJsonEquivalentTo<T>(this StringAssertions assertion, T expected)
        {
            var obj = JsonConvert.DeserializeObject(assertion.Subject, expected.GetType());
            obj.Should().BeEquivalentTo(expected);
        }

        public static AndConstraint<GenericCollectionAssertions<T>> BeEquivalentSequenceTo<T>(
            this GenericCollectionAssertions<T> assertions,
            params object[] expectedValues)
        {
            var actualValues = assertions.Subject.ToArray();

            actualValues
                .Select(a => a?.GetType())
                .Should()
                .BeEquivalentTo(expectedValues.Select(e => e?.GetType()));

            using (new AssertionScope())
            {
                foreach (var tuple in actualValues
                                      .Zip(expectedValues, (actual, expected) => (actual, expected))
                                      .Where(t => t.expected == null || t.expected.GetType().GetProperties().Any()))
                {
                    tuple.actual
                         .Should()
                         .BeEquivalentTo(tuple.expected);
                }
            }

            return new AndConstraint<GenericCollectionAssertions<T>>(assertions);
        }

        public static AndConstraint<StringCollectionAssertions> BeEquivalentSequenceTo(
            this StringCollectionAssertions assertions,
            params string[] expectedValues)
        {
            return assertions.ContainInOrder(expectedValues).And.BeEquivalentTo(expectedValues);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<KernelCommand> should,
            Func<T, bool> where = null)
            where T : KernelCommand
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
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<SyntaxNodeOrToken> should,
            Func<T, bool> where = null)
            where T : SyntaxNodeOrToken
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
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<KernelEvent> should,
            Func<T, bool> where = null)
            where T : KernelEvent
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
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<IKernelEventEnvelope> should,
            Func<T, bool> where = null)
            where T : IKernelEventEnvelope
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
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndConstraint<GenericCollectionAssertions<KernelEvent>> NotContainErrors(
            this GenericCollectionAssertions<KernelEvent> should) =>
            should
                .NotContain(e => e is ErrorProduced)
                .And
                .NotContain(e => e is CommandFailed);

        public static AndWhichConstraint<ObjectAssertions, T> EventuallyContainSingle<T>(
            this GenericCollectionAssertions<KernelEvent> should,
            Func<T, bool> where = null,
            int timeout = 3000)
            where T : KernelEvent
        {
            return Task.Run(async () =>
            {
                if (where == null)
                {
                    where = _ => true;
                }

                var startTime = DateTime.UtcNow;
                var endTime = startTime + TimeSpan.FromMilliseconds(timeout);
                while (DateTime.UtcNow < endTime)
                {
                    if (should.Subject.OfType<T>().Any(where))
                    {
                        break;
                    }

                    await Task.Delay(200);
                }

                return should.ContainSingle<T>(where);
            }).Result;
        }
    }

    public static class ObservableExtensions
    {
        public static SubscribedList<T> ToSubscribedList<T>(this IObservable<T> source)
        {
            return new SubscribedList<T>(source);
        }
    }

    public class SubscribedList<T> : IReadOnlyList<T>, IDisposable
    {
        private ImmutableArray<T> _list = ImmutableArray<T>.Empty;
        private readonly IDisposable _subscription;

        public SubscribedList(IObservable<T> source)
        {
            _subscription = source.Subscribe(x => { _list = _list.Add(x); });
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>) _list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _list.Length;

        public T this[int index] => _list[index];

        public void Dispose() => _subscription.Dispose();
    }
}