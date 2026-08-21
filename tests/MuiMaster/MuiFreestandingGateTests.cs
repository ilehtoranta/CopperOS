/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFreestandingGateTests
{
	private static readonly Dictionary<ushort, OpCode> Opcodes = typeof(OpCodes)
		.GetFields(BindingFlags.Public | BindingFlags.Static)
		.Where(field => field.FieldType == typeof(OpCode))
		.Select(field => (OpCode)field.GetValue(null)!)
		.ToDictionary(code => unchecked((ushort)code.Value));

	[Fact]
	public void ProductionSourcesUseOnlyTheConstrainedSubset()
	{
		var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
			"..", "..", "..", "..", ".."));
		var sourceRoot = Path.Combine(root, "src", "Libraries", "muimaster.library");
		Assert.True(Directory.Exists(sourceRoot), sourceRoot);
		var forbidden = new Regex(
			@"\b(throw|try|catch|async|await|yield|lock)\b|\bnew\s+|\b(System\.Collections|System\.Linq|Task|Dictionary|List<)\b|\[\]",
			RegexOptions.CultureInvariant);
		foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
		{
			var source = File.ReadAllText(path);
			Assert.False(forbidden.IsMatch(source), $"Forbidden production syntax in {path}");
		}
	}

	[Fact]
	public void CompiledProductionAssemblyHasNoManagedRuntimeFeatures()
	{
		var assembly = typeof(MuiMasterPrivateRoot).Assembly;
		Assert.DoesNotContain(assembly.GetTypes().SelectMany(type =>
			type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.Static)), field =>
			!field.IsLiteral && !field.FieldType.IsValueType);

		foreach (var method in assembly.GetTypes().SelectMany(type =>
			type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)))
		{
			var body = method.GetMethodBody();
			if (body is null) continue;
			Assert.Empty(body.ExceptionHandlingClauses);
			Inspect(method, body.GetILAsByteArray()!);
		}
	}

	private static void Inspect(MethodInfo owner, byte[] bytes)
	{
		var index = 0;
		while (index < bytes.Length)
		{
			ushort value = bytes[index++];
			if (value == 0xFE) value = (ushort)(0xFE00 | bytes[index++]);
			var opcode = Opcodes[value];
			Assert.DoesNotContain(opcode, new[]
			{
				OpCodes.Newobj, OpCodes.Newarr, OpCodes.Box, OpCodes.Throw,
				OpCodes.Rethrow, OpCodes.Ldtoken,
			});
			var operandStart = index;
			index += OperandSize(opcode.OperandType, bytes, index);
			if (opcode.OperandType == OperandType.InlineMethod)
			{
				var token = BitConverter.ToInt32(bytes, operandStart);
				var target = owner.Module.ResolveMethod(token,
					owner.DeclaringType?.GetGenericArguments(),
					owner.GetGenericArguments());
				var assemblyName = target?.DeclaringType?.Assembly.GetName().Name;
				Assert.False(assemblyName?.StartsWith("System", StringComparison.Ordinal) == true,
					$"Framework call {target} from {owner}");
			}
		}
	}

	private static int OperandSize(OperandType type, byte[] bytes, int index) => type switch
	{
		OperandType.InlineNone => 0,
		OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or
			OperandType.ShortInlineVar => 1,
		OperandType.InlineVar => 2,
		OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or
			OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or
			OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
		OperandType.InlineI8 or OperandType.InlineR => 8,
		OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, index) * 4,
		_ => throw new InvalidOperationException($"Unknown operand type {type}"),
	};
}
