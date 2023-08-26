// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public interface IPipelineStep
{
    void Invalidate();
}

public class PipelineStep<T> : IPipelineStep
{
    private readonly Func<Task<T>> _createValue;
    private TaskCompletionSource<T> _latestValue = new();
    private Task<T> _inFlight;
    private readonly object _lock = new();
    private bool _invalidated = true;
    private Guid _operationId;
    private IPipelineStep _nextStep;

    public PipelineStep(Func<Task<T>> createValue)
    {
        if (createValue == null) throw new ArgumentNullException(nameof(createValue));

        _createValue = async () =>
        {
            await Task.Yield();
            return await createValue();
        };
    }

    public PipelineStep<U> Then<U>(Func<T, Task<U>> nextStep)
    {
        var newStep= new PipelineStep<U>(async () =>
        {
            var previousStepValue = await GetLatestAsync();
            return await nextStep(previousStepValue);
        });

        _nextStep = newStep;
        return newStep;
    }

    public PipelineStep<U> Then<U>(Func<T, U> nextStep)
    {
        var newStep = new PipelineStep<U>(async () =>
        {
            var previousStepValue = await GetLatestAsync();
            return nextStep(previousStepValue);
        });

        _nextStep = newStep;
        return newStep;
    }

    public Task<T> GetLatestAsync()
    {
        lock (_lock)
        {
            if (_invalidated)
            {
                if (_inFlight == null)
                {
                    _latestValue = new TaskCompletionSource<T>();
                    _inFlight = _createValue();
                }
                else
                {
                    _inFlight = _inFlight
                        .ContinueWith(t =>  _createValue().Result);
                }
                var newId = Guid.NewGuid();
                _operationId = newId;
                _inFlight.ContinueWith(t =>
                {
                    lock (_lock)
                    {
                        switch (t.Status)
                        {
                            case TaskStatus.Faulted:

                                if (_operationId == newId)
                                {
                                    _latestValue.SetException(t.Exception.InnerException);
                                }

                                _inFlight = null;
                                _invalidated = true;

                                break;
                            case TaskStatus.RanToCompletion:

                                if (_operationId == newId)
                                {
                                    _latestValue.SetResult(t.Result);
                                }

                                _inFlight = null;
                                _invalidated = false;

                                break;
                        }
                    }
                });
                _invalidated = false;
                return _latestValue.Task;

            }
            else
            {
                return _latestValue.Task;
            }
        }
           
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            if (!_invalidated)
            {
                _invalidated = true;
                    
            }
        }

        _nextStep?.Invalidate();
    }
}