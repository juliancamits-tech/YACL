using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace YACL.BetterEnums.SourceGeneration;

[Generator]
public class MainGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        var declaration = context.SyntaxProvider
         .CreateSyntaxProvider(
             predicate: static (s, _) => IsPotential(s),
             transform: static (ctx, _) => GetInfo(ctx))
         .Where(static m => m is not null);

        var compilationAndDeclarations = context.CompilationProvider.Combine(declaration.Collect());

        context.RegisterSourceOutput(compilationAndDeclarations,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsPotential(SyntaxNode node)
    {
        return node is EnumDeclarationSyntax;
    }

    private static EnumInfo GetInfo(GeneratorSyntaxContext context)
    {
        if (context.Node is not EnumDeclarationSyntax declaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.DeclaredAccessibility == Accessibility.Private)
            return null;

        var info = new EnumInfo
        {
            EnumName = symbol.ToDisplayString(),
            NameSpace = symbol.ContainingNamespace.ToDisplayString(),
            TypeOfEnum = symbol.EnumUnderlyingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "int"
        };

        foreach (var item in declaration.Members)
        {
            var enumItem = new EnumItem()
            {
                TextValue = item.Identifier.ValueText,
            };

            // 1. Obtener el símbolo del miembro (los miembros de un enum son representados como IFieldSymbol)
            if (context.SemanticModel.GetDeclaredSymbol(item) is IFieldSymbol fieldSymbol)
            {
                // Obtener el valor numérico (explícito o generado automáticamente)
                enumItem.NumericValue = fieldSymbol.ConstantValue?.ToString();


                // 2. Buscar el atributo específico por su nombre completo (Fully Qualified Name)
                var stepAttribute = fieldSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "BetterEnums.Abstractions.Attributes.EnumStepAttribute");

                if (stepAttribute != null)
                {
                    // 3. Extraer los argumentos posicionales del constructor (lo que llamamos el constructor primario)
                    // ConstructorArguments mapea en orden los parámetros que recibe el constructor del atributo.
                    var firstArgument = stepAttribute.ConstructorArguments.FirstOrDefault();

                    for (var x = 0; x < 3; x++)
                    {
                        var argument = stepAttribute.ConstructorArguments[x];
                        var value = argument.Value;
                        switch (x)
                        {
                            case 0:
                                enumItem.NextItem = argument.IsNull ? "" : value.ToString();
                                break;
                            case 1:
                                enumItem.PrevItem = argument.IsNull ? "" : value.ToString();
                                break;
                            case 2:
                                enumItem.ErrorItem = argument.IsNull ? "" : value.ToString();
                                break;
                            default:
                                continue;
                        }
                    }
                }
            }


            info.AddEnumItem(enumItem);
        }



        return info;
    }

    private static void Execute(Compilation _, ImmutableArray<EnumInfo> infos, SourceProductionContext context)
    {
        foreach (var namespaceInfo in infos)
        {
            var fileText = ExtensionMethodGenerator.GenerateExtensionFile(namespaceInfo);
            context.AddSource($"{namespaceInfo.CleanNameSpace}EnumsExtension.g.cs", SourceText.From(fileText, Encoding.UTF8));
        }
    }
}
