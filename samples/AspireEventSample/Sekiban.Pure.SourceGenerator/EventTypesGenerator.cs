using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
namespace Sekiban.Pure.SourceGenerator;

[Generator]
public class EventTypesGenerator : IIncrementalGenerator
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
                ctx.AddSource("EventTypes.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            });
    }

    public ImmutableArray<CommandWithHandlerValues> GetEventValues(
        Compilation compilation,
        ImmutableArray<SyntaxNode> types)
    {
        var iEventPayloadSymbol = compilation.GetTypeByMetadataName("Sekiban.Pure.Events.IEventPayload");
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
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using ResultBoxes;");
        sb.AppendLine("using Sekiban.Pure;");
        sb.AppendLine("using Sekiban.Pure.Exceptions;");
        sb.AppendLine("using Sekiban.Pure.Events;");
        sb.AppendLine("using Sekiban.Pure.Documents;");
        sb.AppendLine("using Sekiban.Pure.Extensions;");
        sb.AppendLine("using Sekiban.Pure.Serialize;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");

        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {rootNamespace.Replace(".", "")}EventTypes : IEventTypes");
        sb.AppendLine("    {");
        sb.AppendLine("        public ResultBox<IEvent> GenerateTypedEvent(");
        sb.AppendLine("            IEventPayload payload,");
        sb.AppendLine("            PartitionKeys partitionKeys,");
        sb.AppendLine("            string sortableUniqueId,");
        sb.AppendLine("            int version,");
        sb.AppendLine("            EventMetadata metadata) => payload switch");
        sb.AppendLine("        {");

        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IEventPayload", 0):
                    sb.AppendLine(
                        $"            {type.RecordName} {type.RecordName.Split('.').Last().ToLower()} => new Event<{type.RecordName}>(");
                    sb.AppendLine("                GuidExtensions.CreateVersion7(),");
                    sb.AppendLine($"                {type.RecordName.Split('.').Last().ToLower()},");
                    sb.AppendLine("                partitionKeys,");
                    sb.AppendLine("                sortableUniqueId,");
                    sb.AppendLine("                version,");
                    sb.AppendLine("                metadata),");
                    break;
            }
        }

        sb.AppendLine("            _ => ResultBox<IEvent>.FromException(");
        sb.AppendLine(
            "                new SekibanEventTypeNotFoundException($\"Event Type {payload.GetType().Name} Not Found\"))");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        public ResultBox<IEventDocument> ConvertToEventDocument(");
        sb.AppendLine("            IEvent ev) => ev switch");
        sb.AppendLine("        {");

        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IEventPayload", 0):
                    sb.AppendLine(
                        $"            Event<{type.RecordName}> {type.RecordName.Split('.').Last()}Event => EventDocument<{type.RecordName}>.FromEvent(");
                    sb.AppendLine($"                {type.RecordName.Split('.').Last()}Event),");
                    break;
            }
        }

        sb.AppendLine("            _ => ResultBox<IEventDocument>.FromException(");
        sb.AppendLine(
            "                new SekibanEventTypeNotFoundException($\"Event Type {ev.GetPayload().GetType().Name} Not Found\"))");
        sb.AppendLine("        };");




        sb.AppendLine("        public ResultBox<IEvent> DeserializeToTyped(");
        sb.AppendLine(
            "            EventDocumentCommon common, JsonSerializerOptions serializeOptions) => common.PayloadTypeName switch");
        sb.AppendLine("        {");

        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IEventPayload", 0):
                    sb.AppendLine(
                        $"            nameof({type.RecordName}) => common.ToEvent<{type.RecordName}>(serializeOptions),");
                    break;
            }
        }

        sb.AppendLine("            _ => ResultBox<IEvent>.FromException(");
        sb.AppendLine(
            "                new SekibanEventTypeNotFoundException($\"Event Type {common.PayloadTypeName} Not Found\"))");
        sb.AppendLine("        };");

        sb.AppendLine();
        sb.AppendLine("        public void CheckEventJsonContextOption(JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine(
            "            if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocumentCommon), options) == null)");
        sb.AppendLine("            {");
        sb.AppendLine(
            "                throw new SekibanEventTypeNotFoundException($\"EventDocumentCommon not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocumentCommon))] \");");
        sb.AppendLine("            }");
        sb.AppendLine(
            "            if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocumentCommon[]), options) == null)");
        sb.AppendLine("            {");
        sb.AppendLine(
            "                throw new SekibanEventTypeNotFoundException($\"EventDocumentCommon[] not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocumentCommon[]))] \");");
        sb.AppendLine("            }");
        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IEventPayload", 0):
                    sb.AppendLine(
                        $"            if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<{type.RecordName}>), options) == null)");
                    sb.AppendLine("            {");
                    sb.AppendLine(
                        $"                throw new SekibanEventTypeNotFoundException($\"EventDocument<{type.RecordName}> not found in {{options?.TypeInfoResolver?.GetType().Name ?? string.Empty}}, put attribute [JsonSerializable(typeof(EventDocument<{type.RecordName}>))]\");");
                    sb.AppendLine("            }");
                    sb.AppendLine(
                        $"            if (options?.TypeInfoResolver?.GetTypeInfo(typeof({type.RecordName}), options) == null)");
                    sb.AppendLine("            {");
                    sb.AppendLine(
                        $"                throw new SekibanEventTypeNotFoundException($\"{type.RecordName} not found in {{options?.TypeInfoResolver?.GetType().Name ?? string.Empty}}, put attribute [JsonSerializable(typeof(EventDocument<{type.RecordName}>))]\");");
                    sb.AppendLine("            }");
                    break;
            }
        }
        sb.AppendLine("        }");

        sb.AppendLine();
        sb.AppendLine(
            "        public ResultBox<string> SerializePayloadToJson(ISekibanSerializer serializer, IEvent ev) =>");
        sb.AppendLine("            ev.GetPayload() switch");
        sb.AppendLine("        {");

        foreach (var type in eventTypes)
        {
            switch (type.InterfaceName, type.TypeCount)
            {
                case ("IEventPayload", 0):
                    var typeName = type.RecordName.Split('.').Last();
                    sb.AppendLine($"            {type.RecordName} {typeName.ToLower()} =>");
                    sb.AppendLine(
                        $"                ResultBox.CheckNullWrapTry(() => serializer.Serialize({typeName.ToLower()})),");
                    break;
            }
        }

        sb.AppendLine("            _ => ResultBox<string>.FromException(");
        sb.AppendLine(
            "                new SekibanEventTypeNotFoundException($\"Event Type {ev.GetPayload().GetType().Name} Not Found\"))");
        sb.AppendLine("        };");

        sb.AppendLine("    }");
        sb.AppendLine("/***");
        sb.AppendLine("    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]");
        sb.AppendLine("    [JsonSerializable(typeof(EventDocumentCommon))]");
        sb.AppendLine("    [JsonSerializable(typeof(EventDocumentCommon[]))]");
        foreach (var type in eventTypes)
        {
            sb.AppendLine($"    [JsonSerializable(typeof(EventDocument<{type.RecordName}>))]");
            sb.AppendLine($"    [JsonSerializable(typeof({type.RecordName}))]");
        }
        sb.AppendLine(
            $"    public partial class {rootNamespace.Replace(".", "")}EventsJsonContext : JsonSerializerContext");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("*****/");
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