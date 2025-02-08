using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
namespace Sekiban.Pure.SourceGenerator;

[Generator]
public class QueryExecutionExtensionGenerator : IIncrementalGenerator
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
                var commandTypes = ImmutableArray.CreateBuilder<QueryWithHandlerValues>();

                commandTypes.AddRange(GetCommandWithHandlerValues(compilation, types));

                // Generate source code
                var rootNamespace = compilation.AssemblyName ?? throw new Exception();
                var sourceCode = GenerateSourceCode(commandTypes.ToImmutable(), rootNamespace);
                ctx.AddSource("QueryExecutorExtension.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            });

    }
    public ImmutableArray<QueryWithHandlerValues> GetCommandWithHandlerValues(
        Compilation compilation,
        ImmutableArray<SyntaxNode> types)
    {
        var iListQueryWithHandlerSymbol
            = compilation.GetTypeByMetadataName("Sekiban.Pure.Query.IMultiProjectionListQuery`3");
        var iQueryWithHandlerSymbol
            = compilation.GetTypeByMetadataName("Sekiban.Pure.Query.IMultiProjectionQuery`3");
        if (iListQueryWithHandlerSymbol == null && iQueryWithHandlerSymbol == null)
            return new ImmutableArray<QueryWithHandlerValues>();
        var eventTypes = ImmutableArray.CreateBuilder<QueryWithHandlerValues>();
        foreach (var typeSyntax in types)
        {
            var model = compilation.GetSemanticModel(typeSyntax.SyntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(typeSyntax) as INamedTypeSymbol ?? throw new Exception();
            var allInterfaces = typeSymbol.AllInterfaces.ToList();
            var matchingInterface = typeSymbol.AllInterfaces.FirstOrDefault(
                m => m.OriginalDefinition is not null && 
                     (m.OriginalDefinition.Name == iListQueryWithHandlerSymbol?.Name || 
                      m.OriginalDefinition.Name == iQueryWithHandlerSymbol?.Name));
            
            if (matchingInterface != null)
            {
                eventTypes.Add(
                    new QueryWithHandlerValues
                    {
                        InterfaceName = matchingInterface.Name,
                        RecordName = typeSymbol.ToDisplayString(),
                        TypeCount = matchingInterface.TypeArguments.Length,
                        Generic1Name = matchingInterface.TypeArguments[0].ToDisplayString(),
                        Generic2Name = matchingInterface.TypeArguments[1].ToDisplayString(),
                        Generic3Name = matchingInterface.TypeArguments.Length > 2
                            ? matchingInterface.TypeArguments[2].ToDisplayString()
                            : string.Empty
                    });
            }
        }
        return eventTypes.ToImmutable();
    }


    private string GenerateSourceCode(ImmutableArray<QueryWithHandlerValues> eventTypes, string rootNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by IncrementalGenerator");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using ResultBoxes;");
        sb.AppendLine("using Sekiban.Pure;");
        sb.AppendLine("using Sekiban.Pure.Projectors;");
        sb.AppendLine("using Sekiban.Pure.Exceptions;");
        sb.AppendLine("using Sekiban.Pure.Events;");
        sb.AppendLine("using Sekiban.Pure.Command.Handlers;");
        sb.AppendLine("using Sekiban.Pure.Command.Resources;");
        sb.AppendLine("using Sekiban.Pure.Command.Executor;");
        sb.AppendLine("using Sekiban.Pure.Aggregates;");
        sb.AppendLine("using Sekiban.Pure.Documents;");
        sb.AppendLine("using Sekiban.Pure.Query;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class QueryExecutorExtensions");
        sb.AppendLine("    {");

        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IMultiProjectionListQuery", 3):
                    sb.AppendLine(
                        $"        public static Task<ResultBox<ListQueryResult<{type.Generic3Name}>>> Execute(this QueryExecutor queryExecutor, {type.RecordName} query, Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<{type.Generic1Name}>>> repositoryLoader) =>");
                    sb.AppendLine(
                        $"      queryExecutor.ExecuteListWithMultiProjectionFunction<{type.Generic1Name},{type.Generic2Name},{type.Generic3Name}>(");
                    sb.AppendLine("                query,");
                    sb.AppendLine($"                {type.Generic2Name}.HandleFilter,");
                    sb.AppendLine($"                {type.Generic2Name}.HandleSort, repositoryLoader);");
                    sb.AppendLine();
                    break;
                case ("IMultiProjectionQuery", 3):
                    sb.AppendLine(
                        $"        public static Task<ResultBox<{type.Generic3Name}>> Execute(this QueryExecutor queryExecutor, {type.RecordName} query,  Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<{type.Generic1Name}>>> repositoryLoader) =>");
                    sb.AppendLine(
                        $"      queryExecutor.ExecuteWithMultiProjectionFunction<{type.Generic1Name},{type.Generic2Name},{type.Generic3Name}>(");
                    sb.AppendLine("                query,");
                    sb.AppendLine($"                {type.Generic2Name}.HandleQuery, repositoryLoader);");
                    sb.AppendLine();
                    break;
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    public class QueryWithHandlerValues
    {
        public string InterfaceName { get; set; } = string.Empty;
        public string RecordName { get; set; } = string.Empty;
        public int TypeCount { get; set; }
        public string Generic1Name { get; set; } = string.Empty;
        public string Generic2Name { get; set; } = string.Empty;
        public string InjectTypeName { get; set; } = string.Empty;
        public string Generic3Name { get; set; } = string.Empty;
    }
}

