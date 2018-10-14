using System;
using System.Collections.Generic;

namespace langbuilder.Lexer.Tokenizers
{
    public abstract class Tokenizer
    {
        public abstract (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source);
    }

    sealed class EofTokenizer : Tokenizer
    {
        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            if (source.Length == 0) return (1, Symbol.EOF);
            return (0, default);
        }
    }

    // TODO: make newline configurable
    sealed class EolTokenizer : Tokenizer
    {
        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            var span = source.Span;
            if (span.Length > 0 && span[0] == '\n') return (1, Symbol.EOL);
            if (span.Length > 1 && span[0] == '\n' && span[1] == '\r') return (2, Symbol.EOL);
            return (0, default);
        }
    }

    sealed class SingleCharacterSymbolTokenizer : Tokenizer
    {
        static readonly Dictionary<char, Symbol> _symbols = new Dictionary<char, Symbol>();

        static SingleCharacterSymbolTokenizer()
        {
            _symbols.Add(';', Symbol.Semicolon);
            _symbols.Add(':', Symbol.Colon);
            _symbols.Add('.', Symbol.Dot);
            _symbols.Add(',', Symbol.Comma);
            _symbols.Add('!', Symbol.Exclamation);
            _symbols.Add('&', Symbol.Ampersand);
            _symbols.Add('#', Symbol.Hash);
            _symbols.Add('@', Symbol.At);
            _symbols.Add('$', Symbol.Dollar);
            _symbols.Add('|', Symbol.VerticalBar);
            _symbols.Add('?', Symbol.Question);
            _symbols.Add('`', Symbol.Apostrophy);
            _symbols.Add('\\', Symbol.Backslash);

            _symbols.Add('(', Symbol.ParenthesisOpen);
            _symbols.Add(')', Symbol.ParenthesisClose);
            _symbols.Add('{', Symbol.BraceOpen);
            _symbols.Add('}', Symbol.BraceClose);
            _symbols.Add('[', Symbol.SquareBracketOpen);
            _symbols.Add(']', Symbol.SquareBracketClose);

            _symbols.Add('<', Symbol.LessThan);
            _symbols.Add('>', Symbol.GreaterThan);
            _symbols.Add('=', Symbol.Equal);

            _symbols.Add('+', Symbol.Plus);
            _symbols.Add('-', Symbol.Minus);
            _symbols.Add('*', Symbol.Multiply);
            _symbols.Add('/', Symbol.Divide);
        }

        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            if (_symbols.TryGetValue(source.Span[0], out Symbol symbol)) return (1, symbol);
            return (0, default);
        }
    }

    sealed class TwoCharacterSymbolTokenizer : Tokenizer
    {
        sealed class CharactersComparer : IEqualityComparer<ReadOnlyMemory<char>>
        {
            public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => MemoryExtensions.SequenceEqual(x.Span, y.Span);


            // TODO: this is bad
            public int GetHashCode(ReadOnlyMemory<char> obj)
            {
                int hash = 0;
                var span = obj.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    hash |= (span[i] << i % 2);
                }
                return hash;
            }

            public static readonly IEqualityComparer<ReadOnlyMemory<char>> Default = new CharactersComparer();
        }

        static readonly Dictionary<ReadOnlyMemory<char>, Symbol> s_symbols = new Dictionary<ReadOnlyMemory<char>, Symbol>(CharactersComparer.Default);

        static TwoCharacterSymbolTokenizer()
        {
            s_symbols.Add("//".AsMemory(), Symbol.DoubleForwardSlash);

            s_symbols.Add("/*".AsMemory(), Symbol.CommentOpen);
            s_symbols.Add("*/".AsMemory(), Symbol.CommentClose);

            s_symbols.Add("==".AsMemory(), Symbol.EqualEqual);
            s_symbols.Add("<=".AsMemory(), Symbol.LessThanOrEqual);
            s_symbols.Add(">=".AsMemory(), Symbol.GreaterThanOrEqual);
            s_symbols.Add("!=".AsMemory(), Symbol.NotEqual);
        }

        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            if (source.Length > 1)
            {
                var substring = source.Slice(0, 2);
                if (s_symbols.TryGetValue(substring, out Symbol symbol)) return (2, symbol);
            }

            return (0, default);
        }
    }

    // TODO: add signed, and double tokenizer
    sealed class IntegerTokenizer : Tokenizer
    {
        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            ulong value = 0;
            int consumed = 0;

            var span = source.Span;
            for (consumed = 0; consumed < span.Length; consumed++)
            {
                var c = span[consumed];
                if (c < '0' || c > '9') break;
                value *= 10;
                value += (ulong)(c - '0');
            }

            if (consumed == 0) return (0, default);
            return (consumed, new Token(value));
        }
    }

    // TODO: combine inclusive and exclusive range tokenizers
    abstract class ExclusiveRangeTokenizer : Tokenizer
    {
        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            int index = 0;
            var span = source.Span;

            if (!IsStart(span[index++])) return (0, default);

            while (span.Length > index)
            {
                char next = span[index];

                if (IsEnd(next)) return (index, new Token(Kind, source.Slice(0, index)));

                if (!IsValid(next)) break;

                index++;
            }

            return (0, default);
        }

        protected abstract bool IsEnd(char c);

        protected abstract bool IsValid(char c);
        protected abstract bool IsStart(char c);

        protected abstract TokenKind Kind { get; }
    }

    abstract class InclusiveRangeTokenizer : Tokenizer
    {
        public override (int consumed, Token token) Tokenize(ReadOnlyMemory<char> source)
        {
            int index = 0;
            var span = source.Span;

            if (!IsStart(span[index++])) return (0, default);

            while (source.Length > index)
            {
                char next = span[index];

                if (IsEnd(next)) return (index + 1, new Token(Kind, source.Slice(0, index + 1)));

                if (!IsValid(next)) break;

                index++;
            }

            return (0, default);
        }

        protected abstract bool IsEnd(char c);

        protected abstract bool IsValid(char c);
        protected abstract bool IsStart(char c);

        protected abstract TokenKind Kind { get; }
    }

    sealed class StringTokenizer : InclusiveRangeTokenizer
    {
        protected override bool IsEnd(char c) => c == '"';
        protected override bool IsValid(char c) => true;
        protected override bool IsStart(char c) => c == '"';
        protected override TokenKind Kind => TokenKind.String;
    }

    sealed class IdentifierTokenizer : ExclusiveRangeTokenizer
    {
        protected override bool IsEnd(char c) => !IsValid(c);

        protected override bool IsValid(char c)
            => Char.IsLetterOrDigit(c) || c == '_';
        protected override bool IsStart(char c)
            => Char.IsLetter(c) || c == '_';

        protected override TokenKind Kind => TokenKind.Identifier;
    }

    sealed class WhitespaceTokenizer : ExclusiveRangeTokenizer
    {
        protected override bool IsEnd(char c) => !char.IsWhiteSpace(c);
        protected override bool IsValid(char c) => char.IsWhiteSpace(c);
        protected override bool IsStart(char c) => char.IsWhiteSpace(c);
        protected override TokenKind Kind => TokenKind.Whitespace;
    }
}