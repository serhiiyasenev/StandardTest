using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DeveloperTestInterfaces;

namespace DeveloperTest
{
    /// <summary>
    /// Contains the word parsing and result formatting shared by both implementations.
    /// </summary>
    internal static class WordFrequencyProcessor
    {
        public static IDictionary<string, int> CreateCounts()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public static void CountWords(
            ICharacterReader reader,
            IDictionary<string, int> counts,
            object countsLock)
        {
            var parser = new StreamingWordParser(word => AddWord(counts, countsLock, word));

            try
            {
                while (true)
                {
                    parser.Add(reader.GetNextChar());
                }
            }
            catch (EndOfStreamException)
            {
                // EndOfStreamException is the reader contract's normal completion signal.
            }

            // A stream is not required to end in punctuation or whitespace.
            parser.Complete();
        }

        public static async Task CountWordsAsync(
            ICharacterReader reader,
            IDictionary<string, int> counts,
            object countsLock,
            CancellationToken cancellationToken)
        {
            var parser = new StreamingWordParser(word => AddWord(counts, countsLock, word));

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    char character = await reader.GetNextCharAsync(cancellationToken).ConfigureAwait(false);
                    parser.Add(character);
                }
            }
            catch (EndOfStreamException)
            {
                // EndOfStreamException is the reader contract's normal completion signal.
            }

            cancellationToken.ThrowIfCancellationRequested();
            parser.Complete();
        }

        public static void WriteResults(
            IOutputResult output,
            IDictionary<string, int> counts,
            object countsLock,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<KeyValuePair<string, int>> snapshot;
            lock (countsLock)
            {
                snapshot = new List<KeyValuePair<string, int>>(counts);
            }

            snapshot.Sort(CompareResults);
            foreach (KeyValuePair<string, int> result in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                output.AddResult(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} - {1}",
                    result.Key,
                    result.Value));
            }
        }

        private static void AddWord(
            IDictionary<string, int> counts,
            object countsLock,
            string word)
        {
            lock (countsLock)
            {
                int count;
                counts.TryGetValue(word, out count);
                counts[word] = count + 1;
            }
        }

        private static int CompareResults(
            KeyValuePair<string, int> left,
            KeyValuePair<string, int> right)
        {
            int countComparison = right.Value.CompareTo(left.Value);
            return countComparison != 0
                ? countComparison
                : StringComparer.Ordinal.Compare(left.Key, right.Key);
        }

        /// <summary>
        /// Parses incrementally so long-running readers can contribute to periodic snapshots.
        /// A single hyphen between letters is part of a word (for example, "daisy-chain"),
        /// while repeated or trailing hyphens are word boundaries.
        /// </summary>
        private sealed class StreamingWordParser
        {
            private readonly Action<string> _wordHandler;
            private readonly StringBuilder _word = new StringBuilder();
            private bool _hasPendingHyphen;

            public StreamingWordParser(Action<string> wordHandler)
            {
                _wordHandler = wordHandler;
            }

            public void Add(char character)
            {
                if (char.IsLetter(character))
                {
                    if (_hasPendingHyphen)
                    {
                        _word.Append('-');
                    }

                    _word.Append(char.ToLowerInvariant(character));
                    _hasPendingHyphen = false;
                    return;
                }

                if (character == '-' && _word.Length > 0 && !_hasPendingHyphen)
                {
                    // Delay adding the hyphen until a following letter proves it is internal.
                    _hasPendingHyphen = true;
                    return;
                }

                Flush();
            }

            public void Complete()
            {
                Flush();
            }

            private void Flush()
            {
                if (_word.Length > 0)
                {
                    _wordHandler(_word.ToString());
                    _word.Clear();
                }

                _hasPendingHyphen = false;
            }
        }
    }
}
