
using System;
using System.ComponentModel;

namespace langbuilder.Lexer
{
    public enum TokenKind : byte
    {
        Other,
        String,
        Identifier,
        Whitespace,
        Comment,
        Symbol,
        Unsigned,
        Signed,
        Double,
        EOL,
        EOF,
    }

    public enum Symbol : byte
    {
        NotASymbol = 0,

        // single character
        Equal,
        Semicolon,
        Colon,
        ParenthesisOpen,
        ParenthesisClose,
        BraceOpen,
        BraceClose,
        Dot,
        Comma,

        // comparisons
        EqualEqual,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,

        // two character
        DoubleForwardSlash,
        CommentOpen,
        CommentClose,

        Plus,
        Minus,
        Exclamation,
        Multiply,
        Divide,
        SquareBracketOpen,
        SquareBracketClose,
        Ampersand,
        Hash,
        At,
        Dollar,
        VerticalBar,
        Backslash,
        Question,
        Apostrophy
    }

    public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
    {
        ushort _line;
        ushort _column;

        public int CurrentLine => _line;
        public int CurrentColumn => _column;

        public void AdvanceLine()
        {
            _line++;
            _column = 0;
        }

        public void Advance(int items)
        {
            checked { _column += (ushort)items; }
        }

        public bool Equals(SourceLocation other)
            => _column == other._column && _line == other._line;

        public int CompareTo(SourceLocation other)
        {
            int lineComparison = _line.CompareTo(other._line);
            if (lineComparison != 0) return lineComparison;
            return _column.CompareTo(other._column);
        }

        public override int GetHashCode()
            => _line.GetHashCode() ^ _column.GetHashCode();

        public override string ToString() => $"{_line}:{_column}";

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
            => Equals((SourceLocation)obj);
    }

    public struct Token : IEquatable<Token>
    {
        public TokenKind Kind { get; }
        public Symbol Symbol { get; }

        public ulong _bytes;

        public SourceLocation SourceLocation { get; set; }

        public ReadOnlyMemory<char> Text { get; }

        public Token(Symbol symbol)
        {
            Kind = TokenKind.Symbol;
            Symbol = symbol;
            Text = default;
            SourceLocation = default;
            _bytes = default;
        }

        public Token(TokenKind kind, ReadOnlyMemory<char> text)
        {
            Kind = kind;
            Text = text;
            Symbol = default;
            SourceLocation = default;
            _bytes = default;
        }

        public Token(ulong value)
        {
            Kind = TokenKind.Unsigned;
            Text = default;
            Symbol = default;
            SourceLocation = default;
            _bytes = value;
        }

        public Token(long value)
        {
            Kind = TokenKind.Signed;
            Text = default;
            Symbol = default;
            SourceLocation = default;
            _bytes = (ulong)value;
        }

        public Token(double value)
        {
            Kind = TokenKind.Double;
            Text = default;
            Symbol = default;
            SourceLocation = default;
            _bytes = (ulong)value;
        }

        public static implicit operator Token(Symbol symbol) => new Token(symbol);

        public static readonly Token EOL = new Token(TokenKind.EOL, ReadOnlyMemory<char>.Empty);
        public static readonly Token EOF = new Token(TokenKind.EOF, ReadOnlyMemory<char>.Empty);

        public bool Equals(Token other)
        {
            if (Kind != other.Kind) return false;
            if (Symbol != other.Symbol) return false;
            if (!MemoryExtensions.SequenceEqual(Text.Span, other.Text.Span)) return false;
            return true;
        }

        public override string ToString()
            => Kind == TokenKind.Symbol ? $"{Kind},{Symbol}:{Text}" : $"{Kind},{Text}";
    }
}