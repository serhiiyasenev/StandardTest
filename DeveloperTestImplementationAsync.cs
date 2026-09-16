#region Copyright statement
// --------------------------------------------------------------
// Copyright (C) 1999-2016 Exclaimer Ltd. All Rights Reserved.
// No part of this source file may be copied and/or distributed 
// without the express permission of a director of Exclaimer Ltd
// ---------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DeveloperTestInterfaces;

namespace DeveloperTest
{
    public sealed class DeveloperTestImplementationAsync : IDeveloperTestAsync
    {
        private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(10);

        public async Task RunQuestionOne(ICharacterReader reader, IOutputResult output, CancellationToken cancellationToken)
        {
            ValidateReader(reader, nameof(reader));
            ValidateOutput(output);
            cancellationToken.ThrowIfCancellationRequested();

            object countsLock = new object();
            IDictionary<string, int> counts = WordFrequencyProcessor.CreateCounts();
            await WordFrequencyProcessor
                .CountWordsAsync(reader, counts, countsLock, cancellationToken)
                .ConfigureAwait(false);
            WordFrequencyProcessor.WriteResults(output, counts, countsLock, cancellationToken);
        }

        public async Task RunQuestionTwo(ICharacterReader[] readers, IOutputResult output, CancellationToken cancellationToken)
        {
            ValidateReaders(readers);
            ValidateOutput(output);
            cancellationToken.ThrowIfCancellationRequested();

            object countsLock = new object();
            IDictionary<string, int> counts = WordFrequencyProcessor.CreateCounts();
            Task[] readerTasks = readers
                .Select(reader => Task.Run(() =>
                    WordFrequencyProcessor.CountWordsAsync(
                        reader,
                        counts,
                        countsLock,
                        cancellationToken)))
                .ToArray();
            Task allReadersTask = Task.WhenAll(readerTasks);

            try
            {
                while (!allReadersTask.IsCompleted)
                {
                    Task timerTask = Task.Delay(ProgressInterval, cancellationToken);
                    Task completedTask = await Task
                        .WhenAny(allReadersTask, timerTask)
                        .ConfigureAwait(false);

                    if (completedTask == allReadersTask)
                    {
                        break;
                    }

                    // Awaiting the timer propagates cancellation before any more output is written.
                    await timerTask.ConfigureAwait(false);
                    WordFrequencyProcessor.WriteResults(output, counts, countsLock, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Readers receive the same token. Await them so their cancellation cleanup
                // finishes and every task exception is observed before cancellation escapes.
                try
                {
                    await allReadersTask.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Preserve the cancellation that caused this path after observing the task.
                }

                throw;
            }

            // Observe and propagate exceptions (including cancellation) from every reader.
            await allReadersTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            WordFrequencyProcessor.WriteResults(output, counts, countsLock, cancellationToken);
        }

        private static void ValidateReaders(ICharacterReader[] readers)
        {
            if (readers == null)
            {
                throw new ArgumentNullException(nameof(readers));
            }

            for (int index = 0; index < readers.Length; index++)
            {
                if (readers[index] == null)
                {
                    throw new ArgumentException("The readers collection cannot contain null entries.", nameof(readers));
                }
            }
        }

        private static void ValidateReader(ICharacterReader reader, string parameterName)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        private static void ValidateOutput(IOutputResult output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
        }
    }
}
