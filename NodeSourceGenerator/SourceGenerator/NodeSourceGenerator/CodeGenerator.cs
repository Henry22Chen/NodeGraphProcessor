using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NodeSourceGenerator
{
    [Generator]
    public class CodeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var compilation = context.Compilation;
            var baseNodeSymbol = compilation.GetTypeByMetadataName("GraphProcessor.BaseNode");
            var partialNodeAttributeSymbol = compilation.GetTypeByMetadataName("GraphProcessor.PartialNodeAttribute");

            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var typeSymbol = model.GetDeclaredSymbol(classDeclaration);

                if (typeSymbol == null || typeSymbol.IsAbstract)
                    continue;

                // if (!typeSymbol.DeclaringSyntaxReferences.Any(r =>
                //         r.GetSyntax() is ClassDeclarationSyntax syntax &&
                //         syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))))
                //     continue;

                if (!typeSymbol.GetAttributes().Any(ad =>
                        ad.AttributeClass?.Equals(partialNodeAttributeSymbol, SymbolEqualityComparer.Default) ?? false))
                    continue;

                var currentType = typeSymbol;
                bool isBaseNodeDescendant = false;
                while (currentType != null)
                {
                    if (currentType.Equals(baseNodeSymbol, SymbolEqualityComparer.Default))
                    {
                        isBaseNodeDescendant = true;
                        break;
                    }

                    currentType = currentType.BaseType;
                }

                if (!isBaseNodeDescendant)
                    continue;

                var source = GeneratePartialClass(typeSymbol);
                context.AddSource($"{typeSymbol.Name}.g.cs", source);
            }
        }

        public IEnumerable<IFieldSymbol> GetOrderedFields(INamedTypeSymbol typeSymbol, bool isDescending = true)
        {
            var allFields = new List<(IFieldSymbol field, int level)>();

            void CollectFields(INamedTypeSymbol currentType, int level)
            {
                var fields = currentType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => !f.IsStatic &&
                                (
                                    f.DeclaredAccessibility == Accessibility.Public ||
                                    f.DeclaredAccessibility == Accessibility.Protected ||
                                    f.DeclaredAccessibility == Accessibility.Internal ||
                                    (f.DeclaredAccessibility == Accessibility.Private &&
                                     ReferenceEquals(currentType, typeSymbol))
                                )
                    );

                allFields.AddRange(fields.Select(field => (field, level)));
            }

            int inheritanceLevel = 0;
            var current = typeSymbol;
            while (current != null)
            {
                CollectFields(current, inheritanceLevel++);
                current = current.BaseType;
            }

            int FieldSort(IFieldSymbol field)
            {
                var location = field.Locations.FirstOrDefault();
                if (location == null)
                    return 0;
                return location.GetLineSpan().StartLinePosition.Line;
            }

            if (!isDescending)
            {
                return allFields
                    .OrderByDescending(tuple => tuple.level) 
                    .ThenBy(tuple => FieldSort(tuple.field)) 
                    .Select(tuple => tuple.field); 
            }

            return allFields
                .OrderBy(tuple => tuple.level) 
                .ThenByDescending(tuple => FieldSort(tuple.field)) 
                .Select(tuple => tuple.field); 
        }

        private IPropertySymbol? GetPropertySymbol(string name, INamedTypeSymbol? typeSymbol)
        {
            while (true)
            {
                if (typeSymbol == null)
                {
                    return null;
                }

                var properties = typeSymbol.GetMembers(name).OfType<IPropertySymbol>();
                var propertySymbols = properties as IPropertySymbol[] ?? properties.ToArray();
                if (!propertySymbols.Any())
                {
                    typeSymbol = typeSymbol.BaseType;
                    continue;
                }

                return propertySymbols.FirstOrDefault();
            }
        }

        private string GeneratePartialClass(INamedTypeSymbol typeSymbol)
        {
            var fields = GetOrderedFields(typeSymbol).ToList();
            var inputFields = fields.Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == "InputAttribute")).ToList();
            var outputFields = fields.Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == "OutputAttribute")).ToList();
            var allFields = inputFields.Concat(outputFields).ToList();

            // 创建 using 指令
            var usings = new List<UsingDirectiveSyntax>
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("GraphProcessor"))
            };

            // 创建类声明
            var classDeclaration = SyntaxFactory.ClassDeclaration(typeSymbol.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            // 创建 PrepareInputsGenerated 方法
            var prepareInputsMethod = CreatePrepareInputsMethod(inputFields);
            classDeclaration = classDeclaration.AddMembers(prepareInputsMethod);

            // 创建 InitializeFieldData 方法
            var initializeFieldDataMethod = CreateInitializeFieldDataMethod(allFields);
            classDeclaration = classDeclaration.AddMembers(initializeFieldDataMethod);

            // 创建命名空间声明
            SyntaxNode rootNode;
            if (!typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(typeSymbol.ContainingNamespace.ToString()))
                    .AddMembers(classDeclaration);
                rootNode = namespaceDeclaration;
            }
            else
            {
                rootNode = classDeclaration;
            }

            // 创建编译单元
            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(usings.ToArray())
                .AddMembers((MemberDeclarationSyntax)rootNode)
                .WithLeadingTrivia(SyntaxFactory.Comment("// <auto-generated/>"));

            return compilationUnit.NormalizeWhitespace().ToFullString();
        }

        private MethodDeclarationSyntax CreatePrepareInputsMethod(List<IFieldSymbol> inputFields)
        {
            var statements = new List<StatementSyntax>();

            // 添加 index 变量声明
            statements.Add(SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"))
                    .AddVariables(SyntaxFactory.VariableDeclarator("index")
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ConditionalExpression(
                                SyntaxFactory.IdentifierName("fieldsSortedDescending"),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(-1)),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(inputFields.Count))
                            )
                        ))
                    )
            ));

            // 创建 if (prevNode != null) 块
            var ifStatements = new List<StatementSyntax>();
            foreach (var field in inputFields)
            {
                ifStatements.Add(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("TryReadInputValue"))
                        .AddArgumentListArguments(
                            SyntaxFactory.Argument(
                                SyntaxFactory.ConditionalExpression(
                                    SyntaxFactory.IdentifierName("fieldsSortedDescending"),
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, SyntaxFactory.IdentifierName("index")),
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreDecrementExpression, SyntaxFactory.IdentifierName("index"))
                                )
                            ),
                            SyntaxFactory.Argument(SyntaxFactory.RefExpression(SyntaxFactory.IdentifierName(field.Name))),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("prevNode"))
                        )
                ));
            }

            var ifBlock = SyntaxFactory.Block(ifStatements);
            
            // 创建 else 块
            var elseStatements = new List<StatementSyntax>();
            foreach (var field in inputFields)
            {
                elseStatements.Add(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("TryReadInputValue"))
                        .AddArgumentListArguments(
                            SyntaxFactory.Argument(
                                SyntaxFactory.ConditionalExpression(
                                    SyntaxFactory.IdentifierName("fieldsSortedDescending"),
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, SyntaxFactory.IdentifierName("index")),
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreDecrementExpression, SyntaxFactory.IdentifierName("index"))
                                )
                            ),
                            SyntaxFactory.Argument(SyntaxFactory.RefExpression(SyntaxFactory.IdentifierName(field.Name)))
                        )
                ));
            }

            var elseBlock = SyntaxFactory.Block(elseStatements);
            statements.Add(SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName("prevNode"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                ),
                ifBlock,
                SyntaxFactory.ElseClause(elseBlock)
            ));

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "PrepareInputsGenerated")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("prevNode"))
                        .WithType(SyntaxFactory.ParseTypeName("BaseNode"))
                        .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))
                )
                .WithBody(SyntaxFactory.Block(statements));
        }

        private MethodDeclarationSyntax CreateInitializeFieldDataMethod(List<IFieldSymbol> allFields)
        {
            var statements = new List<StatementSyntax>();

            // 添加 _needsInspector 赋值
            statements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("_needsInspector"),
                    SyntaxFactory.LiteralExpression(
                        allFields.Any(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == "ShowInInspector"))
                            ? SyntaxKind.TrueLiteralExpression
                            : SyntaxKind.FalseLiteralExpression
                    )
                )
            ));

            // 添加 index 变量声明
            statements.Add(SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"))
                    .AddVariables(SyntaxFactory.VariableDeclarator("index")
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ConditionalExpression(
                                SyntaxFactory.IdentifierName("fieldsSortedDescending"),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(-1)),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(allFields.Count))
                            )
                        ))
                    )
            ));

            // 为每个字段添加 nodeFields 赋值
            foreach (var field in allFields)
            {
                var attributes = field.GetAttributes();
                var inputAttr = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "InputAttribute");
                var outputAttr = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "OutputAttribute");
                var tooltipAttr = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "TooltipAttribute");
                var verticalAttr = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "VerticalAttribute");

                var isInput = inputAttr != null;
                var attr = isInput ? inputAttr : outputAttr;
                var name = attr?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? field.Name;
                var allowMultiple =
                    (bool)(attr?.NamedArguments.FirstOrDefault(na => na.Key == "allowMultiple").Value.Value ??
                           (isInput ? false : true));
                var tooltip = tooltipAttr?.ConstructorArguments.FirstOrDefault().Value?.ToString();
                var isVertical = verticalAttr != null;

                var indexExpression = SyntaxFactory.ConditionalExpression(
                    SyntaxFactory.IdentifierName("fieldsSortedDescending"),
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, SyntaxFactory.IdentifierName("index")),
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreDecrementExpression, SyntaxFactory.IdentifierName("index"))
                );

                var newNodeFieldInfo = SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("NodeFieldInformation"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(field.Name))),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(isInput ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(allowMultiple ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)),
                        SyntaxFactory.Argument(tooltip != null
                            ? SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(tooltip))
                            : SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(isVertical ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)),
                        SyntaxFactory.Argument(indexExpression)
                    );

                statements.Add(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.ElementAccessExpression(
                            SyntaxFactory.IdentifierName("nodeFields"))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(field.Name)))
                            ),
                        newNodeFieldInfo
                    )
                ));
            }

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "InitializeFieldData")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                .WithBody(SyntaxFactory.Block(statements));
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                classDeclaration.AttributeLists.SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "PartialNode"))
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}