// Most of the code is taken from SharpLab (https://github.com/ashmind/SharpLab/blob/main/source/Server/Decompilation/JitAsmDecompiler.cs).
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Iced.Intel;

using Microsoft.Diagnostics.Runtime;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler
{
    internal sealed class JitAsmDecompiler
    {
        private static readonly FormatterOptions FormatterOptions = new FormatterOptions
        {
            HexPrefix = "0x",
            HexSuffix = null,
            UppercaseHex = false,
            SpaceAfterOperandSeparator = true
        };

        private readonly Pool<ClrRuntime> _runtimePool;

        public static string Decompile(Stream dll)
        {
            if (dll is null)
                throw new ArgumentNullException(nameof(dll));

            dll.Seek(0, SeekOrigin.Begin);

            var jitDecompilation = new StringWriter();
            var jitDecompiler = new JitAsmDecompiler(Defaults.DefaultRuntimePool);
            jitDecompiler.Decompile(dll, jitDecompilation);

            return jitDecompilation.ToString();
        }

        private JitAsmDecompiler(Pool<ClrRuntime> runtimePool)
        {
            _runtimePool = runtimePool;
        }

        private void Decompile(Stream dll, TextWriter codeWriter)
        {
            using (var loadContext = new CustomAssemblyLoadContext(shouldShareAssembly: _ => true))
            {
                var assembly = loadContext.LoadFromStream(dll);
                ValidateStaticConstructors(assembly);

                using var runtimeLease = _runtimePool.GetOrCreate();
                var runtime = runtimeLease.Object;
                runtime.Flush();

                var context = new JitWriteContext(codeWriter, runtime);

                WriteJitInfo(runtime.ClrInfo, codeWriter);
                foreach (var type in assembly.DefinedTypes)
                {
                    if (type.IsNested)
                        continue; // it's easier to handle nested generic types recursively, so we suppress all nested for consistency
                    DisassembleAndWriteMembers(context, type);
                }
            }
        }

        private void ValidateStaticConstructors(Assembly assembly)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                foreach (var constructor in type.DeclaredConstructors)
                {
                    if (constructor.IsStatic)
                        throw new NotSupportedException($"Type {type} has a static constructor, which is not supported by SharpLab JIT decompiler.");
                }
            }
        }

        private void WriteJitInfo(ClrInfo clr, TextWriter writer)
        {
            writer.WriteLine(
                "; {0:G} CLR {1} on {2}",
                clr.Flavor, clr.Version, clr.DacInfo.TargetArchitecture.ToString("G").ToLowerInvariant()
            );
        }

        private void DisassembleAndWriteMembers(JitWriteContext context, TypeInfo type, ImmutableArray<Type>? genericArgumentTypes = null)
        {
            if (type.IsGenericTypeDefinition)
            {
                if (TryDisassembleAndWriteMembersOfGeneric(context, type, genericArgumentTypes))
                    return;
            }

            foreach (var constructor in type.DeclaredConstructors)
            {
                DisassembleAndWriteMethod(context, constructor);
            }

            foreach (var method in type.DeclaredMethods)
            {
                if (method.IsAbstract)
                    continue;
                DisassembleAndWriteMethod(context, method);
            }

            foreach (var nested in type.DeclaredNestedTypes)
            {
                DisassembleAndWriteMembers(context, nested, genericArgumentTypes);
            }
        }

        private bool TryDisassembleAndWriteMembersOfGeneric(JitWriteContext context, TypeInfo type, ImmutableArray<Type>? parentArgumentTypes = null)
        {
            if (parentArgumentTypes != null)
            {
                var genericInstance = type.MakeGenericType(parentArgumentTypes.Value.ToArray());
                DisassembleAndWriteMembers(context, genericInstance.GetTypeInfo(), parentArgumentTypes);
                return true;
            }

            return false;
        }

        private void DisassembleAndWriteMethod(JitWriteContext context, MethodBase method)
        {
            if ((method.MethodImplementationFlags & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime)
            {
                WriteSignatureFromReflection(context, method);
                context.Writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
                return;
            }

            if (method.DeclaringType?.IsGenericTypeDefinition ?? false)
            {
                WriteIgnoredOpenGeneric(context, method);
                return;
            }

            if (method.IsGenericMethodDefinition)
            {
                DisassembleAndWriteGenericMethod(context, (MethodInfo)method);
                return;
            }

            DisassembleAndWriteSimpleMethod(context, method);
        }

        private void DisassembleAndWriteGenericMethod(JitWriteContext context, MethodInfo method) => WriteIgnoredOpenGeneric(context, method);

        private void DisassembleAndWriteSimpleMethod(JitWriteContext context, MethodBase method)
        {
            var handle = method.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);

            var clrMethod = context.Runtime.GetMethodByHandle((ulong)handle.Value.ToInt64());
            var regions = FindNonEmptyHotColdInfo(clrMethod);

            if (clrMethod == null || regions == null)
            {
                context.Runtime.Flush();
                clrMethod = context.Runtime.GetMethodByHandle((ulong)handle.Value.ToInt64());
                regions = FindNonEmptyHotColdInfo(clrMethod);
            }

            if (clrMethod == null || regions == null)
            {
                var address = (ulong)handle.GetFunctionPointer().ToInt64();
                clrMethod = context.Runtime.GetMethodByAddress(address);
                regions = FindNonEmptyHotColdInfo(clrMethod);
            }

            var writer = context.Writer;
            if (clrMethod != null)
            {
                writer.WriteLine();
                writer.WriteLine(clrMethod.GetFullSignature());
            }
            else
            {
                WriteSignatureFromReflection(context, method);
            }

            if (regions == null)
            {
                if (method.IsGenericMethod || (method.DeclaringType?.IsGenericType ?? false))
                {
                    writer.WriteLine("    ; Failed to find HotColdInfo for generic method (reference types?).");
                    return;
                }
                writer.WriteLine("    ; Failed to find HotColdRegions.");
                return;
            }

            var methodAddress = regions.HotStart;
            var methodLength = regions.HotSize;

            var reader = new MemoryCodeReader(new IntPtr(unchecked((long)methodAddress)), methodLength);
            var decoder = Decoder.Create(MapArchitectureToBitness(context.Runtime.DataTarget.Architecture), reader);

            var instructions = new InstructionList();
            decoder.IP = methodAddress;
            while (decoder.IP < (methodAddress + methodLength))
            {
                decoder.Decode(out instructions.AllocUninitializedElement());
            }

            var resolver = new JitAsmSymbolResolver(context.Runtime, methodAddress, methodLength);
            var formatter = new IntelFormatter(FormatterOptions, resolver);
            var output = new StringOutput();
            foreach (ref var instruction in instructions)
            {
                formatter.Format(instruction, output);

                writer.Write("    L");
                writer.Write((instruction.IP - methodAddress).ToString("x4"));
                writer.Write(": ");
                writer.WriteLine(output.ToStringAndReset());
            }
        }

        private void WriteIgnoredOpenGeneric(JitWriteContext context, MethodBase method)
        {
            WriteSignatureFromReflection(context, method);
            var writer = context.Writer;
            writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
        }

        private static void WriteSignatureFromReflection(JitWriteContext context, MethodBase method)
        {
            context.Writer.WriteLine();

            var md = (ulong)method.MethodHandle.Value.ToInt64();
            var signature = context.Runtime.DacLibrary.SOSDacInterface.GetMethodDescName(md);

            context.Writer.WriteLine(signature ?? "Unknown Method");
        }

        private HotColdRegions FindNonEmptyHotColdInfo(ClrMethod method)
        {
            if (method == null)
                return null;

            // I can't really explain this, but it seems that some methods
            // are present multiple times in the same type -- one compiled
            // and one not compiled. A bug in clrmd?
            if (method.HotColdInfo.HotSize > 0)
                return method.HotColdInfo;

            if (method.Type == null)
                return null;

            var methodSignature = method.GetFullSignature();
            foreach (var other in method.Type.Methods)
            {
                if (other.MetadataToken == method.MetadataToken && other.GetFullSignature() == methodSignature && other.HotColdInfo.HotSize > 0)
                    return other.HotColdInfo;
            }

            return null;
        }

        private int MapArchitectureToBitness(Architecture architecture) => architecture switch
        {
            Architecture.Amd64 => 64,
            Architecture.X86 => 32,
            _ => throw new Exception($"Unsupported architecture {architecture}.")
        };

        private class JitWriteContext
        {
            public JitWriteContext(TextWriter writer, ClrRuntime runtime)
            {
                Writer = writer;
                Runtime = runtime;
            }

            public TextWriter Writer { get; }
            public ClrRuntime Runtime { get; }
        }
    }
}
