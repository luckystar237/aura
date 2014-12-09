// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Shared.Util;

namespace Aura.Channel.Scripting.Loaders
{
	public class CompilerErrorsException : Exception
	{
		public List<CompilerError> Errors { get; protected set; }

		public CompilerErrorsException()
		{
			this.Errors = new List<CompilerError>();
		}
	}

	public class CompilerError
	{
		public string File { get; protected set; }
		public bool IsWarning { get; protected set; }
		public string Message { get; protected set; }

		public CompilerError(string file, string message, bool isWarning)
		{
			this.Message = message;
			this.File = file;
			this.IsWarning = isWarning;
		}

		public virtual void Print()
		{
			// Error msg
			Log.WriteLine((!this.IsWarning ? LogLevel.Error : LogLevel.Warning), "In {0}", this.File);
			Log.WriteLine(LogLevel.None, "          {0}", this.Message);
		}
	}

	/// <summary>
	/// Represents a CompilerError with line information
	/// </summary>
	public class LinedCompilerError : CompilerError
	{
		public int Line { get; protected set; }
		public int Column { get; protected set; }

		public LinedCompilerError(string file, int line, int column, string message, bool isWarning)
			: base(file, message, isWarning)
		{
			this.Line = line;
			this.Column = column;
		}

		public override void Print()
		{
			var lines = System.IO.File.ReadAllLines(this.File);

			// Error msg
			Log.WriteLine((!this.IsWarning ? LogLevel.Error : LogLevel.Warning), "In {0} on line {1}, column {2}", this.File, this.Line, this.Column);
			Log.WriteLine(LogLevel.None, "          {0}", this.Message);

			// Display lines around the error
			int startLine = Math.Max(1, this.Line - 1);
			int endLine = Math.Min(lines.Length, startLine + 2);
			for (int i = startLine; i <= endLine; ++i)
			{
				// Make sure we don't get out of range.
				// (ReadAllLines "trims" the input)
				var line = (i <= lines.Length) ? lines[i - 1] : "";

				Log.WriteLine(LogLevel.None, "  {2} {0:0000}: {1}", i, line, (this.Line == i ? '*' : ' '));
			}
		}
	}
}
