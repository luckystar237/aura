using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSScriptLibrary;

namespace Aura.Channel.Scripting.Loaders
{
	public class CSharpLoader : CompilingLoader
	{
		public override IEnumerable<string> HandledExtensions
		{
			get { return new[] {"cs"}; }
		}

		protected override System.Reflection.Assembly Compile(string inPath, string outPath)
		{
			try
			{
				var src = this.PreCompile(File.ReadAllText(inPath));

				var asm = CSScript.LoadCode(src, outPath, IsDebug);

				return asm;
			}
			catch (csscript.CompilerException ex)
			{
				var errors = ex.Data["Errors"] as System.CodeDom.Compiler.CompilerErrorCollection;
				var newExs = new CompilerErrorsException();

				foreach (System.CodeDom.Compiler.CompilerError err in errors)
				{
					// Line-1 to compensate lines added by the pre-compiler.
					var newEx = new LinedCompilerError(inPath, err.Line - 1, err.Column, err.ErrorText, err.IsWarning);
					newExs.Errors.Add(newEx);
				}

				throw newExs;
			}
		}
		public string PreCompile(string script)
		{
			// Default usings and compiler options
			var add = new StringBuilder();

			add.Append("//css_co");

			// Mono needs these to not treat harmless warnings as errors
			// (like a missing await in an async Task) and to not spam
			// us with warnings.
			if (Type.GetType ("Mono.Runtime") != null)
				add.Append(" /warnaserror- /warn:0;");

			add.AppendLine();

			add.Append("using System;");
			add.Append("using System.Collections.Generic;");
			add.Append("using System.Collections;");
			add.Append("using System.Linq;");
			add.Append("using System.Text;");
			add.Append("using System.Threading.Tasks;");
			add.Append("using System.Timers;");
			add.Append("using Microsoft.CSharp;");
			add.Append("using Aura.Channel.Network;");
			add.Append("using Aura.Channel.Network.Sending;");
			add.Append("using Aura.Channel.Scripting.Scripts;");
			add.Append("using Aura.Channel.Scripting;");
			add.Append("using Aura.Channel.Util;");
			add.Append("using Aura.Channel.World.Entities;");
			add.Append("using Aura.Channel.World;");
			add.Append("using Aura.Channel;");
			add.Append("using Aura.Data;");
			add.Append("using Aura.Data.Database;");
			add.Append("using Aura.Shared.Mabi.Const;");
			add.Append("using Aura.Shared.Mabi;");
			add.Append("using Aura.Shared.Network;");
			add.Append("using Aura.Shared.Util;");
			add.Append("using Aura.Shared.Util.Commands;");
			script = add + script;

			// Return();
			// --> yield break;
			// Stops Enumerator and the conversation.
			script = Regex.Replace(script,
				@"([\{\}:;\t ])?Return\s*\(\s*\)\s*;",
				"$1yield break;",
				RegexOptions.Compiled);

			// Do|Call(<method_call>);
			// --> foreach(var __callResult in <method_call>) yield return __callResult;
			// Loops through Enumerator returned by the method called and passes
			// the results to the main Enumerator.
			script = Regex.Replace(script,
				@"([\{\}:;\t ])?(Call|Do)\s*\(([^;]*)\)\s*;",
				"$1foreach(var __callResult in $3) yield return __callResult;",
				RegexOptions.Compiled);

			// duplicate <new_class> : <old_class> { <content_of_load> }
			// --> public class <new_class> : <old_class> { public override void OnLoad() { base.OnLoad(); <content_of_load> } }
			// Makes a new class, based on another one, calls the inherited
			// load first, and the new load afterwards.
			script = Regex.Replace(script,
			   @"duplicate +([^\s:]+) *: *([^\s{]+) *{ *([^}]+) *}",
			   "public class $1 : $2 { public override void Load() { base.Load(); $3 } }",
			   RegexOptions.Compiled);

			return script;
		}
	}
}
