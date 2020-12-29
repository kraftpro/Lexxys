// Lexxys Infrastructural library.
// file: AssemblyInfo.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !NETCORE
[assembly: AssemblyTitle("Lexxys.dll")]
[assembly: AssemblyDescription("Common Infrastructure Library")]
[assembly: AssemblyConfiguration("")]

[assembly: AssemblyCopyright("Copyright � 2001-2016.")]

[assembly: AssemblyVersion("2.5.3.04261")] // format: major.minor.revision.MMDDi

[assembly: Guid("00000009-9fc2-4682-aece-e76885fd83f3")]
[assembly: ComVisible(false)]
#endif

#if DEBUG
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented")]
#endif

[assembly: InternalsVisibleTo("LexxysTest")]
[assembly: InternalsVisibleTo("TestPizza")]
[assembly: InternalsVisibleTo("Lexxys.Tests")]
[assembly: InternalsVisibleTo("Lexxys.Tests.Core")]
[assembly: InternalsVisibleTo("Lexxys.Explorables")]
[assembly: InternalsVisibleTo("Lexxys.Tests1")]
[assembly: InternalsVisibleTo("Lexxys.Test.Console")]


