using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DeveloperTest;
using DeveloperTestInterfaces;

using NUnit.Framework;

namespace DeveloperTestFramework
{
    [TestFixture]
    public sealed class AsyncImplementationEdgeCases
    {
        [Test, Timeout(5000)]
        public async Task QuestionTwoStartsAllReadersWhenReadsCompleteSynchronously()
        {
            using (var allReadersStarted = new CountdownEvent(2))
            using (var firstReader = new SynchronouslyCompletingReader(allReadersStarted))
            using (var secondReader = new SynchronouslyCompletingReader(allReadersStarted))
            {
                var output = new CollectingOutput();
                var implementation = new DeveloperTestImplementationAsync();

                await implementation.RunQuestionTwo(
                    new ICharacterReader[] { firstReader, secondReader },
                    output,
                    CancellationToken.None);

                CollectionAssert.AreEqual(new[] { "a - 2" }, output.Results);
            }
        }

        [Test, Timeout(5000)]
        public async Task QuestionTwoWaitsForReaderCancellationCleanup()
        {
            using (var reader = new DelayedCancellationReader())
            using (var cancellationSource = new CancellationTokenSource())
            {
                var implementation = new DeveloperTestImplementationAsync();
                Task execution = implementation.RunQuestionTwo(
                    new ICharacterReader[] { reader },
                    new CollectingOutput(),
                    cancellationSource.Token);

                Assert.IsTrue(reader.Started.Wait(1000), "The reader did not start in time.");
                cancellationSource.Cancel();

                try
                {
                    await execution;
                    Assert.Fail("Cancellation should propagate to the caller.");
                }
                catch (OperationCanceledException)
                {
                    // Expected.
                }

                Assert.IsTrue(
                    reader.CleanupCompleted.IsSet,
                    "RunQuestionTwo returned before the reader finished cancellation cleanup.");
            }
        }

        private sealed class CollectingOutput : IOutputResult
        {
            private readonly IList<string> _results = new List<string>();

            public IEnumerable<string> Results => _results;

            public void AddResult(string text)
            {
                _results.Add(text);
            }
        }

        private sealed class SynchronouslyCompletingReader : ICharacterReader
        {
            private readonly CountdownEvent _allReadersStarted;
            private bool _hasReturnedCharacter;

            public SynchronouslyCompletingReader(CountdownEvent allReadersStarted)
            {
                _allReadersStarted = allReadersStarted;
            }

            public char GetNextChar()
            {
                throw new NotSupportedException();
            }

            public Task<char> GetNextCharAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_hasReturnedCharacter)
                {
                    _hasReturnedCharacter = true;
                    _allReadersStarted.Signal();
                    return Task.FromResult('a');
                }

                if (!_allReadersStarted.Wait(1000))
                {
                    throw new TimeoutException("Readers were not started concurrently.");
                }

                throw new EndOfStreamException();
            }

            public void Dispose()
            {
            }
        }

        private sealed class DelayedCancellationReader : ICharacterReader
        {
            public DelayedCancellationReader()
            {
                Started = new ManualResetEventSlim();
                CleanupCompleted = new ManualResetEventSlim();
            }

            public ManualResetEventSlim CleanupCompleted { get; }

            public ManualResetEventSlim Started { get; }

            public char GetNextChar()
            {
                throw new NotSupportedException();
            }

            public async Task<char> GetNextCharAsync(CancellationToken cancellationToken)
            {
                Started.Set();

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException("The infinite delay completed unexpectedly.");
                }
                catch (OperationCanceledException)
                {
                    await Task.Delay(150).ConfigureAwait(false);
                    CleanupCompleted.Set();
                    throw;
                }
            }

            public void Dispose()
            {
                Started.Dispose();
                CleanupCompleted.Dispose();
            }
        }
    }
}
