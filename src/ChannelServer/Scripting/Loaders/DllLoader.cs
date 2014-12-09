using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aura.Shared.Util;

namespace Aura.Channel.Scripting.Loaders
{
	public class DllLoader : Loader
	{
		public override IEnumerable<string> HandledExtensions
		{
			get { return new[] {"dll"}; }
		}

		public override Assembly Load(string path)
		{
			return Assembly.LoadFrom(path);
		}
	}
}
