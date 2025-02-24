using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
namespace Sekiban.Pure.SourceGenerator;

[Generator]
public class AggregateTypesGenerator : IIncrementalGenerator
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
                var commandTypes = ImmutableArray.CreateBuilder<CommandWithHandlerValues>();

                commandTypes.AddRange(GetEventValues(compilation, types));

                // Generate source code
                var rootNamespace = compilation.AssemblyName ?? throw new ApplicationException("AssemblyName is null");
                var sourceCode = GenerateSourceCode(commandTypes.ToImmutable(), rootNamespace);
                ctx.AddSource("AggregateTypes.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            });

    }
    public ImmutableArray<CommandWithHandlerValues> GetEventValues(
        Compilation compilation,
        ImmutableArray<SyntaxNode> types)
    {
        var iEventPayloadSymbol = compilation.GetTypeByMetadataName("Sekiban.Pure.Aggregates.IAggregatePayload");
        if (iEventPayloadSymbol == null)
            return new ImmutableArray<CommandWithHandlerValues>();
        var eventTypes = ImmutableArray.CreateBuilder<CommandWithHandlerValues>();
        foreach (var typeSyntax in types)
        {
            var model = compilation.GetSemanticModel(typeSyntax.SyntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(typeSyntax) as INamedTypeSymbol ??
                throw new ApplicationException("TypeSymbol is null");
            var allInterfaces = typeSymbol.AllInterfaces.ToList();
            if (typeSymbol.AllInterfaces.Any(m => m.Equals(iEventPayloadSymbol, SymbolEqualityComparer.Default)))
            {
                var interfaceImplementation = typeSymbol.AllInterfaces.First(
                    m => m.Equals(iEventPayloadSymbol, SymbolEqualityComparer.Default));
                eventTypes.Add(
                    new CommandWithHandlerValues
                    {
                        InterfaceName = interfaceImplementation.Name,
                        RecordName = typeSymbol.ToDisplayString()
                    });
            }
        }
        return eventTypes.ToImmutable();
    }

    private string GenerateSourceCode(ImmutableArray<CommandWithHandlerValues> eventTypes, string rootNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by IncrementalGenerator");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using ResultBoxes;");
        sb.AppendLine("using Sekiban.Pure;");
        sb.AppendLine("using Sekiban.Pure.Aggregates;");
        sb.AppendLine("using Sekiban.Pure.Exceptions;");
        sb.AppendLine("using Sekiban.Pure.Events;");
        sb.AppendLine("using Sekiban.Pure.Documents;");
        sb.AppendLine("using Sekiban.Pure.Extensions;");

        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {rootNamespace.Replace(".", "")}AggregateTypes : IAggregateTypes");
        sb.AppendLine("    {");
        sb.AppendLine("        public ResultBox<IAggregate> ToTypedPayload(Aggregate aggregate)");
        sb.AppendLine("            => aggregate.Payload switch");
        sb.AppendLine("            {");






        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IAggregatePayload", 0):
                    sb.AppendLine(
                        $"                {type.RecordName} => aggregate.ToTypedPayload<{type.RecordName}>().Match(ResultBox<IAggregate>.FromValue, ResultBox<IAggregate>.FromException),");
                    break;
            }
        }

        sb.AppendLine("            _ => ResultBox<IAggregate>.FromException(");
        sb.AppendLine(
            "       new SekibanAggregateTypeException($\"Payload Type {aggregate.Payload.GetType().Name} Not Found\"))");
        sb.AppendLine("        };");

        sb.AppendLine();
        sb.AppendLine("        public List<Type> GetAggregateTypes()");
        sb.AppendLine("        {");
        sb.AppendLine("            return new List<Type>");
        sb.AppendLine("            {");
        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IAggregatePayload", 0):
                    sb.AppendLine($"                typeof({type.RecordName}),");
                    break;
            }
        }
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    public class CommandWithHandlerValues
    {
        public string InterfaceName { get; set; } = string.Empty;
        public string RecordName { get; set; } = string.Empty;
        public int TypeCount { get; set; }
    }
}
