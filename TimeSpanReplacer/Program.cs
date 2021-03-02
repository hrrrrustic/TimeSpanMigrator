using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TimeSpanReplacer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = "D:\\Development\\VisualStudio\\OpenSource\\runtime\\src\\libraries";
            foreach (String file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\ref\\") || file.Contains("\\tests\\") || file.Contains("asn.xml", StringComparison.OrdinalIgnoreCase) || file.Contains("asn1", StringComparison.OrdinalIgnoreCase))
                    continue;

                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                CSharpCompilation fileCompilation = CSharpCompilation.Create(null).AddSyntaxTrees(tree);
                
                new TimeSpanArgumentVisitor(fileCompilation.GetSemanticModel(tree)).Visit(tree.GetRoot());
            }

            Console.WriteLine(TimeSpanArgumentVisitor.Counter);
        }
    }

    public class TimeSpanArgumentVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        public static int Counter;
        private readonly HashSet<String> _timeNames = new HashSet<String>
        {
            "timeout", "milliseconds", "interval", "time", "seconds", "time"
        };
        public TimeSpanArgumentVisitor(SemanticModel model)
        {
            _model = model;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var intParameters = node
                .ParameterList
                .Parameters
                .Where(k => _model.GetTypeInfo(k.Type).Type is {Name: "Int32" or "int"})
                .ToList();

            if (!intParameters.Any())
                return;

            var timeParameters = intParameters
                .Where(k => _timeNames.Contains(k.Identifier.ValueText.ToLower()))
                .ToList();

            if(!timeParameters.Any())
                return;

            Counter++;
        }
    }
}
