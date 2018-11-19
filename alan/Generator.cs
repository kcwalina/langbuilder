using alan.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace alan.Generators
{
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
            string windowsShared = @"""c:\Program Files (x86)\Windows Kits\10\Include\10.0.17134.0\shared""";
            string windowsOther = @"""c:\Program Files (x86)\Windows Kits\10\Include\10.0.17134.0\um""";
            string vsInclude = @"""c:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\include""";

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

            string crtLibs = @"""c:\Program Files (x86)\Windows Kits\10\Lib\10.0.17134.0\ucrt\x86""";
            string vsLibs = @"""c:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\lib""";
            string osLibs = @"""c:\Program Files (x86)\Windows Kits\10\Lib\10.0.17134.0\um\x86""";
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
            foreach (var statement in statements) {
                GenerateStatement(writer, statement);
            }
        }

        public void GenerateStatement(TextWriter writer, FxStatement statement)
        {
            if (statement is FxCall call) { GenerateCall(writer, call); }
            if (statement is FxConditional conditional) { GenerateConditional(writer, conditional); }
            if (statement is FxReturn ret) { GenerateReturn(writer, ret); }
        }

        public void GenerateConditional(TextWriter writer, FxConditional statement)
        {
            throw new NotImplementedException();
        }
        public void GenerateReturn(TextWriter writer, FxReturn statement)
        {
            throw new NotImplementedException();
        }
        
        public void GenerateCall(TextWriter writer, FxCall statement)
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
            foreach (var argument in arguments) {
                if (first) first = false;
                else writer.Write(", ");
                writer.Write(argument);
            }
        }

        FxCall Alias(FxCall original)
        {
            if (original.Function == "write") {
                var arguments = new List<FxArgument>();
                arguments.Add(new FxArgument("\"%s\""));
                arguments.AddRange(original.Arguments);
                return new FxCall("printf".AsMemory(), arguments);
            }
            return original;
        }
    }
}
