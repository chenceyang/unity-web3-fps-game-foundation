using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Web3
{
    public static class UnityWebRequestAwaiter
    {
        public static Task AwaitAsync(this UnityWebRequestAsyncOperation operation, CancellationToken ct = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (operation.isDone) return Task.CompletedTask;

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;

            void OnCompleted(AsyncOperation _)
            {
                registration.Dispose();
                completion.TrySetResult(true);
            }

            operation.completed += OnCompleted;
            if (ct.CanBeCanceled)
            {
                registration = ct.Register(() =>
                {
                    operation.completed -= OnCompleted;
                    operation.webRequest?.Abort();
                    completion.TrySetCanceled(ct);
                });
            }

            return completion.Task;
        }
    }
}
