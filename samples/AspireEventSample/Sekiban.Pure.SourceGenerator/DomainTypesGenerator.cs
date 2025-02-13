using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
namespace Sekiban.Pure.SourceGenerator;

[Generator]
public class DomainTypesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect all class and record declarations
        var typeDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax || node is RecordDeclarationSyntax,
                static (ctx, _) => ctx.Node)
            .Where(static typeDecl => typeDecl is ClassDeclarationSyntax || typeDecl is RecordDeclarationSyntax);

        // Combine with compilation information
        var compilationAndTypes = context.CompilationProvider.Combine(typeDeclarations.Collect());


        // Generate source code
        context.RegisterSourceOutput(
            compilationAndTypes,
            (ctx, source) =>
            {
                var (compilation, types) = source;
                // Generate source code
                var rootNamespace = compilation.AssemblyName ?? throw new ApplicationException("AssemblyName is null");
                var sourceCode = GenerateSourceCode(rootNamespace);
                ctx.AddSource("DomainTypes.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            });

    }

    private string GenerateSourceCode(string rootNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by IncrementalGenerator");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using ResultBoxes;");
        sb.AppendLine("using Sekiban.Pure;");
        sb.AppendLine("using Sekiban.Pure.Aggregates;");
        sb.AppendLine("using Sekiban.Pure.Exceptions;");
        sb.AppendLine("using Sekiban.Pure.Events;");
        sb.AppendLine("using Sekiban.Pure.Documents;");
        sb.AppendLine("using Sekiban.Pure.Extensions;");
        sb.AppendLine("using System.Text.Json;");
        var baseName = rootNamespace.Replace(".", "");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {baseName}DomainTypes");
        sb.AppendLine("    {");
        sb.AppendLine(
            "        public static SekibanDomainTypes Generate(JsonSerializerOptions jsonSerializerOptions = null)");
        sb.AppendLine(
            $"            => new(new {baseName}EventTypes(), new {baseName}AggregateTypes(), new {baseName}AggregateProjectorSpecifier(), new {baseName}QueryTypes(), new {baseName}MultiProjectorTypes(), jsonSerializerOptions);");
        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }

}