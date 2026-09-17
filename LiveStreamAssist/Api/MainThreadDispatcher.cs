using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace LiveStreamAssist.Api;

internal sealed class GameWork
{
    public PendingRequest Pending;
    public DataCall Call;
    public int Generation;
    public long DeadlineMs;
}

internal delegate object GameWorkExecutor(GameWork work);

internal sealed class MainThreadDispatcher
{
    readonly SynchronizationContext _context;
    readonly object _token;
    readonly object _gate = new object();
    readonly Queue<GameWork> _queue = new Queue<GameWork>();
    readonly GameWorkExecutor _execute;
    readonly Func<long> _nowMs;
    bool _pumpScheduled;
    bool _stopped;
    int _frame = -1;
    int _startedThisFrame;
    readonly Stopwatch _budget = new Stopwatch();

    public MainThreadDispatcher(SynchronizationContext context, object token, GameWorkExecutor execute, Func<long> nowMs)
    {
        _context = context;
        _token = token;
        _execute = execute;
        _nowMs = nowMs;
    }

    public bool TryEnqueue(GameWork work)
    {
        lock (_gate)
        {
            if (_stopped) return false;
            _queue.Enqueue(work);
            ScheduleLocked();
            return true;
        }
    }

    public void CancelQueued(Func<GameWork, ApiError> reason)
    {
        List<GameWork> taken;
        lock (_gate)
        {
            if (_queue.Count == 0) return;
            taken = new List<GameWork>(_queue.Count);
            while (_queue.Count > 0)
                taken.Add(_queue.Dequeue());
        }

        foreach (var work in taken)
            work.Pending.Complete(reason(work));
    }

    public void Stop()
    {
        lock (_gate)
        {
            _stopped = true;
        }

        CancelQueued(_ => ApiErrors.RequestTimeout());
    }

    void ScheduleLocked()
    {
        if (_pumpScheduled || _stopped) return;
        _pumpScheduled = true;
        _context.Post(Pump, _token);
    }

    void Pump(object state)
    {
        if (!ReferenceEquals(state, _token)) return;
        Drain();
        lock (_gate)
        {
            if (_stopped || _queue.Count == 0)
            {
                _pumpScheduled = false;
                return;
            }
        }

        _context.Post(Pump, _token);
    }

    void Drain()
    {
        var frame = Time.frameCount;
        if (frame != _frame)
        {
            _frame = frame;
            _startedThisFrame = 0;
            _budget.Restart();
        }

        while (_startedThisFrame < ApiLimits.MaxRequestsPerFrame)
        {
            if (_startedThisFrame > 0 && _budget.ElapsedMilliseconds >= ApiLimits.MainThreadBudgetMs)
                break;
            GameWork work;
            lock (_gate)
            {
                if (_stopped || _queue.Count == 0) return;
                work = _queue.Dequeue();
            }

            _startedThisFrame++;
            Run(work);
        }
    }

    void Run(GameWork work)
    {
        if (!work.Pending.IsActive)
            return;
        var now = _nowMs();
        if (now > work.DeadlineMs)
        {
            work.Pending.Complete(ApiErrors.RequestTimeout());
            return;
        }

        object result;
        try
        {
            result = _execute(work);
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogError($"API game-data request failed: {ex}");
            work.Pending.Complete(ApiErrors.InternalError());
            return;
        }

        if (!work.Pending.IsActive)
            return;
        if (_nowMs() > work.DeadlineMs)
        {
            work.Pending.Complete(ApiErrors.RequestTimeout());
            return;
        }

        if (result is ApiError err)
            work.Pending.Complete(err);
        else
            work.Pending.CompleteResult(result);
    }
}
