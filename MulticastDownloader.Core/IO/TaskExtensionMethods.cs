// <copyright file="TaskExtensionMethods.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class TaskExtensionMethods
    {
        internal static async Task<T> WaitWithCancellation<T>(this Task<T> t0, CancellationToken token)
        {
            await WaitWithCancellation(t0, token);
            return await t0;
        }

        internal static async Task WaitWithCancellation(this Task t0, CancellationToken token)
        {
            await WaitWithCancellation(t0, token);
            await t0;
        }

        private static async Task WaitWithCancellationInternal(Task t0, CancellationToken token)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            using (CancellationTokenRegistration registration = token.Register(() => tcs.TrySetCanceled()))
            {
                Task waited = await Task.WhenAny(t0, tcs.Task);
                if (waited == tcs.Task)
                {
                    throw new OperationCanceledException();
                }
            }
        }
    }
}
