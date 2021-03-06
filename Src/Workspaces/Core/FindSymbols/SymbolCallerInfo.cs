﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Contains information about a call from one symbol to another.  The symbol making the call is
    /// stored in CallingSymbol and the symbol that the call was made to is stored in CalledSymbol.
    /// Whether or not the call is direct or indirect is also stored.  A direct call is a call that
    /// does not go through any other symbols in the inheritance hierarchy of CalledSymbol, while an
    /// indirect call does go through the inheritance hierarchy.  For example, calls through a base
    /// member that this symbol overrides, or through an interface member that this symbol
    /// implements will be considered 'indirect'. 
    /// </summary>
    public struct SymbolCallerInfo
    {
        /// <summary>
        /// The symbol that is calling the symbol being called.
        /// </summary>
        public ISymbol CallingSymbol { get; private set; }

        /// <summary>
        /// The locations inside the calling symbol where the called symbol is referenced.
        /// </summary>
        public IEnumerable<Location> Locations { get; private set; }

        /// <summary>
        /// The symbol being called.
        /// </summary>
        public ISymbol CalledSymbol { get; private set; }

        /// <summary>
        /// True if the CallingSymbol is directly calling CalledSymbol.  False if it is calling a
        /// symbol in the inheritance hierarchy of the CalledSymbol.  For example, if the called
        /// symbol is a class method, then an indirect call might be through an interface method that
        /// the class method implements.
        /// </summary>
        public bool IsDirect { get; private set; }

        internal SymbolCallerInfo(ISymbol callingSymbol, ISymbol calledSymbol, IEnumerable<Location> locations, bool isDirect)
            : this()
        {
            this.CallingSymbol = callingSymbol;
            this.CalledSymbol = calledSymbol;
            this.IsDirect = isDirect;
            this.Locations = locations;
        }
    }
}