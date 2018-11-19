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
        private ReadOnlyMemory<char> _name;
        private FxTypeReference _returnType;
        private List<FxVariable> _parameters;
        private FxScope _body;

        public FxFunction(ReadOnlyMemory<char> functionName, FxTypeReference returnType, List<FxVariable> parameters, FxScope functionBody)
        {
            _name = functionName;
            _returnType = returnType;
            _parameters = parameters;
            _body = functionBody;
        }

        public string Name => _name.ToString();
        public string ReturnType => _returnType.Name;
        public IReadOnlyList<FxVariable> Parameters => _parameters;
        public FxScope Body => _body;

        public override string ToString() => _name.ToString();
    }

    class FxVariable
    {
        public FxTypeReference Type;
        public ReadOnlyMemory<char> Name;

        public FxVariable(FxTypeReference type, ReadOnlyMemory<char> name)
        {
            Type = type;
            Name = name;
        }
    }

    class FxType
    {
    }

    class FxExpression
    {
        ReadOnlyMemory<char> _literalString;

        internal static FxExpression Literal(ReadOnlyMemory<char> literal)
            => new FxExpression() { _literalString = literal };

        public override string ToString() => _literalString.ToString();
    }

    abstract class FxStatement
    {

    }

    class FxReturn : FxStatement
    {
        private FxExpression expression;

        public FxReturn(FxExpression expression)
        {
            this.expression = expression;
        }
    }

    class FxConditional : FxStatement
    {
        private ReadOnlyMemory<char> name;
        private FxExpression expression;
        private FxStatement statement;

        public FxConditional(ReadOnlyMemory<char> name, FxExpression expression, FxStatement statement)
        {
            this.name = name;
            this.expression = expression;
            this.statement = statement;
        }
    }
    class FxCall : FxStatement
    {
        ReadOnlyMemory<char> _functionName;
        List<FxArgument> _arguments;

        public FxCall(ReadOnlyMemory<char> functionName, List<FxArgument> arguments)
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
        public List<FxStatement> Statements { get; } = new List<FxStatement>();

        public override string ToString() => Statements.Count.ToString();
    }
}
