﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportLanguageService(GetType(ISyntaxFormattingService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicSyntaxFormattingService
        Inherits AbstractSyntaxFormattingService

        Private ReadOnly lazyExportedRules As Lazy(Of IEnumerable(Of IFormattingRule))

        Friend Sub New()
        End Sub

        <ImportingConstructor>
        Sub New(<ImportMany> rules As IEnumerable(Of Lazy(Of IFormattingRule, OrderableLanguageMetadata)))
            Me.lazyExportedRules = New Lazy(Of IEnumerable(Of IFormattingRule))(
                Function()
                    Return ExtensionOrderer.Order(rules).Where(Function(x) x.Metadata.Language = LanguageNames.VisualBasic).Select(Function(x) x.Value).Concat(New DefaultOperationProvider()).ToImmutableArray()
                End Function)
        End Sub

        Public Overrides Function GetDefaultFormattingRules() As IEnumerable(Of IFormattingRule)
            Return lazyExportedRules.Value
        End Function

        Protected Overrides Function CreateAggregatedFormattingResult(node As SyntaxNode, results As IList(Of AbstractFormattingResult), Optional formattingSpans As SimpleIntervalTree(Of TextSpan) = Nothing) As IFormattingResult
            Return New AggregatedFormattingResult(node, results, formattingSpans)
        End Function

        Protected Overrides Function Format(root As SyntaxNode, optionSet As OptionSet, formattingRules As IEnumerable(Of IFormattingRule), token1 As SyntaxToken, token2 As SyntaxToken, cancellationToken As CancellationToken) As AbstractFormattingResult
            Return New VisualBasicFormatEngine(root, optionSet, formattingRules, token1, token2).Format(cancellationToken)
        End Function
    End Class
End Namespace