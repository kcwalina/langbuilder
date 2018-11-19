using langbuilder.Lexer;
using System;
using System.Collections.Generic;

namespace alan.Ast
{
    class FxProgram
    {
        public readonly List<FxFunction> Functions = new List<FxFunction>();
        public readonly List<FxType> Types = new List<FxType>();
        public readonly Dictionary<ReadOnlyMemory<char>, FxTypeReference> References = new Dictionary<ReadOnlyMemory<char>, FxTypeReference>(new StringSliceComparer());
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
        List<FxStatement> _statements = new List<FxStatement>();

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
}
