#region Copyright statement
// --------------------------------------------------------------
// Copyright (C) 1999-2016 Exclaimer Ltd. All Rights Reserved.
// No part of this source file may be copied and/or distributed 
// without the express permission of a director of Exclaimer Ltd
// ---------------------------------------------------------------
#endregion
using DeveloperTestInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeveloperTest
{
    public sealed class DeveloperTestImplementation : IDeveloperTest
    {
        private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(10);

        public void RunQuestionOne(ICharacterReader reader, IOutputResult output)
        {
            ValidateReader(reader, nameof(reader));
            ValidateOutput(output);

            object countsLock = new object();
            IDictionary<string, int> counts = WordFrequencyProcessor.CreateCounts();
            WordFrequencyProcessor.CountWords(reader, counts, countsLock);
            WordFrequencyProcessor.WriteResults(output, counts, countsLock);
        }

        public void RunQuestionTwo(ICharacterReader[] readers, IOutputResult output)
        {
            ValidateReaders(readers);
            ValidateOutput(output);

            object countsLock = new object();
            IDictionary<string, int> counts = WordFrequencyProcessor.CreateCounts();

            // Each reader is independent, so processing them on separate worker tasks avoids
            // one slow reader preventing progress on all the others.
            Task[] readerTasks = readers
                .Select(reader => Task.Run(() =>
                    WordFrequencyProcessor.CountWords(reader, counts, countsLock)))
                .ToArray();

            while (!Task.WaitAll(readerTasks, ProgressInterval))
            {
                WordFrequencyProcessor.WriteResults(output, counts, countsLock);
            }

            WordFrequencyProcessor.WriteResults(output, counts, countsLock);
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
