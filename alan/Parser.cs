using alan.Ast;
using alan.Generators;
using langbuilder.Lexer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

struct Tokens
{
    ReadOnlyMemory<Token> _tokens;

    public Tokens(ReadOnlyMemory<Token> tokens) => _tokens = tokens;

    public Token Peek() => _tokens.Span[0];
    public Token Read()
    {
        var token = _tokens.Span[0];
        _tokens = _tokens.Slice(1);
        return token;
    }
    public override string ToString()
    {
        if (_tokens.Length == 0) return "<empty>";

        var span = _tokens.Span;
        var sb = new StringBuilder();
        for (int i = 0; i < Math.Min(span.Length, 5); i++) { 
            sb.Append(span[i].Text.ToString());
            sb.Append(' ');
        }
        return sb.ToString();
    }

    public bool IsEmpty => _tokens.IsEmpty;
}
class Alan
{
    static bool TryParseProgram(Tokens tokens, out FxProgram program)
    {
        program = new FxProgram();
        var tokensCopy = tokens;
        while (!tokensCopy.IsEmpty) {
            if (TryParseFunction(ref tokensCopy, program, out FxFunction function)) {
                program.Functions.Add(function);
            }
            else if (TryParseType(ref tokensCopy, program, out FxType type)) {
                program.Types.Add(type);
            }
            else {
                return false;
            }
        }
        tokens = tokensCopy;
        return true;
    }

    static bool TryParseType(ref Tokens tokens, FxProgram program, out FxType type)
    {
        throw new NotImplementedException();
    }

    static bool TryParseFunction(ref Tokens tokens, FxProgram program, out FxFunction function)
    {
        var tokensCopy = tokens;
        function = null;
        if (!TryParseTypeReference(ref tokensCopy, program, out FxTypeReference returnType)) return false;
        if (!TryParseIdentifier(ref tokensCopy, out ReadOnlyMemory<char> functionName)) return false;
        if (!TryParseParameterList(ref tokensCopy, program, out List<FxVariable> parameters)) return false;
        if (!TryParseScope(ref tokensCopy, program, out FxScope functionBody)) return false;
        tokens = tokensCopy;
        function = new FxFunction(functionName, returnType, parameters, functionBody);
        return true;
    }

    static bool TryParseScope(ref Tokens tokens, FxProgram ast, out FxScope scope)
    {
        var tokensCopy = tokens;

        scope = new FxScope(); 
        if (!TryParseSymbol(ref tokensCopy, Symbol.BraceOpen)) return false;

        while (true) {
            if (!TryParseStatement(ref tokensCopy, ast, out FxStatement statement)) break;
            scope.Statements.Add(statement);
        }

        if (!TryParseSymbol(ref tokensCopy, Symbol.BraceClose)) return false;

        tokens = tokensCopy;      
        return true;

    }

