using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aura.Shared.Util;

namespace Aura.Channel.Scripting.Compilers
{
	public class DllLoader : Compiler
	{
		public override Assembly Compile(string path, string outPath)
		{
			try
			{
				return Assembly.LoadFrom(path);
			}
			catch (Exception ex)
			{
				var up = new CompilerErrorsException();
				up.Errors.Add(new CompilerError(path, ex.Message, false));

				throw up;
			}
		}
	}
}
