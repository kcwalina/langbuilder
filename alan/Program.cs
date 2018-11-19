using alan.Ast;
using alan.Generators;
using langbuilder.Lexer;
using System;
using System.Collections.Generic;
using System.IO;

class Alan
{
    static void Main(string[] args)
    {
        if(args.Length < 1) {
            Console.WriteLine("alan.exe <filename>");
            return;

        }
        var lexer = new Lexer();
        var tokens = lexer.GetTokens(File.ReadAllText(args[0]).AsMemory());

        if(!TryParseProgram(ref tokens, out FxProgram program)) {
            Console.WriteLine("Error");
        }

        var generator = new CGenerator();
        
        generator.Generate(@".", program);
    }

    static bool TryParseProgram(ref ReadOnlyMemory<Token> tokens, out FxProgram program)
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

    static bool TryParseType(ref ReadOnlyMemory<Token> tokens, FxProgram program, out FxType type)
    {
        throw new NotImplementedException();
    }

    static bool TryParseFunction(ref ReadOnlyMemory<Token> tokens, FxProgram ast, out FxFunction function)
    {
        var tokensCopy = tokens;
        function = null;
        if (!TryParseTypeReference(ref tokensCopy, ast, out FxTypeReference returnType)) return false;
        if (!TryParseIdentifier(ref tokensCopy, ast, out Token functionName)) return false;
        if (!TryParseParameterList(ref tokensCopy, ast, out List<FxParameter> parameters)) return false;
        if (!TryParseScope(ref tokensCopy, ast, out FxScope functionBody)) return false;
        tokens = tokensCopy;
        function = new FxFunction(functionName, returnType, parameters, functionBody);
        return true;
    }

    static bool TryParseScope(ref ReadOnlyMemory<Token> tokens, FxProgram ast, out FxScope scope)
    {
        var tokensCopy = tokens;
        scope = default;
        if (!TryParseSymbol(ref tokensCopy, Symbol.BraceOpen)) return false;
        if (!TryParseStatement(ref tokensCopy, ast, out FxStatement statement)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.BraceClose)) return false;
        tokens = tokensCopy;
        scope = new FxScope(statement);
        return true;

    }

    static bool TryParseStatement(ref ReadOnlyMemory<Token> tokens, FxProgram ast, out FxStatement statement)
    {
        var tokensCopy = tokens;
        statement = default;
        if (!TryParseIdentifier(ref tokensCopy, ast, out Token functionName)) return false;
        if (!TryParseArgumentList(ref tokensCopy, ast, out List<FxArgument> arguments)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.Semicolon)) return false;
        tokens = tokensCopy;
        statement = new FxStatement(functionName.Text, arguments);
        return true;
    }

    static bool TryParseExpression(ref ReadOnlyMemory<Token> tokensCopy, FxProgram ast, out FxExpression expression)
    {
        expression = null;
        if (!TryParseStringLiteral(ref tokensCopy, ast, out string str)) return false;
        expression = FxExpression.Literal(str);
        return true;
    }

    static bool TryParseArgumentList(ref ReadOnlyMemory<Token> tokens, FxProgram ast, out List<FxArgument> arguments)
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

    static bool TryParseParameterList(ref ReadOnlyMemory<Token> tokens, FxProgram program, out List<FxParameter> parameters)
    {
        var tokensCopy = tokens;
        parameters = null;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisOpen)) return false;
        if (!TryParseSymbol(ref tokensCopy, Symbol.ParenthesisClose)) return false;
        tokens = tokensCopy;
        return true;
    }

    static bool TryParseTypeReference(ref ReadOnlyMemory<Token> tokens, FxProgram program, out FxTypeReference reference)
    {
        reference = default;
        if (!TryParseIdentifier(ref tokens, program, out Token token)) return false;
        if(program.References.TryGetValue(token.Text, out reference)){
            return true;
        }
        reference = new FxTypeReference(token.Text);
        program.References.Add(token.Text, reference);
        return true;
    }

    static bool TryParseSymbol(ref ReadOnlyMemory<Token> tokens, Symbol symbol)
    {
        if (tokens.Length < 1) return false;
        if (tokens.Span[0].Kind != TokenKind.Symbol) return false;
        if (tokens.Span[0].Symbol != symbol) return false;
        tokens = tokens.Slice(1);
        return true;
    }

    static bool TryParseStringLiteral(ref ReadOnlyMemory<Token> tokens, FxProgram program, out string str)
    {
        str = null;
        if (tokens.Length < 1) return false;
        if (tokens.Span[0].Kind != TokenKind.String) return false;
        str = tokens.Span[0].Text.ToString();
        tokens = tokens.Slice(1);
        return true;
    }

    static bool TryParseIdentifier(ref ReadOnlyMemory<Token> tokens, FxProgram program, out Token identifier)
    {
        identifier = default;
        if (tokens.Length < 1) return false;
        if (tokens.Span[0].Kind != TokenKind.Identifier) return false;
        identifier = tokens.Span[0];
        tokens = tokens.Slice(1);
        return true;
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

