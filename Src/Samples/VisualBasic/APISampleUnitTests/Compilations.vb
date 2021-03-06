' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<TestClass()>
Public Class Compilations

    <TestMethod()>
    Sub EndToEndCompileAndRun()
        Dim expression = "6 * 7"
        Dim code =
<code>
Public Module Calculator
    Public Function Evaluate() As Object
        Return $
    End Function
End Module
</code>.GetCode().Replace("$", expression)

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim comp = VisualBasicCompilation.Create(
            "calc.dll",
            options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees:={tree},
            references:={MetadataReference.CreateFromAssembly(GetType(Object).Assembly),
                        MetadataReference.CreateFromAssembly(GetType(CompilerServices.StandardModuleAttribute).Assembly)})

        Dim compiledAssembly As Assembly
        Using stream = New MemoryStream()
            Dim compileResult = comp.Emit(stream)
            Assert.IsTrue(compileResult.Success)
            compiledAssembly = Assembly.Load(stream.GetBuffer())
        End Using

        Dim calculator = compiledAssembly.GetType("Calculator")
        Dim evaluate = calculator.GetMethod("Evaluate")
        Dim answer = evaluate.Invoke(Nothing, Nothing).ToString()
        Assert.AreEqual("42", answer)
    End Sub

    <TestMethod()>
    Sub GetErrorsAndWarnings()
        Dim code =
<code>
Module Module1
    Function Main() As Integer
    End Function
End Module
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim comp = VisualBasicCompilation.Create(
            "program.exe",
            syntaxTrees:={tree},
            references:={MetadataReference.CreateFromAssembly(GetType(Object).Assembly),
                        MetadataReference.CreateFromAssembly(GetType(CompilerServices.StandardModuleAttribute).Assembly)})

        Dim errorsAndWarnings = comp.GetDiagnostics()
        Assert.AreEqual(1, errorsAndWarnings.Count())

        Dim err As Diagnostic = errorsAndWarnings.First()
        Assert.AreEqual("Function 'Main' doesn't return a value on all code paths. Are you missing a 'Return' statement?", err.GetMessage(CultureInfo.InvariantCulture))

        Dim errorLocation = err.Location
        Assert.AreEqual(12, errorLocation.SourceSpan.Length)

        Dim programText = errorLocation.SourceTree.GetText()
        Assert.AreEqual("End Function", programText.ToString(errorLocation.SourceSpan))

        Dim span = err.Location.GetLineSpan()
        Assert.AreEqual(4, span.StartLinePosition.Character)
        Assert.AreEqual(2, span.StartLinePosition.Line)
    End Sub
End Class
