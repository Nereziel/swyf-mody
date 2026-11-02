using System;
using System.Threading;
using System.Threading.Tasks;

namespace CzechSpeechEnhanced
{
    internal sealed class SerialWork
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        internal Task AfterCurrent(Action action) => Run(() => { action(); return Task.FromResult(0); });
        internal async Task<T> Run<T>(Func<Task<T>> action)
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try { return await action().ConfigureAwait(false); }
            finally { gate.Release(); }
        }
    }
}
