using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.Shared.Util;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;

namespace Aura.Channel.Scripting.Loaders
{
	public class BooLoader : CompilingLoader
	{
		public override IEnumerable<string> HandledExtensions
		{
			get { return new[] {"boo"}; }
		}

		protected override System.Reflection.Assembly Compile(string inPath, string outPath)
		{
			var compiler = new Boo.Lang.Compiler.BooCompiler();
			compiler.Parameters.AddAssembly(typeof(Log).Assembly);
			compiler.Parameters.AddAssembly(typeof(ScriptManager).Assembly);
			compiler.Parameters.Input.Add(new FileInput(inPath));
			compiler.Parameters.OutputAssembly = outPath;
			compiler.Parameters.Pipeline = new CompileToFile();

			compiler.Parameters.Debug = this.IsDebug;

			var context = compiler.Run();
			if (context.GeneratedAssembly == null)
			{
				var errors = context.Errors;
				var newExs = new CompilerErrorsException();

				foreach (var err in errors)
				{
					var newEx = new LinedCompilerError(inPath, err.LexicalInfo.Line, err.LexicalInfo.Column, err.Message, false);
					newExs.Errors.Add(newEx);
				}

				throw newExs;
			}

			return context.GeneratedAssembly;
		}
	}
}
