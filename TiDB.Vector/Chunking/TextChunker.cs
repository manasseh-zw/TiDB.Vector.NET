using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TiDB.Vector.Chunking
{
    // Adapted from Microsoft.SemanticKernel.Text.TextChunker (MIT License)
    // Simplified to remove experimental attributes and maintainers' annotations
    public static class TextChunker
    {
        private sealed class StringListWithTokenCount
        {
            private readonly TokenCounter? _tokenCounter;
            public StringListWithTokenCount(TokenCounter? tokenCounter) { _tokenCounter = tokenCounter; }

            public void Add(string value) => Values.Add((value, _tokenCounter is null ? GetDefaultTokenCount(value.Length) : _tokenCounter(value)));
            public void Add(string value, int tokenCount) => Values.Add((value, tokenCount));
            public void AddRange(StringListWithTokenCount range) => Values.AddRange(range.Values);
            public void RemoveRange(int index, int count) => Values.RemoveRange(index, count);
            public int Count => Values.Count;
            public List<string> ToStringList() => Values.Select(v => v.Value).ToList();
            private List<(string Value, int TokenCount)> Values { get; } = new();
            public string ValueAt(int i) => Values[i].Value;
            public int TokenCountAt(int i) => Values[i].TokenCount;
        }

        public delegate int TokenCounter(string input);

        private static readonly char[] s_spaceChar = [' '];
        private static readonly string?[] s_plaintextSplitOptions = ["\n", ".。．", "?!", ";", ":", ",，、", ")]}", " ", "-", null];
        private static readonly string?[] s_markdownSplitOptions = [".\u3002\uFF0E", "?!", ";", ":", ",\uFF0C\u3001", ")]}", " ", "-", "\n\r", null];

        public static List<string> SplitPlainTextLines(string text, int maxTokensPerLine, TokenCounter? tokenCounter = null) =>
            InternalSplitLines(text, maxTokensPerLine, trim: true, s_plaintextSplitOptions, tokenCounter);

        public static List<string> SplitMarkDownLines(string text, int maxTokensPerLine, TokenCounter? tokenCounter = null) =>
            InternalSplitLines(text, maxTokensPerLine, trim: true, s_markdownSplitOptions, tokenCounter);

        public static List<string> SplitPlainTextParagraphs(
            IEnumerable<string> lines,
            int maxTokensPerParagraph,
            int overlapTokens = 0,
            string? chunkHeader = null,
            TokenCounter? tokenCounter = null) =>
            InternalSplitTextParagraphs(
                lines.Select(line => line.Replace("\r\n", "\n").Replace('\r', '\n')),
                maxTokensPerParagraph,
                overlapTokens,
                chunkHeader,
                static (text, maxTokens, tokenCounter) => InternalSplitLines(text, maxTokens, trim: false, s_plaintextSplitOptions, tokenCounter),
                tokenCounter);

        public static List<string> SplitMarkdownParagraphs(
            IEnumerable<string> lines,
            int maxTokensPerParagraph,
            int overlapTokens = 0,
            string? chunkHeader = null,
            TokenCounter? tokenCounter = null) =>
            InternalSplitTextParagraphs(lines, maxTokensPerParagraph, overlapTokens, chunkHeader,
                static (text, maxTokens, tokenCounter) => InternalSplitLines(text, maxTokens, trim: false, s_markdownSplitOptions, tokenCounter), tokenCounter);

        private static List<string> InternalSplitTextParagraphs(
            IEnumerable<string> lines,
            int maxTokensPerParagraph,
            int overlapTokens,
            string? chunkHeader,
            Func<string, int, TokenCounter?, List<string>> longLinesSplitter,
            TokenCounter? tokenCounter)
        {
            if (maxTokensPerParagraph <= 0)
                throw new ArgumentException("maxTokensPerParagraph should be a positive number", nameof(maxTokensPerParagraph));
            if (maxTokensPerParagraph <= overlapTokens)
                throw new ArgumentException("overlapTokens cannot be larger than maxTokensPerParagraph", nameof(maxTokensPerParagraph));

            if (lines is ICollection<string> c && c.Count == 0)
                return new List<string>();

            var chunkHeaderTokens = chunkHeader is { Length: > 0 } ? GetTokenCount(chunkHeader, tokenCounter) : 0;
            var adjustedMaxTokensPerParagraph = maxTokensPerParagraph - overlapTokens - chunkHeaderTokens;

            IEnumerable<string> truncatedLines = lines.SelectMany(line => longLinesSplitter(line, adjustedMaxTokensPerParagraph, tokenCounter));
            var paragraphs = BuildParagraph(truncatedLines, adjustedMaxTokensPerParagraph, tokenCounter);
            var processedParagraphs = ProcessParagraphs(paragraphs, adjustedMaxTokensPerParagraph, overlapTokens, chunkHeader, longLinesSplitter, tokenCounter);
            return processedParagraphs;
        }

        private static List<string> BuildParagraph(IEnumerable<string> truncatedLines, int maxTokensPerParagraph, TokenCounter? tokenCounter)
        {
            StringBuilder paragraphBuilder = new();
            List<string> paragraphs = new();

            foreach (string line in truncatedLines)
            {
                if (paragraphBuilder.Length > 0)
                {
                    string? paragraph = null;

                    int currentCount = GetTokenCount(line, tokenCounter) + 1;
                    if (currentCount < maxTokensPerParagraph)
                    {
                        currentCount += tokenCounter is null ? GetDefaultTokenCount(paragraphBuilder.Length) : tokenCounter(paragraph = paragraphBuilder.ToString());
                    }

                    if (currentCount >= maxTokensPerParagraph)
                    {
                        paragraph ??= paragraphBuilder.ToString();
                        paragraphs.Add(paragraph.Trim());
                        paragraphBuilder.Clear();
                    }
                }

                paragraphBuilder.AppendLine(line);
            }

            if (paragraphBuilder.Length > 0)
                paragraphs.Add(paragraphBuilder.ToString().Trim());

            return paragraphs;
        }

        private static List<string> ProcessParagraphs(
            List<string> paragraphs,
            int adjustedMaxTokensPerParagraph,
            int overlapTokens,
            string? chunkHeader,
            Func<string, int, TokenCounter?, List<string>> longLinesSplitter,
            TokenCounter? tokenCounter)
        {
            if (paragraphs.Count > 1)
            {
                var lastParagraph = paragraphs[^1];
                var secondLastParagraph = paragraphs[^2];

                if (GetTokenCount(lastParagraph, tokenCounter) < adjustedMaxTokensPerParagraph / 4)
                {
                    var lastParagraphTokens = lastParagraph.Split(s_spaceChar, StringSplitOptions.RemoveEmptyEntries);
                    var secondLastParagraphTokens = secondLastParagraph.Split(s_spaceChar, StringSplitOptions.RemoveEmptyEntries);

                    if (lastParagraphTokens.Length + secondLastParagraphTokens.Length <= adjustedMaxTokensPerParagraph)
                    {
                        var newSecondLastParagraph = string.Join(" ", secondLastParagraphTokens);
                        var newLastParagraph = string.Join(" ", lastParagraphTokens);

                        paragraphs[^2] = $"{newSecondLastParagraph} {newLastParagraph}";
                        paragraphs.RemoveAt(paragraphs.Count - 1);
                    }
                }
            }

            var processedParagraphs = new List<string>();
            var paragraphStringBuilder = new StringBuilder();

            for (int i = 0; i < paragraphs.Count; i++)
            {
                paragraphStringBuilder.Clear();
                if (chunkHeader is not null)
                    paragraphStringBuilder.Append(chunkHeader);

                var paragraph = paragraphs[i];

                if (overlapTokens > 0 && i < paragraphs.Count - 1)
                {
                    var nextParagraph = paragraphs[i + 1];
                    var split = longLinesSplitter(nextParagraph, overlapTokens, tokenCounter);

                    paragraphStringBuilder.Append(paragraph);
                    if (split.Count != 0)
                        paragraphStringBuilder.Append(' ').Append(split[0]);
                }
                else
                {
                    paragraphStringBuilder.Append(paragraph);
                }

                processedParagraphs.Add(paragraphStringBuilder.ToString());
            }

            return processedParagraphs;
        }

        private static List<string> InternalSplitLines(string text, int maxTokensPerLine, bool trim, string?[] splitOptions, TokenCounter? tokenCounter)
        {
            var result = new StringListWithTokenCount(tokenCounter);

            text = text.Replace("\r\n", "\n");
            result.Add(text);
            for (int i = 0; i < splitOptions.Length; i++)
            {
                int count = result.Count;
                var (splits2, inputWasSplit2) = Split(result, maxTokensPerLine, splitOptions[i].AsSpan(), trim, tokenCounter);
                result.AddRange(splits2);
                result.RemoveRange(0, count);
                if (!inputWasSplit2) break;
            }
            return result.ToStringList();
        }

        private static (StringListWithTokenCount, bool) Split(StringListWithTokenCount input, int maxTokens, ReadOnlySpan<char> separators, bool trim, TokenCounter? tokenCounter)
        {
            bool inputWasSplit = false;
            StringListWithTokenCount result = new(tokenCounter);
            int count = input.Count;
            for (int i = 0; i < count; i++)
            {
                var (splits, split) = Split(input.ValueAt(i).AsSpan(), input.ValueAt(i), maxTokens, separators, trim, tokenCounter, input.TokenCountAt(i));
                result.AddRange(splits);
                inputWasSplit |= split;
            }
            return (result, inputWasSplit);
        }

        private static (StringListWithTokenCount, bool) Split(ReadOnlySpan<char> input, string? inputString, int maxTokens, ReadOnlySpan<char> separators, bool trim, TokenCounter? tokenCounter, int inputTokenCount)
        {
            Debug.Assert(inputString is null || input.SequenceEqual(inputString.AsSpan()));
            StringListWithTokenCount result = new(tokenCounter);
            var inputWasSplit = false;

            if (inputTokenCount > maxTokens)
            {
                inputWasSplit = true;
                int half = input.Length / 2;
                int cutPoint = -1;

                if (separators.IsEmpty)
                {
                    cutPoint = half;
                }
                else if (input.Length > 2)
                {
                    int pos = 0;
                    while (true)
                    {
                        int index = input.Slice(pos, input.Length - 1 - pos).IndexOfAny(separators);
                        if (index < 0) break;
                        index += pos;
                        if (Math.Abs(half - index) < Math.Abs(half - cutPoint))
                            cutPoint = index + 1;
                        pos = index + 1;
                    }
                }

                if (cutPoint > 0)
                {
                    var firstHalf = input.Slice(0, cutPoint);
                    var secondHalf = input.Slice(cutPoint);
                    if (trim)
                    {
                        firstHalf = firstHalf.Trim();
                        secondHalf = secondHalf.Trim();
                    }

                    var (splits1, split1) = Split(firstHalf, null, maxTokens, separators, trim, tokenCounter, GetTokenCount(firstHalf.ToString(), tokenCounter));
                    result.AddRange(splits1);
                    var (splits2, split2) = Split(secondHalf, null, maxTokens, separators, trim, tokenCounter, GetTokenCount(secondHalf.ToString(), tokenCounter));
                    result.AddRange(splits2);

                    inputWasSplit = split1 || split2;
                    return (result, inputWasSplit);
                }
            }

            var resultString = inputString ?? input.ToString();
            var resultTokenCount = inputTokenCount;
            if (trim && !resultString.Trim().Equals(resultString, StringComparison.Ordinal))
            {
                resultString = resultString.Trim();
                resultTokenCount = GetTokenCount(resultString, tokenCounter);
            }

            result.Add(resultString, resultTokenCount);

            return (result, inputWasSplit);
        }

        private static int GetTokenCount(string input, TokenCounter? tokenCounter) => tokenCounter is null ? GetDefaultTokenCount(input.Length) : tokenCounter(input);
        private static int GetDefaultTokenCount(int length) => length >> 2;
    }
}


