using langbuilder.Lexer.Tokenizers;
using System;
using System.Collections.Generic;

namespace langbuilder.Lexer
{
    public class Lexer
    {
        // TODO: add comment tokenizer (to improve perf)
        // TODO: make tokenizers configurable
        List<Tokenizer> s_tokenizers = new List<Tokenizer>() {
            new EofTokenizer(),
            new EolTokenizer(),
            new WhitespaceTokenizer(),
            new StringTokenizer(),
            new IntegerTokenizer(),
            new IdentifierTokenizer(),
            new TwoCharacterSymbolTokenizer(),
            new SingleCharacterSymbolTokenizer()
        };

        public ReadOnlyMemory<Token> GetTokens(ReadOnlyMemory<char> source, Settings settings = default)
        {
            if (settings.Eol != null) s_tokenizers[1] = settings.Eol;
            if (settings.Whitespace != null) s_tokenizers[2] = settings.Whitespace;
            if (settings.String != null) s_tokenizers[3] = settings.String;
            if (settings.Identifier != null) s_tokenizers[5] = settings.Identifier;

            var sourceLocation = new SourceLocation();

            List<Token> tokens = new List<Token>(source.Length / 5);

            while (true)
            {
                int consumed = 0;
                Token token = default;

                foreach (Tokenizer tokenizer in s_tokenizers)
                {
                    (consumed, token) = tokenizer.Tokenize(source);

                    if (consumed > 0)
                    {
                        token.SourceLocation = sourceLocation;
                        sourceLocation.Advance(consumed);

                        if (token.Kind != TokenKind.Whitespace || settings.KeepWhitespace) tokens.Add(token);
                        if (token.Symbol == Symbol.EOF) return tokens.ToArray();
                        if (token.Symbol == Symbol.EOL) sourceLocation.AdvanceLine();

                        source = source.Slice(consumed);

                        break;
                    }
                }

                // Did we make progress?
                if (consumed == 0)
                {
                    int eol = source.Span.IndexOf('\n');
                    throw new InvalidTokenException($"ERROR at {sourceLocation.CurrentLine}:{sourceLocation.CurrentColumn} in {source.Slice(0, eol)}");
                }
            }
        }

        public struct Settings
        {
            public bool KeepWhitespace;
            public bool KeepComments;
            public Tokenizer Eol;
            public Tokenizer Whitespace;
            public Tokenizer String;
            public Tokenizer Identifier;
        }

        public sealed class InvalidTokenException : Exception
        {
            public InvalidTokenException(string message) : base(message) { }
        }
    }
}