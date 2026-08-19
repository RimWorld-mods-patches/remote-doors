using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var decompiler = new CSharpDecompiler(args[0], new DecompilerSettings());
Console.WriteLine(decompiler.DecompileTypeAsString(new FullTypeName(args[1])));
