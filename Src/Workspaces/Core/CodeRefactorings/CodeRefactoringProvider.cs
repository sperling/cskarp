// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Inherit this type to provide source code refactorings.
    /// Remember to use <see cref="ExportCodeRefactoringProviderAttribute"/> so the host environment can offer your refactorings in a UI.
    /// </summary>
    public abstract class CodeRefactoringProvider
    {
        /// <summary>
        /// Gets refactorings that are applicable within the span of the document represented as a list of <see cref="CodeAction"/>'s.
        /// </summary>
        /// <returns>A list of zero or more applicable <see cref="CodeAction"/>'s. It is also safe to return null if there are none.</returns>
        public abstract Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context);
    }
}