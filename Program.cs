﻿using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;

namespace CelesteUncapped {
	internal class Program {
		public static void Main(string[] args) {
			if (args.Length < 1) {
				System.Console.WriteLine("Missing argument!\nUsage: CelesteUncapped.exe <Celeste.exe path>");
				return;
			}

			using (var game = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters {ReadWrite = true})) {
				// get main game entry point
				var body = game.MainModule.Types.Single(t => t.Name == "Celeste")
					.Methods.Single(m => m.IsConstructor && !m.IsStatic).Body;

				// find set_IsFixedTimeStep call
				var b = body.Instructions
					.Single(i =>
						i.OpCode == OpCodes.Call && ((MethodReference) i.Operand).Name == "set_IsFixedTimeStep")
					.Previous;

				if (b.OpCode != OpCodes.Ldc_I4_1) {
					System.Console.WriteLine($"Error! Expected ldc.i4.1, got {b.OpCode}");
					System.Console.WriteLine("Not patching game: Are you sure the game isn't already patched?");
					return;
				}

				// update to set to false instead of true
				var il = body.GetILProcessor();
				il.Replace(b, il.Create(OpCodes.Ldc_I4_0));

				// save the patched game
				var oldPath = args[0] + ".old";
				if (File.Exists(oldPath)) {
					File.Delete(oldPath);
				}

				File.Copy(args[0], oldPath);
				game.Write();
				System.Console.WriteLine("Game patched! Old executable moved to " + oldPath);
			}
		}
	}
}