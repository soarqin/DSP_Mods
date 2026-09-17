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
    readonly Func<int> _frameCount;
    readonly Timer _watchdog;
    GameWork _running;
    bool _pumpScheduled;
    bool _stopped;
    int _frame = -1;
    int _startedThisFrame;
    readonly Stopwatch _budget = new Stopwatch();

    public MainThreadDispatcher(SynchronizationContext context, object token, GameWorkExecutor execute, Func<long> nowMs,
        Func<int> frameCount = null)
    {
        _context = context;
        _token = token;
        _execute = execute;
        _nowMs = nowMs;
        _frameCount = frameCount ?? (() => Time.frameCount);
        _watchdog = new Timer(_ => SweepTimeouts(), null, 250, 250);
    }

    public bool TryEnqueue(GameWork work)
    {
        lock (_gate)
        {
            if (_stopped || _queue.Count >= ApiLimits.MaxPendingRequests) return false;
            _queue.Enqueue(work);
            ScheduleLocked();
            return true;
        }
    }

    public void CancelQueued(Func<GameWork, ApiError> reason)
    {
        lock (_gate)
        {
            var count = _queue.Count;
            while (count-- > 0)
            {
                var work = _queue.Dequeue();
                var error = reason(work);
                if (error == null)
                    _queue.Enqueue(work);
                else
                    work.Pending.TryComplete(error);
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _stopped = true;
            _running?.Pending.TryComplete(ApiErrors.RequestTimeout());
        }

        _watchdog.Dispose();
        CancelQueued(_ => ApiErrors.RequestTimeout());
    }

    void SweepTimeouts()
    {
        var now = _nowMs();
        lock (_gate)
        {
            if (_stopped) return;
            if (_running != null && now >= _running.DeadlineMs)
                _running.Pending.TryComplete(ApiErrors.RequestTimeout());
            var count = _queue.Count;
            while (count-- > 0)
            {
                var work = _queue.Dequeue();
                if (!work.Pending.IsActive)
                    continue;
                if (now >= work.DeadlineMs)
                    work.Pending.TryComplete(ApiErrors.RequestTimeout());
                else
                    _queue.Enqueue(work);
            }
        }
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
        var frame = _frameCount();
        if (frame != _frame)
        {
            _frame = frame;
            _startedThisFrame = 0;
            _budget.Reset();
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
                _running = work;
            }

            _startedThisFrame++;
            _budget.Start();
            try { Run(work); }
            finally
            {
                _budget.Stop();
                lock (_gate) _running = null;
            }
        }
    }

    void Run(GameWork work)
    {
        if (!work.Pending.IsActive)
            return;
        var now = _nowMs();
        if (now >= work.DeadlineMs)
        {
            work.Pending.TryComplete(ApiErrors.RequestTimeout());
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
            work.Pending.TryComplete(ApiErrors.InternalError());
            return;
        }

        if (!work.Pending.IsActive)
            return;
        if (_nowMs() >= work.DeadlineMs)
        {
            work.Pending.TryComplete(ApiErrors.RequestTimeout());
            return;
        }

        if (result is ApiError err)
            work.Pending.TryComplete(err);
        else
            work.Pending.TryCompleteResult(result);
    }
}
