// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class PipelineStepTests
{
    [Fact]
    public async Task It_produces_a_new_value_when_there_is_none()
    {
        var producer = new PipelineStep<string>(() => Task.FromResult("something"));
        var value = await producer.GetLatestAsync();
        value.Should().Be("something");
    }

    [Fact]
    public async Task It_produces_a_new_value_when_invalidated()
    {
        var seed = 0;
        var producer = new PipelineStep<int>(() => Task.FromResult(Interlocked.Increment(ref seed)));
        var value = await producer.GetLatestAsync();
        value.Should().Be(1);

        producer.Invalidate();
        var newValue = await producer.GetLatestAsync();
        newValue.Should().Be(2);
    }

    [Fact]
    public async Task It_propagates_exception()
    {
        var producer = new PipelineStep<int>(() => throw new InvalidOperationException());
        await producer.Awaiting(p => p.GetLatestAsync())
            .Should()
            .ThrowAsync<InvalidOperationException>();

    }

    [Fact]
    public async Task It_remains_invalid_if_exceptions_are_thrown()
    {
        var seed = 0;
        var producer = new PipelineStep<int>(() =>
        {
            var next = Interlocked.Increment(ref seed);
            if (next == 1)
            {
                throw new InvalidOperationException();
            }
            return Task.FromResult(next);
        });

        await producer.Awaiting(p => p.GetLatestAsync())
            .Should()
            .ThrowAsync<InvalidOperationException>();

        var value = await producer.GetLatestAsync();
        value.Should().Be(2);

    }

    [Fact]
    public async Task It_does_not_produces_a_new_value_when_invalidated_until_asked_for_latest_value()
    {
        var seed = 0;
        var producer = new PipelineStep<int>(() => Task.FromResult(Interlocked.Increment(ref seed)));
        var value = await producer.GetLatestAsync();
        value.Should().Be(1);
        producer.Invalidate();
        seed.Should().Be(1);
    }

    [Fact]
    public async Task It_retains_the_latest_value()
    {
        var seed = 0;
        var producer = new PipelineStep<int>(() => Task.FromResult(Interlocked.Increment(ref seed)));
        var value1 = await producer.GetLatestAsync();
        var value2 = await producer.GetLatestAsync();
        var value3 = await producer.GetLatestAsync();
        value1.Should().Be(1);
        value2.Should().Be(1);
        value3.Should().Be(1);
    }

    [Fact]
    public async Task It_returns_same_value_to_concurrent_requests()
    {
        var seed = 0;
        var barrier = new Barrier(3);
        var producer = new PipelineStep<int>(() => Task.FromResult(++seed));

        var firstConsumer = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return producer.GetLatestAsync();
        });

        var secondConsumer = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return producer.GetLatestAsync();
        });

        var thirdConsumer = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return producer.GetLatestAsync();
        });

        var values = await Task.WhenAll(firstConsumer, secondConsumer, thirdConsumer);

        values.Should().HaveCount(3).And.OnlyContain(i => i == 1);
    }

    [Fact(Skip = "flaky")]
    public async Task When_invalidated_while_producing_a_value_the_consumer_waiting_will_wait_for_latest_production_to_be_finished()
    {
        var seed = 0;
        var barrier = new Barrier(2);

        PipelineStep<int> producer = null;

        producer = new PipelineStep<int>(() =>
        {
            // will require all consumer to reach this point to move on
            barrier.SignalAndWait(3.Seconds());
            barrier.RemoveParticipant();

            producer.Invalidate();

            return Task.FromResult(Interlocked.Increment(ref seed));
        });

        var values = await Task.WhenAll(
                         producer.GetLatestAsync(),
                         producer.GetLatestAsync());

        values.Should().BeEquivalentTo(new[] { 2, 2 });
    }

    [Fact]
    public async Task Sequence_of_steps_produce_a_value()
    {
        var step1 = new PipelineStep<int>(() => Task.FromResult(1));
        var step2 = step1.Then((number) => Task.FromResult($"{number} {number}"));
        var value1 = await step2.GetLatestAsync();
        value1.Should().Be("1 1");
    }

    [Fact]
    public async Task Invalidating_a_step_in_a_sequence_causes_only_that_step_to_re_evaluate()
    {
        var seed1 = 0;
        var step1 = new PipelineStep<int>(() => Task.FromResult(Interlocked.Increment(ref seed1)));
        var seed2 = 0;
        var step2 = step1.Then((number) => Task.FromResult($"{number} {Interlocked.Increment(ref seed2)}"));
        await step2.GetLatestAsync();

        step2.Invalidate();
        var value = await step2.GetLatestAsync();
        value.Should().Be("1 2");
    }

    [Fact]
    public async Task Invalidating_a_step_in_a_sequence_causes_all_successor_to_evaluate()
    {
        var seed1 = 0;
        var seed2 = 0;
        var seed3 = 0;
        var step1 = new PipelineStep<int>(() => Task.FromResult(Interlocked.Increment(ref seed1)));
        var step2 = step1.Then((number) => Task.FromResult($"{number} {Interlocked.Increment(ref seed2)}"));
        var step3 = step2.Then((text) => Task.FromResult($"{text} {Interlocked.Increment(ref seed3)}"));
        await step3.GetLatestAsync();

        step2.Invalidate();
        var value = await step3.GetLatestAsync();
        value.Should().Be("1 2 2");
    }
}