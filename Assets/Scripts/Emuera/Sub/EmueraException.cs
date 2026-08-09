using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    /// Base exception class for all Emuera-specific exceptions.
    /// Captures script position information for better error reporting.
    /// </summary>
    [Serializable]
    internal abstract class EmueraException : ApplicationException
    {
		protected EmueraException(string errormes, ScriptPosition position)
			: base(errormes)
		{
			Position = position;
		}
		protected EmueraException(string errormes)
			: base(errormes)
		{
			Position = null;
		}
		public ScriptPosition Position;
	}

	/// <summary>
	/// Error originating from the Emuera engine itself.
	/// </summary>
    [Serializable]
    internal sealed class ExeEE : EmueraException
	{
		public ExeEE(string errormes)
			: base(errormes)
		{
		}
		public ExeEE(string errormes, ScriptPosition position)
			: base(errormes, position)
		{
		}
	}

	/// <summary>
	/// Error that appears to originate from the script side
	/// </summary>
    [Serializable]
    internal class CodeEE : EmueraException
	{
		public CodeEE(string errormes, ScriptPosition position)
			: base(errormes, position)
		{
		}
		public CodeEE(string errormes)
			: base(errormes)
		{
		}
	}

	/// <summary>
	/// Error that appears to originate from the script side; related to undefined identifiers
	/// </summary>
	[Serializable]
	internal class IdentifierNotFoundCodeEE : CodeEE
	{
		public IdentifierNotFoundCodeEE(string errormes, ScriptPosition position)
			: base(errormes, position)
		{
		}
		public IdentifierNotFoundCodeEE(string errormes)
			: base(errormes)
		{
		}
	}

	/// <summary>
	/// Not-implemented Error
	/// </summary>
    [Serializable]
    internal sealed class NotImplCodeEE : CodeEE
	{
		public NotImplCodeEE(ScriptPosition position)
			: base("この機能は現バージョンでは使えません", position)
		{
		}
		public NotImplCodeEE()
			: base("この機能は現バージョンでは使えません")
		{
		}
	}

	/// <summary>
	/// Error during Save, Load
	/// </summary>
    [Serializable]
    internal sealed class FileEE : EmueraException
	{
		public FileEE(string errormes)
			: base(errormes)
		{ }
	}

	/// <summary>
	/// Position data used to show where the error occurred. This is unformatted data, so it should not be referenced for any purpose other than error display.
	/// </summary>
	internal sealed class ScriptPosition : IEquatable<ScriptPosition>, IEqualityComparer<ScriptPosition>
	{
		public ScriptPosition()
		{
			LineNo = -1;
			Filename = "";
		}
		public ScriptPosition(string srcFile, int srcLineNo)
		{
			LineNo = srcLineNo;
            if (srcFile == null)
				Filename = "";
            else
                Filename = srcFile;
		}
		public readonly int LineNo;
		public readonly string Filename;

		public override string ToString()
		{
			if(LineNo == -1)
				return base.ToString();
			return Filename + ":" + LineNo.ToString();
		}

		#region IEqualityComparer<ScriptPosition> Members

		public bool Equals(ScriptPosition x, ScriptPosition y)
		{
			if((x == null)||(y == null))
				return false;
			return ((x.Filename == y.Filename) && (x.LineNo == y.LineNo));
		}

		public int GetHashCode(ScriptPosition obj)
		{
			return Filename.GetHashCode() ^ LineNo.GetHashCode();
		}

		#endregion

		#region IEquatable<ScriptPosition> Members

		public bool Equals(ScriptPosition other)
		{
			return this.Equals(this, other);
		}

		#endregion
	}
}
