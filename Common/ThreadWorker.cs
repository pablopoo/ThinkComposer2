using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Windows;

namespace Instrumind.Common
{
    /// <summary>
    /// Executes code on an STA-Thread, allowing creation of WPF objects.
    /// (not like the BackgroundWorker which only runs on MTA-Threads).
    /// </summary>
    public class ThreadWorker<TResult>
    {
        private Thread WorkingThread = null;
        private readonly object CompletionLock = new object();
        private bool CompletionReported = false;
        private volatile bool CancellationRequested = false;

        private Dispatcher OriginalThreadDispatcher { get; set; }

        public Func<ThreadWorker<TResult>, OperationResult<TResult>> WorkTask { get; private set; }  // Returns cancellation message, null when completed

        // To be subscribed by WPF Thread
        public event Action<int, string> ProgressChanged;

        // To be subscribed by WPF Thread. Sends completion-status and message.
        public event Action<OperationResult<TResult>> ExecutionFinished;

        public bool IsBusy { get; private set; }
        public bool IsCancellationRequested { get { return this.CancellationRequested; } }

        public ThreadWorker(Dispatcher SourceDispatcher)
        {
            General.ContractRequiresNotNull(SourceDispatcher);

            this.OriginalThreadDispatcher = SourceDispatcher;
        }

        public void Start(Func<ThreadWorker<TResult>, OperationResult<TResult>> WorkTask)
        {
            General.ContractRequiresNotNull(WorkTask);

            this.WorkTask = WorkTask;
            this.CancellationRequested = false;
            this.CompletionReported = false;
            this.IsBusy = true;

            this.WorkingThread = new Thread(Run);
            this.WorkingThread.SetApartmentState(ApartmentState.STA);
            this.WorkingThread.Start();
        }

        private void Run()
        {
            OperationResult<TResult> TaskResult = null;

            try
            {
                this.ThrowIfCancellationRequested();
                TaskResult = this.WorkTask(this);
            }
            catch (OperationCanceledException)
            {
                TaskResult = OperationResult.Failure<TResult>("Cancelled by user.");
            }
            catch (Exception Problem)
            {
                TaskResult = OperationResult.Failure<TResult>("Operation failed.\nProblem: " + Problem.Message);
            }
            finally
            {
                this.IsBusy = false;
            }

            this.ReportFinished(TaskResult);
        }

        // To be called by WPF UI
        public void Cancel()
        {
            this.CancellationRequested = true;
            this.IsBusy = false;

            this.ReportFinished(OperationResult.Failure<TResult>("Cancelled by user."));
        }

        public void ThrowIfCancellationRequested()
        {
            if (this.CancellationRequested)
                throw new OperationCanceledException();
        }

        // To be called by working task
        public void ReportProgress(int Percentage, string StatusMessage)
        {
            this.ThrowIfCancellationRequested();

            Thread.MemoryBarrier();
            var Handler = ProgressChanged;
            Thread.MemoryBarrier();

            if (Handler != null)
                this.OriginalThreadDispatcher.BeginInvoke(Handler, Percentage, StatusMessage);
        }

        private void ReportFinished(OperationResult<TResult> TaskResult)
        {
            Action<OperationResult<TResult>> Handler;

            lock (this.CompletionLock)
            {
                if (this.CompletionReported)
                    return;

                this.CompletionReported = true;

                Thread.MemoryBarrier();
                Handler = this.ExecutionFinished;
                Thread.MemoryBarrier();
            }

            if (Handler != null)
                this.OriginalThreadDispatcher.BeginInvoke(Handler, TaskResult);
        }

        // -----------------------------------------------------------------------------------------
        // Methods To be called from alternate thread
        public TReturn AtOriginalThreadInvoke<TReturn>(Func<TReturn> Operation)
        {
            TReturn Result = default(TReturn);

            this.OriginalThreadDispatcher.Invoke(new Action(
                () =>
                {
                    Result = Operation();
                }));

            return Result;
        }

        public TReturn AtOriginalThreadGetFrozen<TReturn>(TReturn Target)
            where TReturn : Freezable
        {
            if (Target == null)
                return null;

            TReturn Result = null;

            this.OriginalThreadDispatcher.Invoke(new Action(
                () =>
                {
                    if (Target.IsFrozen)
                        Result = Target;
                    else
                        Result = (TReturn)Target.GetAsFrozen();
                }));

            return Result;
        }
    }
}
