using langbuilder.Lexer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class CGenerator
{
    FxProgram _program;
    string _directory;

    public void Generate(string directory, FxProgram program)
    {
        _program = program;
        _directory = directory;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        using (TextWriter writer = new StreamWriter(Path.Combine(directory, "main.c"))) {
            writer.WriteLine(@"#include <stdio.h>");
            writer.WriteLine();


            foreach (var type in program.Types) {
                throw new NotImplementedException();
            }

            foreach (var function in program.Functions) {
                Generate(writer, function);
            }
        }

        var clInfo = new ProcessStartInfo();
        string crt = @"""c:\Program Files (x86)\Windows Kits\10\Include\10.0.17134.0\ucrt""";
        string windowsShared    = @"""c:\Program Files (x86)\Windows Kits\10\Include\10.0.17134.0\shared""";
        string windowsOther     = @"""c:\Program Files (x86)\Windows Kits\10\Include\10.0.17134.0\um""";
        string vsInclude        = @"""c:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\include""";

        clInfo.FileName = "cl.exe";
        clInfo.Arguments = $" -c /MT /Gz /I {crt} /I {windowsShared} /I {windowsOther} /I {vsInclude} main.c";
        clInfo.CreateNoWindow = true;
        clInfo.WorkingDirectory = directory;
        clInfo.RedirectStandardOutput = true;
        clInfo.UseShellExecute = false;

        Console.WriteLine($"{clInfo.FileName} {clInfo.Arguments}");
        var clProcess = Process.Start(clInfo);
        Console.Out.Write(clProcess.StandardOutput.ReadToEnd());
        clProcess.WaitForExit();

        string crtLibs  = @"""c:\Program Files (x86)\Windows Kits\10\Lib\10.0.17134.0\ucrt\x86""";
        string vsLibs   = @"""c:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\lib""";
        string osLibs   = @"""c:\Program Files (x86)\Windows Kits\10\Lib\10.0.17134.0\um\x86""";
        // /LIBPATH:"C:\Program Files (x86)\Windows Kits\10\Lib\10.0.17134.0\um\x86"
        var linkInfo = new ProcessStartInfo();
        linkInfo.FileName = "link.exe";
        linkInfo.Arguments = $" /SUBSYSTEM:console main.obj /LIBPATH:{crtLibs} /LIBPATH:{vsLibs} /LIBPATH:{osLibs}";
        linkInfo.CreateNoWindow = true;
        linkInfo.WorkingDirectory = directory;
        linkInfo.RedirectStandardOutput = true;
        linkInfo.UseShellExecute = false;

        Console.WriteLine($"{linkInfo.FileName} {linkInfo.Arguments}");
        var linkProcess = Process.Start(linkInfo);
        Console.Out.Write(clProcess.StandardOutput.ReadToEnd());
        clProcess.WaitForExit();
    }

    public void Generate(TextWriter writer, FxFunction function)
    {
        if (function.Parameters != null) throw new NotImplementedException();
        writer.Write(function.ReturnType);
        writer.Write(' ');
        writer.Write(function.Name);
        writer.Write('(');
        writer.WriteLine(") {");
        Generate(writer, function.Body.Statements);
        writer.WriteLine('}');
    }

    public void Generate(TextWriter writer, IReadOnlyList<FxStatement> statements)
    {
        foreach(var statement in statements) {
            GenerateStatement(writer, statement);
        }
    }

    public void GenerateStatement(TextWriter writer, FxStatement statement)
    {
        statement = Alias(statement);
        writer.Write('\t');
        writer.Write(statement.Function);
        writer.Write('(');
        Generate(writer, statement.Arguments);
        writer.WriteLine(");");
    }

    public void Generate(TextWriter writer, IReadOnlyList<FxArgument> arguments)
    {
        bool first = true;
        foreach(var argument in arguments) {
            if (first) first = false;
            else writer.Write(", ");
            writer.Write(argument);
        }
    }

    FxStatement Alias(FxStatement original)
    {
        if(original.Function == "write") {
            var arguments = new List<FxArgument>();
            arguments.Add(new FxArgument("\"%s\""));
            arguments.AddRange(original.Arguments);
            return new FxStatement("printf".AsMemory(), arguments);
        }
        return original;
    }
}

class Alan
{
    static void Main(string[] args)
    {
        var lexer = new Lexer();
        var tokens = lexer.GetTokens(File.ReadAllText(@".\content\demo.alan").AsMemory());

        if(!TryParseProgram(ref tokens, out FxProgram program)) {
            Console.WriteLine("Error");
        }

        var generator = new CGenerator();
        
        generator.Generate(@".\src\c\", program);
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

class FxProgram
{
    public readonly List<FxFunction> Functions = new List<FxFunction>();
    public readonly List<FxType> Types = new List<FxType>();
    public readonly Dictionary<ReadOnlyMemory<char>,FxTypeReference> References = new Dictionary<ReadOnlyMemory<char>, FxTypeReference>(new StringSliceComparer());
}
class FxFunction
{
    private Token _name;
    private FxTypeReference _returnType;
    private List<FxParameter> _parameters;
    private FxScope _body;

    public FxFunction(Token functionName, FxTypeReference returnType, List<FxParameter> parameters, FxScope functionBody)
    {
        _name = functionName;
        _returnType = returnType;
        _parameters = parameters;
        _body = functionBody;
    }

    public string Name => _name.Text.ToString();
    public string ReturnType => _returnType.Name;
    public IReadOnlyList<FxParameter> Parameters => _parameters;
    public FxScope Body => _body;

    public override string ToString() => _name.Text.ToString();
}

class FxType
{
}

class FxExpression
{
    string _literalString;

    internal static FxExpression Literal(string literal)
        => new FxExpression() { _literalString = literal };

    public override string ToString() => _literalString;
}
class FxStatement
{
    ReadOnlyMemory<char> _functionName;
    List<FxArgument> _arguments;

    public FxStatement(ReadOnlyMemory<char> functionName, List<FxArgument> arguments)
    {
        _functionName = functionName;
        _arguments = arguments;
    }

    public string Function => _functionName.ToString();
    public IReadOnlyList<FxArgument> Arguments => _arguments;

    public override string ToString() => _functionName.ToString();
}
class FxTypeReference
{
    public string Name { get; }

    public FxTypeReference(ReadOnlyMemory<char> name)
        => Name = name.ToString();

    public override string ToString() => Name;
}

class FxArgument
{
    private string _literal;

    public FxArgument(string literal)
    {
        _literal = literal;
    }

    public override string ToString() => _literal;
}
class FxScope
{
    List<FxStatement> _statements= new List<FxStatement>();

    public FxScope(FxStatement statement)
    {
        _statements.Add(statement);
    }

    public IReadOnlyList<FxStatement> Statements => _statements;
    public override string ToString() => _statements.Count.ToString();
}

class FxParameter
{

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

