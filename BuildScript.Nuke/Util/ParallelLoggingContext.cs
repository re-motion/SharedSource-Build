// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under
// the Apache License, Version 2.0 (the "License"); you may not use this
// file except in compliance with the License.  You may obtain a copy of the
// License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the
// License for the specific language governing permissions and limitations
// under the License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;
using Serilog.Events;

namespace Remotion.BuildScript.Util;

/// <summary>
/// Provides a way to override Console out/err as well as Serilog logging for parallel scenarios.
/// Will buffer events during a parallel work item and flush it synchronized once the work item is completed.
/// </summary>
public class ParallelLoggingContext : IDisposable
{
  private enum WorkItemEventType
  {
    LogEvent,
    Out,
    Error
  }

  private class WorkItemContext
  {
    public ConcurrentQueue<(WorkItemEventType, object)> LogEvents = new();
  }

  private class ParallelLogger (ParallelLoggingContext context) : ILogger
  {
    /// <inheritdoc />
    public void Write (LogEvent logEvent) => context.AddEvent(WorkItemEventType.LogEvent, logEvent);
  }

  private class ParallelTextWriter (ParallelLoggingContext context) : TextWriter
  {
    public override Encoding Encoding { get; }

    /// <inheritdoc />
    public override void Write (char value)
    {
      context.AddEvent(WorkItemEventType.Out, "" + value);
    }

    /// <inheritdoc />
    public override void Write (char[]? buffer, int index, int count)
    {
      if (buffer != null)
        context.AddEvent(WorkItemEventType.Out, new string(buffer, index, count));
    }

    /// <inheritdoc />
    public override void Write (string? value)
    {
      if (value != null)
        context.AddEvent(WorkItemEventType.Out, value);
    }
  }

  public static ParallelLoggingContext CreateAndAssign ()
  {
    var innerLogger = Log.Logger;
    var innerOut = Console.Out;
    var innerError = Console.Error;

    var context = new ParallelLoggingContext(innerOut, innerError, innerLogger);
    Log.Logger = new ParallelLogger(context);
    Console.SetOut(new ParallelTextWriter(context));
    Console.SetError(new ParallelTextWriter(context));

    return context;
  }

  private readonly TextWriter _innerOut;
  private readonly TextWriter _innerError;
  private readonly ILogger _innerLogger;

  private readonly object _flushingLock = new();
  private readonly AsyncLocal<WorkItemContext?> _workItemContext = new();

  // Using int as boolean for CompareExchange support in Dispose
  private int _disposed = 0;

  private ParallelLoggingContext (
      TextWriter innerOut,
      TextWriter innerError,
      ILogger innerLogger)
  {
    _innerOut = innerOut;
    _innerError = innerError;
    _innerLogger = innerLogger;
  }

  private void AddEvent (WorkItemEventType type, object obj)
  {
    var workItemContext = _workItemContext.Value;
    if (workItemContext != null)
    {
      // If we have a work context there should only be a single thread calling
      // which is why we don't have to lock access here
      workItemContext.LogEvents.Enqueue((type, obj));
    }
    else
    {
      // As we override globals, it might be that someone outside of our parallel loop is
      // writing events/output so we should handle these events directly
      HandleEvent(type, obj);
    }
  }

  public void StartParallelWork ()
  {
    if (_workItemContext.Value != null)
      throw new InvalidOperationException("A parallel logging context was already established for the calling thread.");

    _workItemContext.Value = new WorkItemContext();
  }

  public void StopParallelWork ()
  {
    var workItemContext = _workItemContext.Value;
    if (workItemContext != null)
    {
      // Reset the work item now because handling LogEvents will use
      // the Console to write events, which would otherwise be queued at the back
      _workItemContext.Value = null;

      lock (_flushingLock)
      {
        while (workItemContext.LogEvents.TryDequeue(out var tuple))
        {
          var (type, obj) = tuple;
          HandleEvent(type, obj);
        }
      }
    }
  }

  public void Dispose ()
  {
    // Ensure we only dispose once as we switch global values around
    if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
      return;

    Log.Logger = _innerLogger;
    Console.SetOut(_innerOut);
    Console.SetError(_innerError);
  }

  private void HandleEvent (WorkItemEventType type, object obj)
  {
    switch (type)
    {
      case WorkItemEventType.LogEvent:
        _innerLogger.Write((LogEvent)obj);
        break;
      case WorkItemEventType.Out:
        _innerOut.Write((string)obj);
        break;
      case WorkItemEventType.Error:
        _innerError.Write((string)obj);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
}