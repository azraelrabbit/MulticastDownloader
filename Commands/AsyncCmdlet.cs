// <copyright file="AsyncCmdlet.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Management.Automation;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Logging.Simple;

    // Not practical to fix this with the PS object model.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    /// <summary>
    /// Represent a base for Async commandlets.
    /// </summary>
    /// <seealso cref="Cmdlet" />
    public abstract class AsyncCmdlet : Cmdlet, IDisposable
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private bool disposedValue = false;
        private Thread psThread;
        private AutoResetEvent actionQueuedEvent = new AutoResetEvent(false);
        private ConcurrentQueue<Action> pendingActions = new ConcurrentQueue<Action>();

        /// <summary>
        /// Gets or sets the log level.
        /// <para type="description">The log level.</para>
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        [Parameter]
        public LogLevel LogLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log file.
        /// <para type="description">The log file.</para>
        /// </summary>
        /// <value>
        /// The log file.
        /// </value>
        [Parameter]
        public string LogFile
        {
            get;
            set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        internal Task UiInvoke(Action<Cmdlet> cmdletAction)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (Thread.CurrentThread == this.psThread)
            {
                this.ExecuteActionAndCompleteTcs(cmdletAction, tcs);
            }
            else
            {
                this.pendingActions.Enqueue(() =>
                {
                    this.ExecuteActionAndCompleteTcs(cmdletAction, tcs);
                });
                this.actionQueuedEvent.Set();
            }

            return tcs.Task;
        }

        /// <summary>
        /// Ends the processing.
        /// </summary>
        protected sealed override void EndProcessing()
        {
            base.EndProcessing();
            this.psThread = Thread.CurrentThread;
            CmdletLoggerFactoryAdapter logger = null;

            try
            {
                if (this.LogLevel != LogLevel.Off)
                {
                    logger = new CmdletLoggerFactoryAdapter(this);
                    LogManager.Adapter = logger;
                }

                Task runTask = this.Run();
                while (!runTask.IsCanceled && !runTask.IsCompleted && !runTask.IsFaulted)
                {
                    this.actionQueuedEvent.WaitOne();
                    Action action;
                    while (this.pendingActions.TryDequeue(out action))
                    {
                        action();
                    }
                }

                runTask.Wait();
            }
            finally
            {
                Action action;
                while (this.pendingActions.TryDequeue(out action))
                {
                    action();
                }

                if (logger != null)
                {
                    logger.Dispose();
                }

                this.Dispose();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.actionQueuedEvent != null)
                    {
                        this.actionQueuedEvent.Dispose();
                    }
                }

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>A task object.</returns>
        protected abstract Task Run();

        private void ExecuteActionAndCompleteTcs(Action<Cmdlet> cmdletAction, TaskCompletionSource<bool> tcs)
        {
            try
            {
                cmdletAction(this);
                tcs.SetResult(true);
            }
            catch (OperationCanceledException)
            {
                tcs.SetCanceled();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }
}
