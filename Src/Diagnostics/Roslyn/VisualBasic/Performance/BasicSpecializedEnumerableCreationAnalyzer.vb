﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSpecializedEnumerableCreationAnalyzer
        Inherits SpecializedEnumerableCreationAnalyzer

        Protected Overrides Sub GetCodeBlockStartedAnalyzer(context As CompilationStartAnalysisContext, genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
            context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf New CodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).Initialize)
        End Sub

        Private NotInheritable Class CodeBlockStartedAnalyzer
            Inherits AbstractCodeBlockStartedAnalyzer(Of SyntaxKind)

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Protected Overrides Sub GetSyntaxAnalyzer(context As CodeBlockStartAnalysisContext(Of SyntaxKind), genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                context.RegisterSyntaxNodeAction(AddressOf New SyntaxAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).AnalyzeNode, SyntaxKind.ReturnStatement)
            End Sub
        End Class

        Private NotInheritable Class SyntaxAnalyzer
            Inherits AbstractSyntaxAnalyzer

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim expressionsToAnalyze = context.Node.DescendantNodes().Where(Function(n) ShouldAnalyzeExpression(n, context.SemanticModel))

                For Each expression In expressionsToAnalyze
                    Select Case expression.VisualBasicKind()
                        Case SyntaxKind.ArrayCreationExpression
                            AnalyzeArrayCreationExpression(DirectCast(expression, ArrayCreationExpressionSyntax), AddressOf context.ReportDiagnostic)
                        Case SyntaxKind.CollectionInitializer
                            AnalyzeCollectionInitializerExpression(DirectCast(expression, CollectionInitializerSyntax), expression, AddressOf context.ReportDiagnostic)
                        Case SyntaxKind.SimpleMemberAccessExpression
                            AnalyzeMemberAccessName(DirectCast(expression, MemberAccessExpressionSyntax).Name, context.SemanticModel, AddressOf context.ReportDiagnostic)
                    End Select
                Next
            End Sub

            Private Function ShouldAnalyzeExpression(expression As SyntaxNode, semanticModel As SemanticModel) As Boolean
                Select Case expression.VisualBasicKind()
                    Case SyntaxKind.ArrayCreationExpression,
                         SyntaxKind.CollectionInitializer
                        Return ShouldAnalyzeArrayCreationExpression(expression, semanticModel)
                    Case SyntaxKind.SimpleMemberAccessExpression
                        Return True
                End Select

                Return False
            End Function

            Private Shared Sub AnalyzeArrayCreationExpression(arrayCreationExpression As ArrayCreationExpressionSyntax, addDiagnostic As Action(Of Diagnostic))
                If arrayCreationExpression.RankSpecifiers.Count = 1 Then

                    ' Check for explicit specification of empty or singleton array
                    Dim literalRankSpecifier = DirectCast(arrayCreationExpression.RankSpecifiers(0).ChildNodes() _
                        .SingleOrDefault(Function(n) n.VisualBasicKind() = SyntaxKind.NumericLiteralExpression),
                        LiteralExpressionSyntax)

                    If literalRankSpecifier IsNot Nothing Then
                        Debug.Assert(literalRankSpecifier.Token.Value IsNot Nothing)
                        AnalyzeArrayLength(DirectCast(literalRankSpecifier.Token.Value, Integer), arrayCreationExpression, addDiagnostic)
                        Return
                    End If
                End If

                AnalyzeCollectionInitializerExpression(arrayCreationExpression.Initializer, arrayCreationExpression, addDiagnostic)
            End Sub

            Private Shared Sub AnalyzeCollectionInitializerExpression(initializer As CollectionInitializerSyntax, arrayCreationExpression As SyntaxNode, addDiagnostic As Action(Of Diagnostic))
                ' Check length of initializer list for empty or singleton array
                If initializer IsNot Nothing Then
                    AnalyzeArrayLength(initializer.Initializers.Count, arrayCreationExpression, addDiagnostic)
                End If
            End Sub
        End Class
    End Class
End Namespace
