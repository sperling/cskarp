﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class SymbolMatcherTests : EditAndContinueTestBase
    {
        private static void MatchAll(CSharpSymbolMatcher matcher, ImmutableArray<Symbol> members, int startAt)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                var member = members[(i + startAt) % n];
                var other = matcher.MapDefinition((Cci.IDefinition)member);
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void ConcurrentAccess()
        {
            var source =
@"class A
{
    B F;
    D P { get; set; }
    void M(A a, B b, S s, I i) { }
    delegate void D(S s);
    class B { }
    struct S { }
    interface I { }
}
class B
{
    A M<T, U>() where T : A where U : T, I { return null; }
    event D E;
    delegate void D(S s);
    struct S { }
    interface I { }
}";

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var builder = ArrayBuilder<Symbol>.GetInstance();
            var type = compilation1.GetMember<NamedTypeSymbol>("A");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            type = compilation1.GetMember<NamedTypeSymbol>("B");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            var members = builder.ToImmutableAndFree();
            Assert.True(members.Length > 10);

            for (int i = 0; i < 10; i++)
            {
                var matcher = new CSharpSymbolMatcher(
                    null,
                    compilation1.SourceAssembly,
                    default(EmitContext),
                    compilation0.SourceAssembly,
                    default(EmitContext));
                var tasks = new Task[10];
                for (int j = 0; j < tasks.Length; j++)
                {
                    int startAt = i + j + 1;
                    tasks[j] = Task.Run(() =>
                    {
                        MatchAll(matcher, members, startAt);
                        Thread.Sleep(10);
                    });
                }
                Task.WaitAll(tasks);
            }
        }

        [Fact]
        public void TypeArguments()
        {
            const string source =
@"class A<T>
{
    class B<U>
    {
        static A<V> M<V>(A<U>.B<T> x, A<object>.S y)
        {
            return null;
        }
        static A<V> M<V>(A<U>.B<T> x, A<V>.S y)
        {
            return null;
        }
    }
    struct S
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext));
            var members = compilation1.GetMember<NamedTypeSymbol>("A.B").GetMembers("M");
            Assert.Equal(members.Length, 2);
            foreach (var member in members)
            {
                var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void Constraints()
        {
            const string source =
@"interface I<T> where T : I<T>
{
}
class C
{
    static void M<T>(I<T> o) where T : I<T>
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext));
            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
            Assert.NotNull(other);
        }

        [Fact]
        public void CustomModifiers()
        {
            var ilSource =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object modopt(A) [] F(int32 modopt(object) *p) { }
}";
            var metadataRef = CompileIL(ilSource);
            const string source =
@"unsafe class B : A
{
    public override object[] F(int* p) { return null; }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, references: new[] { metadataRef });
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, references: new[] { metadataRef });

            var member1 = compilation1.GetMember<MethodSymbol>("B.F");
            Assert.Equal(((PointerTypeSymbol)member1.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)member1.ReturnType).CustomModifiers.Length, 1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext));
            var other = (MethodSymbol)matcher.MapDefinition((Cci.IMethodDefinition)member1);
            Assert.NotNull(other);
            Assert.Equal(((PointerTypeSymbol)other.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)other.ReturnType).CustomModifiers.Length, 1);
        }
    }
}