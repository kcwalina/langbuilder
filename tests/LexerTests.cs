using langbuilder.Lexer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace Langbuilder.Tests
{
    [TestClass]
    public class LexerTests
    {
        [TestMethod]
        public void Basics()
        {
            string source = File.ReadAllText("content\\test1.txt");
            var lexer = new Lexer();
            var tokens = lexer.GetTokens(source.AsMemory());

            Assert.AreEqual(39, tokens.Length);
        }
    }
}
