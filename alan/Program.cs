using langbuilder.Lexer;
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var lexer = new Lexer();
        var tokens = lexer.GetTokens(File.ReadAllText(@".\content\demo.alan").AsMemory());
        foreach(var token in tokens.Span) {
            Console.WriteLine(token);
        }

        Console.ReadLine();
    }
}

