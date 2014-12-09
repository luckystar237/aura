using System;
using System.IO;
using System.Reflection;

namespace Aura.Channel.Scripting.Loaders
{
	/// <summary>
	/// Common base for any loaders that need to compile things
	/// </summary>
	public abstract class CompilingLoader : Loader
	{
		protected bool IsDebug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}


		public override Assembly Load(string path)
		{
			var cache = GetCachePath(path);

			if (File.Exists(cache) && File.GetLastWriteTime(path) <= File.GetLastWriteTime(cache))
				return new DllLoader().Load(cache);

			try
			{
				return this.Compile(path, cache);
			}
			catch (CompilerErrorsException ex)
			{
				foreach (var e in ex.Errors)
					e.Print();

				return null;
			}
		}

		protected abstract Assembly Compile(string inPath, string outPath);

		/// <summary>
		/// Returns path for the compiled version of the script.
		/// Creates directory structure if it doesn't exist.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string GetCachePath(string path)
		{
			var result = (!path.StartsWith("cache", StringComparison.OrdinalIgnoreCase) ? Path.Combine("cache", path) : path);

			result = Path.ChangeExtension(result, "dll");

			var dir = Path.GetDirectoryName(result);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			return result;
		}
	}
}
