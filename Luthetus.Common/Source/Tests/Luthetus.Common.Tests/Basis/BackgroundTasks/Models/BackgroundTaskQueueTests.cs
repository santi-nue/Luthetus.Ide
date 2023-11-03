﻿using Luthetus.Common.RazorLib.Keys.Models;

namespace Luthetus.Common.RazorLib.BackgroundTasks.Models;

/// <summary>
/// Tests for <see cref="BackgroundTaskQueue"/>
/// </summary>
public class BackgroundTaskQueueTests
{
    [Fact]
    public void Constructor()
    {
        /*
         public BackgroundTaskQueue(Key<BackgroundTaskQueue> key, string displayName)
         */

        var key = Key<BackgroundTaskQueue>.NewKey();
        var displayName = "Continuous";

        var backgroundTaskQueue = new BackgroundTaskQueue(
            key,
            displayName);

        Assert.Equal(key, backgroundTaskQueue.Key);
        Assert.Equal(displayName, backgroundTaskQueue.DisplayName);
    }

    [Fact]
    public void Key()
    {
        /*
        public Key<BackgroundTaskQueue> Key { get; }
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void DisplayName()
    {
        /*
         public string DisplayName { get; }
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void BackgroundTasks()
    {
        /*
        public ConcurrentQueue<IBackgroundTask> BackgroundTasks { get; } = new();
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void WorkItemsQueueSemaphoreSlim()
    {
        /*
        public SemaphoreSlim WorkItemsQueueSemaphoreSlim { get; } = new(0);
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void ExecutingBackgroundTask()
    {
        /*
        public IBackgroundTask? ExecutingBackgroundTask
        {
            get => _executingBackgroundTask;
            set
            {
            }
        }
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void CountOfBackgroundTasks()
    {
        /*
        public int CountOfBackgroundTasks => BackgroundTasks.Count;
         */

        throw new NotImplementedException();
    }

    [Fact]
    public void ExecutingBackgroundTaskChanged()
    {
        /*
        public event Action? ExecutingBackgroundTaskChanged;
         */

        throw new NotImplementedException();
    }
}
