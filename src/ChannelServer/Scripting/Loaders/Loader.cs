using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Scripting.Loaders
{
	public abstract class Loader
	{
		private static readonly Loader[] _loaders =
		{
			new DllLoader(),
			new CSharpLoader(),
			new BooLoader()
		};

		public abstract IEnumerable<string> HandledExtensions { get; }

		public abstract Assembly Load(string path);

		/// <summary>
		/// Gets the loader for the file specified ny the given type.
		/// 
		/// Returns null if one does not exist.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static Loader GetLoader(string path)
		{
			var ext = Path.GetExtension(path).TrimStart('.');

			return _loaders.FirstOrDefault(l =>
				l.HandledExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)));
		}

		public static Assembly TryLoadAssembly(string path)
		{
			var loader = GetLoader(path);

			return loader == null ? null : loader.Load(path);
		}
		
		public static Assembly LoadAssembly(string path)
		{
			var loader = GetLoader(path);

			if (loader == null)
				throw new InvalidOperationException("No loader could be found for the given file type.");

			return loader.Load(path);
		}
	}
}