    static bool TryParseCall(ref Tokens tokens, FxProgram program, out FxCall call)
    {
        var tokensCopy = tokens;
        call = default;
        if (!TryParseIdentifier(ref tokensCopy, out ReadOnlyMemory<char> functionName)) return false;
        if (!TryParseArgumentList(ref tokensCopy, program, out List<FxArgument> arguments)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.Semicolon)) return false;
        tokens = tokensCopy;
        call = new FxCall(functionName, arguments);
        return true;
    }

    static bool TryParseConditional(ref Tokens tokens, FxProgram program, out FxConditional conditional)
    {
        var tokensCopy = tokens;
        conditional = default;
        if (!TryParseIdentifier(ref tokensCopy, out ReadOnlyMemory<char> name)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisOpen)) return false;
        if (!TryParseExpression(ref tokensCopy, program, out var expression)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisClose)) return false;
        if (!TryParseStatement(ref tokensCopy, program, out var statement)) return false;
        tokens = tokensCopy;
        conditional = new FxConditional(name, expression, statement);
        return true;
    }

    static bool TryParseReturn(ref Tokens tokens, FxProgram program, out FxReturn conditional)
    {
        var tokensCopy = tokens;
        conditional = default;
        if (!TryParseIdentifier(ref tokensCopy, "return")) return false;
        if (!TryParseExpression(ref tokensCopy, program, out var expression)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.Semicolon)) return false;
        tokens = tokensCopy;
        conditional = new FxReturn(expression);
        return true;
    }

    static bool TryParseStatement(ref Tokens tokens, FxProgram program, out FxStatement statement)
    {
        if (TryParseCall(ref tokens, program, out var call)) { statement = call; return true; }
        if (TryParseConditional(ref tokens, program, out var conditional)) { statement = conditional; return true; }
        statement = default;
        return false;
    }

    static bool TryParseExpression(ref Tokens tokensCopy, FxProgram ast, out FxExpression expression)
    {
        expression = null;
        if (!TryParseStringLiteral(ref tokensCopy, ast, out ReadOnlyMemory<char> str)) return false;
        expression = FxExpression.Literal(str);
        return true;
    }

    static bool TryParseArgumentList(ref Tokens tokens, FxProgram ast, out List<FxArgument> arguments)
    {
        var tokensCopy = tokens;
        arguments = null;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisOpen)) return false;
        if (!TryParseExpression(ref tokensCopy, ast, out FxExpression expression)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisClose)) return false;
        tokens = tokensCopy;
        arguments = new List<FxArgument>();
        arguments.Add(new FxArgument(expression.ToString()));
        return true;
    }

    static bool TryParseParameterList(ref Tokens tokens, FxProgram program, out List<FxVariable> parameters)
    {
        var tokensCopy = tokens;
        parameters = default;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisOpen)) return false;
        if (!TryParseVariableDeclaration(ref tokensCopy, program, out FxVariable variable))
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisClose)) return false;
        tokens = tokensCopy;
        parameters = new List<FxVariable>();
        parameters.Add(variable);
        return true;
    }

    static bool TryParseVariableDeclaration(ref Tokens tokens, FxProgram program, out FxVariable variable)
    {
        var tokensCopy = tokens;
        variable = default;
        if (!TryParseTypeReference(ref tokens, program, out var type)) return false;
        if (!TryParseIdentifier(ref tokens, out var name)) return false;
        variable = new FxVariable(type, name);
        tokens = tokensCopy;
        return true;
    }

    static bool TryParseTypeReference(ref Tokens tokens, FxProgram program, out FxTypeReference reference)
    {
        reference = default;
        if (!TryParseIdentifier(ref tokens, out ReadOnlyMemory<char> token)) return false;
        if(program.References.TryGetValue(token, out reference)){
            return true;
        }
        reference = new FxTypeReference(token);
        program.References.Add(token, reference);
        return true;
    }

    static bool TryParseSymbol(ref Tokens tokens, Symbol symbol)
    {
        if (tokens.IsEmpty) return false;
        if (tokens.Peek().Kind != TokenKind.Symbol) return false;
        if (tokens.Peek().Symbol != symbol) return false;
        tokens.Read();
        return true;
    }

    static bool TryParseStringLiteral(ref Tokens tokens, FxProgram program, out ReadOnlyMemory<char> str)
    {
        str = null;
        if (tokens.IsEmpty) return false;
        if (tokens.Peek().Kind != TokenKind.String) return false;
        str = tokens.Read().Text;
        return true;
    }

    static bool TryParseIdentifier(ref Tokens tokens, out ReadOnlyMemory<char> identifier)
    {
        identifier = default;
        if (tokens.IsEmpty) return false;
        if (tokens.Peek().Kind != TokenKind.Identifier) return false;
        identifier = tokens.Read().Text;
        return true;
    }

    static bool TryParseIdentifier(ref Tokens tokens, string identifier)
    {
        if (tokens.IsEmpty) return false;
        var candidate = tokens.Peek();
        if (candidate.Kind != TokenKind.Identifier) return false;
        if (!candidate.Text.Span.SequenceEqual(identifier.AsSpan())) return false;
        tokens.Read();
        return true;
    }

    static void Main(string[] args)
    {
        if (Debugger.IsAttached) args = new string[] { "demo.alan" };
        if (args.Length < 1) {
            Console.WriteLine("alan.exe <filename>");
            return;

        }
        var lexer = new Lexer();
        var tokens = lexer.GetTokens(File.ReadAllText(args[0]).AsMemory());

        if (!TryParseProgram(new Tokens(tokens), out FxProgram program)) {
            Console.WriteLine("Error");
        }

        var generator = new CGenerator();

        generator.Generate(@".", program);
    }
}


class StringSliceComparer : IEqualityComparer<ReadOnlyMemory<char>>
{
    public bool Equals(ReadOnlyMemory<char> left, ReadOnlyMemory<char> right)
        => left.Span.SequenceEqual(right.Span);

    public int GetHashCode(ReadOnlyMemory<char> value)
    {
        var span = value.Span;
        if (span.Length == 0) return 0;
        int code = span[0].GetHashCode();
        code ^= span[span.Length-1].GetHashCode();
        code ^= span[span.Length/2].GetHashCode();
        return code;
    }
}

