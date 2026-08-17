using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    /// Lexical analyzer for ERA script language.
    /// Tokenizes ERA script source code into words (identifiers, operators, literals, etc.)
    /// for parsing and execution by the game engine.
    /// </summary>
    enum LexEndWith
    {
		//Forced termination at EoL in any case
		None = 0,
		EoL,//Always analyze to the very end
		Operator,//Ends when an operator is found. Left side of an assignment expression
		Question,//Ends at the ternary operator ?. \@~~?~~#~~\@
		Percent,//Ends at %. %~~%
		RightCurlyBrace,//Ends at }. {~~}
		Comma,//Ends at ,. TIMES first argument
		//Single,//Ends at a single Identifier//1807 Single removed
		GreaterThan,//Ends at '>'. HTML tag parsing
	}

	enum FormStrEndWith
	{
		//Forced termination at EoL in any case
		None = 0,
		EoL,//Always analyze to the very end
		DoubleQuotation,//Ends at ". @"~~"
		Sharp,//Ends at #. The first part of \@~~?~~#~~\@
		YenAt,//Ends at \@. The second part of \@~~?~~#~~\@
		Comma,//Ends at ,. ANY_FORM argument
		LeftParenthesis_Bracket_Comma_Semicolon,//Ends at [ or ( or , or ;. Function name part of the CALLFORM family.
	}

	enum StrEndWith
	{
		//Forced termination at EoL in any case
		None = 0,
		EoL,//Always analyze to the very end
		SingleQuotation,//Ends at '. '~~'
		DoubleQuotation,//Ends at ". "~~"
		Comma,//Ends at ,. PRINTV'~~,
		LeftParenthesis_Bracket_Comma_Semicolon,//Ends at [ or ( or , or ;. Function name part.
	}

	enum LexAnalyzeFlag
	{
		None = 0,
		AnalyzePrintV = 1,//In PRINTV's argument, text following ' is displayed as a string even though it is not an expression
		AllowAssignment = 2,//Flag indicating scenes where an assignment operator can be used. A = appearing mid-way without this flag causes an Error
		AllowSingleQuotationStr = 4,//For HTML_PRINT parsing. Allows strings enclosed in ''.
	}

	/// <summary>
	/// Renamed from TokenReader in 1756
	/// Lexical name notwithstanding, includes syntax analysis
	/// </summary>
	internal static class LexicalAnalyzer
	{

		const int MAX_EXPAND_MACRO = 100;
		//readonly static IList<char> operators = new char[] { '+', '-', '*', '/', '%', '=', '!', '<', '>', '|', '&', '^', '~', '?', '#' };
		//readonly static IList<char> whiteSpaces = new char[] { ' ', '　', '\t' };
		//readonly static IList<char> endOfExpression = new char[] { ')', '}', ']', ',', ':' };
		//readonly static IList<char> startOfExpression = new char[] { '(' };
		//readonly static IList<char> stringToken = new char[] { '\"', };
		//readonly static IList<char> stringFormToken = new char[] { '@', };
		//readonly static IList<char> etcSymbol = new char[] { '[', '{', '$', '\\', };
		//readonly static IList<char> decimalDigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', };
		readonly static IList<char> hexadecimalDigits = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };

	//1819 Using regular expressions is a bit slow. I would like to support double eventually. I will think about it
		//readonly static Regex DigitsReg = new Regex("" +
		//	"(" +
		//	"((?<simple>[-]?[0-9]+)([^.xXbBeEpP]|$))" +
		//	"|(" +
		//	"(0(x|X)(?<hex>[0-9a-fA-F]+))|"+
		//	"(0(b|B)(?<bin>[01]+))|"+
		//	"(" + //base10
		//	"(?<integer>[-]?[0-9]*(?<double>[.][0-9])?)" +
		//	"(((p|P)(?<exp2>[0-9]+))|" +
		//	"((e|E)(?<exp10>[0-9]+)))?" +
		//	")"+
		//	"))"
		//	, RegexOptions.Compiled);
		//readonly static Regex idReg = new Regex(@"[^][ \t+*/%=!<>|&^~?#(){},:$\\'""@.;　-]+", RegexOptions.Compiled);
		//public static Int64 ReadInt64(StringStream st, bool retZero)
		//{
		//	Match m = DigitsReg.Match(st.RowString, st.CurrentPosition);
		//	string numstr = m.Groups["simple"].Value;
		//	if (numstr.Length > 0)
		//	{
		//		st.Jump(numstr.Length);
		//		return Convert.ToInt64(numstr, 10);
		//	}
		//	st.Jump(m.Length);
		//	if (m.Groups["bin"].Length > 0)
		//		return Convert.ToInt64(m.Groups["bin"].Value, 2);
		//	if(m.Groups["hex"].Length > 0)
		//		return Convert.ToInt64(m.Groups["hex"].Value, 16);
		//	numstr = m.Groups["number"].Value;
		//	if (numstr.Length > 0)
		//	{
		//		int exp = 0;
		//		string exp2 = m.Groups["exp2"].Value;
		//		string exp10 = m.Groups["exp10"].Value;
		//		if(m.Groups["double"].Length == 0 && exp2.Length == 0 && exp10.Length == 0)
		//		{
		//			return Convert.ToInt64(numstr,10);
		//		}
		//		double d = Convert.ToDouble(numstr);
		//		if (exp2.Length > 0)
		//		{
		//			exp = Convert.ToInt32(exp2, 10);
		//			d = d * Math.Pow(2, exp);
		//		}
		//		else if (exp10.Length > 0)
		//		{
		//			exp = Convert.ToInt32(exp10, 10);
		//			d = d * Math.Pow(10, exp);
		//		}
		//		return ((Int64)(d + 0.49));
		//	}
		//	throw new CodeEE("A token starting with a digit is invalid.");
		//}



		public static bool UseMacro = true;
		#region read
		public static Int64 ReadInt64(StringStream st, bool retZero)
		{
			Int64 significand;
			int expBase = 0;
			int exponent = 0;
			int stStartPos = st.CurrentPosition;
			int stEndPos;
			int fromBase = 10;
			if (st.Current == '0')
			{
				char c = st.Next;
				if ((c == 'x') || (c == 'X'))
				{
					fromBase = 16;
					st.ShiftNext();
					st.ShiftNext();
				}
				else if ((c == 'b') || (c == 'B'))
				{
					fromBase = 2;
					st.ShiftNext();
					st.ShiftNext();
				}
				//Octal is not adopted for compatibility reasons.
				//else if (dchar.IsDigit(c))
				//{
				//    fromBase = 8;
				//    st.ShiftNext();
				//}
			}
			if (retZero && st.Current != '+' && st.Current != '-' && !char.IsDigit(st.Current))
			{
				if (fromBase != 16)
					return 0;
				else if (!hexadecimalDigits.Contains(st.Current))
					return 0;
			}
			significand = readDigits(st, fromBase);
			if ((st.Current == 'p') || (st.Current == 'P'))
				expBase = 2;
			else if ((st.Current == 'e') || (st.Current == 'E'))
				expBase = 10;
			if (expBase != 0)
			{
				st.ShiftNext();
				unchecked { exponent = (int)readDigits(st, fromBase); }
			}
			stEndPos = st.CurrentPosition;
			if ((expBase != 0) && (exponent != 0))
			{

				double d = significand * Math.Pow(expBase, exponent);
				if ((double.IsNaN(d)) || (double.IsInfinity(d)) || (d > Int64.MaxValue) || (d < Int64.MinValue))
					throw new CodeEE("\"" + st.Substring(stStartPos, stEndPos) + GameMessages.T("\" exceeds the range of a 64-bit signed integer."));
				significand = (Int64)d;
			}
			return significand;
		}
		//static Regex reg = new Regex(@"[0-9A-Fa-f]+", RegexOptions.Compiled);
		private static Int64 readDigits(StringStream st, int fromBase)
		{
			int start = st.CurrentPosition;
			//1756 Tried regular expressions but there was almost no difference, so dropped
			//Match m = reg.Match(st.RowString, st.CurrentPosition);
			//st.Jump(m.Length);
			//return m.Value;
			char c = st.Current;
			if ((c == '-') || (c == '+'))
			{
				st.ShiftNext();
			}
			if (fromBase == 10)
			{
				while (!st.EOS)
				{
					c = st.Current;
					if (char.IsDigit(c))
					{
						st.ShiftNext();
						continue;
					}
					break;
				}
			}
			else if (fromBase == 16)
			{
				while (!st.EOS)
				{
					c = st.Current;
					if (char.IsDigit(c) || hexadecimalDigits.Contains(c))
					{
						st.ShiftNext();
						continue;
					}
					break;
				}
			}
			else if (fromBase == 2)
			{
				while (!st.EOS)
				{
					c = st.Current;
					if (char.IsDigit(c))
					{
						if ((c != '0') && (c != '1'))
							throw new CodeEE(GameMessages.T("A character that cannot be used in binary notation was used."));
						st.ShiftNext();
						continue;
					}
					break;
				}
			}
			string strInt = st.Substring(start, st.CurrentPosition - start);
			try
			{
				return Convert.ToInt64(strInt, fromBase);
			}
			catch (FormatException)
			{
				throw new CodeEE("\"" + strInt + GameMessages.T("\" cannot be converted to an integer."));
			}
			catch (OverflowException)
			{
				throw new CodeEE("\"" + strInt + GameMessages.T("\" exceeds the range of a 64-bit signed integer."));
			}
			catch (ArgumentOutOfRangeException)
			{
				if (string.IsNullOrEmpty(strInt))
					throw new CodeEE(GameMessages.T("A character recognizable as a number is required."));
				throw new CodeEE(GameMessages.T("String \"") + strInt + GameMessages.T("\" cannot be recognized as a number."));
			}
		}

		/// <summary>
		/// Used only by the TIMES second argument.
		/// Exceptions thrown by the Convert class are passed through as-is, so handle them appropriately.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static double ReadDouble(StringStream st)
		{
			int start = st.CurrentPosition;
			//Roughly read and leave the error handling to the Convert class.
			//Significand and fractional part

			if ((st.Current == '-') || (st.Current == '+'))
			{
				st.ShiftNext();
			}
			while (!st.EOS)
			{//Significand part
				char c = st.Current;
				if (char.IsDigit(c) || (c == '.'))
				{
					st.ShiftNext();
					continue;
				}
				break;
			}
			if ((st.Current == 'e') || (st.Current == 'E'))
			{
				st.ShiftNext();
				if (st.Current == '-')
				{
					st.ShiftNext();
				}
				while (!st.EOS)
				{//Exponent part
					char c = st.Current;
					if (char.IsDigit(c) || (c == '.'))
					{
						st.ShiftNext();
						continue;
					}
					break;
				}
			}
			return Convert.ToDouble(st.Substring(start, st.CurrentPosition - start));
		}

		/// <summary>
		/// Gets the word at the start of the line. With macro expansion. However, it does not expand macros that are not a single word.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static IdentifierWord ReadFirstIdentifierWord(StringStream st)
		{
			//int startpos = st.CurrentPosition;
			string str = ReadSingleIdentifier(st);
			if (string.IsNullOrEmpty(str))
				throw new CodeEE(GameMessages.T("The line starts with an invalid character."));
			//1808a3 Stop expanding the leading single word. Prohibit the replacement of commands.
			//if (UseMacro)
			//{
			//    int i = 0;
			//    while (true)
			//    {
			//        DefineMacro macro = GlobalStatic.IdentifierDictionary.GetMacro(str);
			//        i++;
			//        if (i > MAX_EXPAND_MACRO)
			//            throw new CodeEE("The number of macro expansions exceeded the limit per statement (possible self-reference or circular reference).");
			//        if (macro == null)
			//            break;
			//        //If a macro that is not a word (a single identifier) appears, it is not handled here
			//        if (macro.IDWord == null)
			//        {
			//            st.CurrentPosition = startpos;
			//            return null;//leave it to the variable processing.
			//        }
			//        str = macro.IDWord.Code;
			//    }
			//}
			return new IdentifierWord(str);
		}

		/// <summary>
		/// Gets a word. With macro expansion. Function-type macros are not expanded
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static IdentifierWord ReadSingleIdentifierWord(StringStream st)
		{
			string str = ReadSingleIdentifier(st);
			if (string.IsNullOrEmpty(str))
				return null;
			if (UseMacro)
			{
				int i = 0;
				while (true)
				{
					DefineMacro macro = GlobalStatic.IdentifierDictionary.GetMacro(str);
					i++;
					if (i > MAX_EXPAND_MACRO)
						throw new CodeEE(GameMessages.T("The number of macro expansions exceeded the limit of ") + MAX_EXPAND_MACRO.ToString() + GameMessages.T(" per statement (possible self-reference or circular reference)."));
					if (macro == null)
						break;
					if (macro.IDWord != null)
						throw new CodeEE(GameMessages.T("Macro ") + macro.Keyword + GameMessages.T(" cannot be used in this context (only macros that expand to a single word can be used)."));
					str = macro.IDWord.Code;
				}
			}
			return new IdentifierWord(str);
		}

        static readonly HashSet<char> kHashSet_ReadSingleIdentifier = new HashSet<char>
        {
            ' ',
            '\t',
            '+',
            '-',
            '*',
            '/',
            '%',
            '=',
            '!',
            '<',
            '>',
            '|',
            '&',
            '^',
            '~',
            '?',
            '#',
            ')',
            '}',
            ']',
            ',',
            ':',
            '(',
            '{',
            '[',
            '$',
            '\\',
            '\'',
            '\"',
            '@',
            '.',
            ';',
        };
        /// <summary>
        /// Gets a word as a string. No macro expansion
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static string ReadSingleIdentifier(StringStream st)
		{
			//1819 Somewhat slow. But I want to do it eventually
			//Match m = idReg.Match(st.RowString, st.CurrentPosition);
			//st.Jump(m.Length);
			//return m.Value;
			int start = st.CurrentPosition;
            char c;
			while (!st.EOS)
			{
                //switch (st.Current)
                //{
                //	case ' ':
                //	case '\t':
                //	case '+':
                //	case '-':
                //	case '*':
                //	case '/':
                //	case '%':
                //	case '=':
                //	case '!':
                //	case '<':
                //	case '>':
                //	case '|':
                //	case '&':
                //	case '^':
                //	case '~':
                //	case '?':
                //	case '#':
                //	case ')':
                //	case '}':
                //	case ']':
                //	case ',':
                //	case ':':
                //	case '(':
                //	case '{':
                //	case '[':
                //	case '$':
                //	case '\\':
                //	case '\'':
                //	case '\"':
                //	case '@':
                //	case '.':
                //	case ';'://Comments are handled by the SkipWhiteSpace etc. that follows right after.
                //		goto end;
                //	case '　':
                //		if (!Config.SystemAllowFullSpace)
                //			throw new CodeEE("Unexpected full-width space found (this warning can be ignored with the system option " + Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + ")");
                //		goto end;
                //}

                c = st.Current;
                if(kHashSet_ReadSingleIdentifier.Contains(c))
                    goto end;
                else if(c == '　')
                {
                    if(!Config.SystemAllowFullSpace)
                	    throw new CodeEE(GameMessages.T("Unexpected full-width space found (this warning can be ignored via the system option \"") + Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + GameMessages.T("\")."));
                    goto end;
                }
                st.ShiftNext();
			}
		end:
			return st.Substring(start, st.CurrentPosition - start);
		}

		/// <summary>
		/// Reads until endWith is found. Checking the start point and end terminator is the caller's responsibility.
		/// Escapes supported.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static string ReadString(StringStream st, StrEndWith endWith)
		{
			StringBuilder buffer = new StringBuilder(100);
			while (true)
			{
				switch (st.Current)
				{
					case '\0':
						goto end;
					case '\"':
						if (endWith == StrEndWith.DoubleQuotation)
							goto end;
						break;
					case '\'':
						if (endWith == StrEndWith.SingleQuotation)
							goto end;
						break;
					case ',':
						if ((endWith == StrEndWith.Comma) || (endWith == StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon))
							goto end;
						break;
					case '(':
					case '[':
					case ';':
						if (endWith == StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
							goto end;
						break;
					case '\\'://Escape handling
						st.ShiftNext();//Skip the \
						switch (st.Current)
						{
							case StringStream.EndOfString:
								throw new CodeEE(GameMessages.T("No character follows the escape character \\."));
							case '\n': break;
							case 's': buffer.Append(' '); break;
							case 'S': buffer.Append('　'); break;
							case 't': buffer.Append('\t'); break;
							case 'n': buffer.Append('\n'); break;
							default: buffer.Append(st.Current); break;
						}
						st.ShiftNext();//Skip the character after the \
						continue;
				}
				buffer.Append(st.Current);
				st.ShiftNext();
			}
		end:
			return buffer.ToString();
		}

		/// <summary>
		/// Throws CodeEE on failure. Does not rely on OperatorManager
		/// May return OperatorCode.Assignment.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static OperatorCode ReadOperator(StringStream st, bool allowAssignment)
		{
			char cur = st.Current;
			st.ShiftNext();
			char next = st.Current;
			switch (cur)
			{
				case '+':
					if (next == '+')
					{
						st.ShiftNext();
						return OperatorCode.Increment;
					}
					return OperatorCode.Plus;
				case '-':
					if (next == '-')
					{
						st.ShiftNext();
						return OperatorCode.Decrement;
					}
					return OperatorCode.Minus;
				case '*':
					return OperatorCode.Mult;
				case '/':
					return OperatorCode.Div;
				case '%':
					return OperatorCode.Mod;
				case '=':
					if (next == '=')
					{
						st.ShiftNext();
						return OperatorCode.Equal;
					}
					if (allowAssignment)
						return OperatorCode.Assignment;
					throw new CodeEE(GameMessages.T("Unexpected assignment operator '=' found (use '==' for equality comparison)."));
				case '!':
					if (next == '=')
					{
						st.ShiftNext();
						return OperatorCode.NotEqual;
					}
					else if (next == '&')
					{
						st.ShiftNext();
						return OperatorCode.Nand;
					}
					else if (next == '|')
					{
						st.ShiftNext();
						return OperatorCode.Nor;
					}
					return OperatorCode.Not;
				case '<':
					if (next == '=')
					{
						st.ShiftNext();
						return OperatorCode.LessEqual;
					}
					else if (next == '<')
					{
						st.ShiftNext();
						return OperatorCode.LeftShift;
					}
					return OperatorCode.Less;
				case '>':
					if (next == '=')
					{
						st.ShiftNext();
						return OperatorCode.GreaterEqual;
					}
					else if (next == '>')
					{
						st.ShiftNext();
						return OperatorCode.RightShift;
					}
					return OperatorCode.Greater;
				case '|':
					if (next == '|')
					{
						st.ShiftNext();
						return OperatorCode.Or;
					}
					return OperatorCode.BitOr;
				case '&':
					if (next == '&')
					{
						st.ShiftNext();
						return OperatorCode.And;
					}
					return OperatorCode.BitAnd;
				case '^':
					if (next == '^')
					{
						st.ShiftNext();
						return OperatorCode.Xor;
					}
					return OperatorCode.BitXor;
				case '~':
					return OperatorCode.BitNot;
				case '?':
					return OperatorCode.Ternary_a;
				case '#':
					return OperatorCode.Ternary_b;

			}
			throw new CodeEE("'" + cur + GameMessages.T("' cannot be recognized as an operator."));
		}

		/// <summary>
		/// Throws CodeEE on failure. Does not rely on OperatorManager
		/// "=" returns OperatorCode.Assignment, and "==" returns Equal
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static OperatorCode ReadAssignmentOperator(StringStream st)
		{
			OperatorCode ret = OperatorCode.NULL;
			char cur = st.Current;
			st.ShiftNext();
			char next = st.Current;
			switch (cur)
			{
				case '+':
					if (next == '+')
						ret = OperatorCode.Increment;
					else if (next == '=')
						ret = OperatorCode.Plus;
					break;
				case '-':
					if (next == '-')
						ret = OperatorCode.Decrement;
					else if (next == '=')
						ret = OperatorCode.Minus;
					break;
				case '*':
					if (next == '=')
						ret = OperatorCode.Mult;
					break;
				case '/':
					if (next == '=')
						ret = OperatorCode.Div;
					break;
				case '%':
					if (next == '=')
						ret = OperatorCode.Mod;
					break;
				case '=':
					if (next == '=')
					{
						ret = OperatorCode.Equal;
						break;
					}
					return OperatorCode.Assignment;
				case '\'':
					if (next == '=')
					{
						ret = OperatorCode.AssignmentStr;
						break;
					}
					throw new CodeEE(GameMessages.T("\"\'\" cannot be recognized as an assignment operator."));
				case '<':
					if (next == '<')
					{
						st.ShiftNext();
						if (st.Current == '=')
						{
							ret = OperatorCode.LeftShift;
							break;
						}
						throw new CodeEE(GameMessages.T("'<' cannot be recognized as an assignment operator."));
					}
					break;
				case '>':
					if (next == '>')
					{
						st.ShiftNext();
						if (st.Current == '=')
						{
							ret = OperatorCode.RightShift;
							break;
						}
						throw new CodeEE(GameMessages.T("'>' cannot be recognized as an assignment operator."));
					}
					break;
				case '|':
					if (next == '=')
						ret = OperatorCode.BitOr;
					break;
				case '&':
					if (next == '=')
						ret = OperatorCode.BitAnd;
					break;
				case '^':
					if (next == '=')
						ret = OperatorCode.BitXor;
					break;
			}
			if (ret == OperatorCode.NULL)
				throw new CodeEE("'" + cur + GameMessages.T("' cannot be recognized as an assignment operator."));
			st.ShiftNext();
			return ret;
		}



		/// <summary>
		/// For displaying characters on the Console. Must not be used for lexical or syntax analysis
		/// </summary>
		public static int SkipAllSpace(StringStream st)
		{
			int count = 0;
			while (true)
			{
				switch (st.Current)
				{
					case ' ':
					case '\t':
					case '　':
						count++;
						st.ShiftNext();
						continue;
				}
				return count;
			}
		}

		public static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '　';
		}

		/// <summary>
		/// For lexical and syntax analysis. Skips comments as well as whitespace.
		/// </summary>
		public static int SkipWhiteSpace(StringStream st)
		{
			int count = 0;
			while (true)
			{
				switch (st.Current)
				{
					case ' ':
					case '\t':
						count++;
						st.ShiftNext();
						continue;
					case '　':
						if (!Config.SystemAllowFullSpace)
							return count;
						goto case ' ';
					case ';':
						if (st.CurrentEqualTo(";#;") && Program.DebugMode)
						{
							st.Jump(3);
							continue;
						}
						else if (st.CurrentEqualTo(";!;"))
						{
							st.Jump(3);
							continue;
						}
						st.Seek(0, System.IO.SeekOrigin.End);
						return count;
				}
				return count;
			}
		}

		/// <summary>
		/// For lexical and syntax analysis. Skips half-width spaces immediately before a string. By nature, only looks at half-width spaces.
		/// </summary>
		public static int SkipHalfSpace(StringStream st)
		{
			int count = 0;
			while (st.Current == ' ')
			{
				count++;
				st.ShiftNext();
			}
			return count;
		}
		#endregion

		#region analyse
		
		/// <summary>
		/// Only function declarations and expressions can be analyzed. Do not send FORM strings or ordinary strings.
		/// On return, the character of endWith should be Current. Verification of proper termination is done by the caller.
		/// </summary>
		/// <returns></returns>
		public static WordCollection Analyse(StringStream st, LexEndWith endWith, LexAnalyzeFlag flag)
		{
			WordCollection ret = new WordCollection();
			int nestBracketS = 0;
			//int nestBracketM = 0;
			int nestBracketL = 0;
			while (true)
			{
				switch (st.Current)
				{
					case '\n':
					case '\0':
						goto end;
					case ' ':
					case '\t':
						st.ShiftNext();
						continue;
					case '　':
						if (!Config.SystemAllowFullSpace)
							throw new CodeEE(GameMessages.T("Unexpected full-width space found during lexical analysis (this warning can be ignored via the system option \"") + Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + GameMessages.T("\")."));
						st.ShiftNext();
						continue;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						ret.Add(new LiteralIntegerWord(ReadInt64(st, false)));
						break;
					case '>':
						if(endWith == LexEndWith.GreaterThan)
							goto end;
						goto case '+';
					case '+':
					case '-':
					case '*':
					case '/':
					case '%':
					case '=':
					case '!':
					case '<':
					case '|':
					case '&':
					case '^':
					case '~':
					case '?':
					case '#':
						if ((nestBracketS == 0) && (nestBracketL == 0))
						{
							if (endWith == LexEndWith.Operator)
								goto end;//It should be an assignment operator. The caller should check
							else if ((endWith == LexEndWith.Percent) && (st.Current == '%'))
								goto end;
							else if ((endWith == LexEndWith.Question) && (st.Current == '?'))
								goto end;
						}
						ret.Add(new OperatorWord(ReadOperator(st, (flag & LexAnalyzeFlag.AllowAssignment) == LexAnalyzeFlag.AllowAssignment)));
						break;
					case ')': ret.Add(new SymbolWord(')')); nestBracketS--; st.ShiftNext(); continue;
					case ']': ret.Add(new SymbolWord(']')); nestBracketL--; st.ShiftNext(); continue;
					case '(': ret.Add(new SymbolWord('(')); nestBracketS++; st.ShiftNext(); continue;
					case '[':
						if (st.Next == '[')
						{
							//throw new CodeEE("Unexpected character '[[' found during lexical analysis.");
							////1808alpha006 rename handling change
							//1808beta009 Restore only this one
							//Because with the current handling, by the time we reach here the rename has already failed, but to restore the warning content
							if (ParserMediator.RenameDic == null)
								throw new CodeEE(GameMessages.T("Unexpected character \"[[\" found during lexical analysis."));
							int start = st.CurrentPosition;
							int find = st.Find("]]");
							if (find <= 2)
							{
								if (find == 2)
									throw new CodeEE(GameMessages.T("An empty [[]] was found."));
								else
									throw new CodeEE(GameMessages.T("A \"[[\" without a matching \"]]\" was found."));
							}
							string key = st.Substring(start, find + 2);
							//1810 Anything that could not be replaced up to this point is forced to Error
							//Because even ones that were unreplaceable before line concatenation but became replaceable by it had been replaced
							throw new CodeEE(GameMessages.T("A token that cannot be replaced (rename) was found during lexical analysis: ") + key + GameMessages.T("."));
							//string value = null;
							//if (!ParserMediator.RenameDic.TryGetValue(key, out value))
							//    throw new CodeEE("A token that cannot be replaced (rename) was found during lexical analysis: " + key + ".");
							//st.Replace(start, find + 2, value);
							//continue;//Restart analysis from that spot
						}
						ret.Add(new SymbolWord('[')); nestBracketL++; st.ShiftNext(); continue;
					case ':': ret.Add(new SymbolWord(':')); st.ShiftNext(); continue;
					case ',':
						if ((endWith == LexEndWith.Comma) && (nestBracketS == 0))// && (nestBracketL == 0))
							goto end;
						ret.Add(new SymbolWord(',')); st.ShiftNext(); continue;
					//case '}': ret.Add(new SymbolWT('}')); nestBracketM--; continue;
					//case '{': ret.Add(new SymbolWT('{')); nestBracketM++; continue;
					case '\'':
						if ((flag & LexAnalyzeFlag.AllowSingleQuotationStr) == LexAnalyzeFlag.AllowSingleQuotationStr)
						{
							st.ShiftNext();
							ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.SingleQuotation)));
							if (st.Current != '\'')
								throw new CodeEE(GameMessages.T("\' is not closed."));
							st.ShiftNext();
							break;
						}
						if ((flag & LexAnalyzeFlag.AnalyzePrintV) != LexAnalyzeFlag.AnalyzePrintV)
						{
							//AssignmentStr special handling. Only when '=' is found while searching for the assignment operator in an assignment statement
							if ((endWith == LexEndWith.Operator) && (nestBracketS == 0) && (nestBracketL == 0) && st.Next == '=' )
								goto end;
							throw new CodeEE(GameMessages.T("Unexpected character '") + st.Current + GameMessages.T("' found during lexical analysis."));
						}
						st.ShiftNext();
						ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.Comma)));
						if (st.Current == ',')
							goto case ',';//If there is more, go to the , handling. Otherwise it should be the end of the line
						goto end;
					case '}':
						if (endWith == LexEndWith.RightCurlyBrace)
							goto end;
						throw new CodeEE(GameMessages.T("Unexpected character '") + st.Current + GameMessages.T("' found during lexical analysis."));
					case '\"':
						st.ShiftNext();
						ret.Add(new LiteralStringWord(ReadString(st, StrEndWith.DoubleQuotation)));
						if (st.Current != '\"')
							throw new CodeEE(GameMessages.T("\" is not closed."));
						st.ShiftNext();
						break;
					case '@':
						if (st.Next != '\"')
						{
							ret.Add(new SymbolWord('@'));
							st.ShiftNext();
							continue;
						}
						st.ShiftNext();
						st.ShiftNext();
						ret.Add(AnalyseFormattedString(st, FormStrEndWith.DoubleQuotation, false));
						if (st.Current != '\"')
							throw new CodeEE(GameMessages.T("\" is not closed."));
						st.ShiftNext();
						break;
					case '.':
						ret.Add(new SymbolWord('.'));
						st.ShiftNext();
						continue;

					case '\\':
						if (st.Next != '@')
							throw new CodeEE(GameMessages.T("Unexpected character '") + st.Current + GameMessages.T("' found during lexical analysis."));
						{
							st.Jump(2);
							ret.Add(new StrFormWord(new string[] { "", "" }, new SubWord[] { AnalyseYenAt(st) }));
						}
						break;
					case '{':
					case '$':
						throw new CodeEE(GameMessages.T("Unexpected character '") + st.Current + GameMessages.T("' found during lexical analysis."));
					case ';'://1807 Semicolon comment sections in a line
						if (st.CurrentEqualTo(";#;") && Program.DebugMode)
						{
							st.Jump(3);
							break;
						}
						else if (st.CurrentEqualTo(";!;"))
						{
							st.Jump(3);
							break;
						}
						st.Seek(0, System.IO.SeekOrigin.End);
						goto end;
					default:
						{
							ret.Add(new IdentifierWord(ReadSingleIdentifier(st)));
							break;
						}
				}
			}
		end:
			if ((nestBracketS != 0) || (nestBracketL != 0))
			{
				if (nestBracketS < 0)
					throw new CodeEE(GameMessages.T("A ')' without a matching '(' was found during lexical analysis."));
				else if (nestBracketS > 0)
					throw new CodeEE(GameMessages.T("A '(' without a matching ')' was found during lexical analysis."));
				if (nestBracketL < 0)
					throw new CodeEE(GameMessages.T("A ']' without a matching '[' was found during lexical analysis."));
				else if (nestBracketL > 0)
					throw new CodeEE(GameMessages.T("A '[' without a matching ']' was found during lexical analysis."));
			}
			if (UseMacro)
				return expandMacro(ret);
			return ret;

	}

	/// <summary>
	/// Public method to expand macros in an existing WordCollection.
	/// Used for re-expanding macros in DIM lines that were created before UseMacro was enabled.
	/// </summary>
	/// <param name="wc">WordCollection to expand macros in</param>
	/// <returns>WordCollection with macros expanded</returns>
	public static WordCollection ExpandMacroPublic(WordCollection wc)
	{
		if (!UseMacro)
			return wc;
		return expandMacro(wc);
	}

	private static WordCollection expandMacro(WordCollection wc)
	{
			//Macro expansion
			wc.Pointer = 0;
			int count = 0;
			while (!wc.EOL)
			{
				IdentifierWord word = wc.Current as IdentifierWord;
				if (word == null)
				{
					wc.ShiftNext();
					continue;
				}
				string idStr = word.Code;
				DefineMacro macro = GlobalStatic.IdentifierDictionary.GetMacro(idStr);
				if (macro == null)
				{
					wc.ShiftNext();
					continue;
				}
				count++;
				if (count > MAX_EXPAND_MACRO)
					throw new CodeEE(GameMessages.T("The number of macro expansions exceeded the limit of ") + MAX_EXPAND_MACRO.ToString() + GameMessages.T(" per statement (possible self-reference or circular reference)."));
				if (!macro.HasArguments)
				{
					wc.Remove();
					wc.InsertRange(macro.Statement);
					continue;
				}
				//Function-type macro
				wc = expandFunctionlikeMacro(macro, wc);
			}
			wc.Pointer = 0;
			return wc;
		}

		private static WordCollection expandFunctionlikeMacro(DefineMacro macro, WordCollection wc)
		{
			int macroStart = wc.Pointer;
			wc.ShiftNext();
			SymbolWord symbol = wc.Current as SymbolWord;
			if (symbol == null || symbol.Type != '(')
				throw new CodeEE(GameMessages.T("Function-type macro ") + macro.Keyword + GameMessages.T(" has no argument."));
			WordCollection macroWC = macro.Statement.Clone();
			WordCollection[] args = new WordCollection[macro.ArgCount];
			//argument part reading loop
			for (int i = 0; i < macro.ArgCount; i++)
			{
				int macroNestBracketS = 0;
				args[i] = new WordCollection();
				while (true)
				{
					wc.ShiftNext();
					if (wc.EOL)
						throw new CodeEE(GameMessages.T("Function-type macro ") + macro.Keyword + GameMessages.T(" is used incorrectly."));
					symbol = wc.Current as SymbolWord;
					if (symbol == null)
					{
						args[i].Add(wc.Current);
						continue;
					}
					switch (symbol.Type)
					{
						case '(': macroNestBracketS++; break;
						case ')':
							if (macroNestBracketS > 0)
							{
								macroNestBracketS--;
								break;
							}
							if (i != macro.ArgCount - 1)
								throw new CodeEE(GameMessages.T("Function-type macro ") + macro.Keyword + GameMessages.T(" has an incorrect number of arguments."));
							goto exitfor;
						case ',':
							if (macroNestBracketS == 0)
								goto exitwhile;
							break;
					}
					args[i].Add(wc.Current);
				}
			exitwhile:
				if (args[i].Collection.Count == 0)
					throw new CodeEE(GameMessages.T("Function-type macro ") + macro.Keyword + GameMessages.T(" cannot omit arguments."));
				continue;
			}
		//argument part reading loop end
		exitfor:
			symbol = wc.Current as SymbolWord;
			if (symbol == null || symbol.Type != ')')
				throw new CodeEE(GameMessages.T("Function-type macro ") + macro.Keyword + GameMessages.T(" is used incorrectly."));
			int macroLength = wc.Pointer - macroStart + 1;
			wc.Pointer = macroStart;
			for (int j = 0; j < macroLength; j++)
				wc.Collection.RemoveAt(macroStart);
			while (!macroWC.EOL)
			{
				MacroWord w = macroWC.Current as MacroWord;
				if (w == null)
				{
					macroWC.ShiftNext();
					continue;
				}
				macroWC.Remove();
				macroWC.InsertRange(args[w.Number]);
				macroWC.Pointer += args[w.Number].Collection.Count;
			}
			wc.InsertRange(macroWC);
			wc.Pointer = macroStart;
			return wc;
		}

		/// <summary>
		/// Starts from right after @" etc.
		/// On return, the character of endWith should be Current. Verification of proper termination is done by the caller.
		/// </summary>
		/// <returns></returns>
		public static StrFormWord AnalyseFormattedString(StringStream st, FormStrEndWith endWith, bool trim)
		{
			List<string> strs = new List<string>();
			List<SubWord> SWTs = new List<SubWord>();
			StringBuilder buffer = new StringBuilder(100);
			while (true)
			{
				char cur = st.Current;
				switch (cur)
				{
					case '\n':
					case '\0':
						goto end;
					case '\"':
						if (endWith == FormStrEndWith.DoubleQuotation)
							goto end;
						buffer.Append(cur);
						break;
					case '#':
						if (endWith == FormStrEndWith.Sharp)
							goto end;
						buffer.Append(cur);
						break;
					case ',':
						if ((endWith == FormStrEndWith.Comma) || (endWith == FormStrEndWith.LeftParenthesis_Bracket_Comma_Semicolon))
							goto end;
						buffer.Append(cur);
						break;
					case '(':
					case '[':
					case ';':
						if (endWith == FormStrEndWith.LeftParenthesis_Bracket_Comma_Semicolon)
							goto end;
						buffer.Append(cur);
						break;
					case '%':
						strs.Add(buffer.ToString());
						buffer.Remove(0, buffer.Length);
						st.ShiftNext();
						SWTs.Add(new PercentSubWord(Analyse(st, LexEndWith.Percent, LexAnalyzeFlag.None)));
						if (st.Current != '%')
							throw new CodeEE(GameMessages.T("\'%\' was used but a matching \'%\' was not found."));
						break;
					case '{':
						strs.Add(buffer.ToString());
						buffer.Remove(0, buffer.Length);
						st.ShiftNext();
						SWTs.Add(new CurlyBraceSubWord(Analyse(st, LexEndWith.RightCurlyBrace, LexAnalyzeFlag.None)));
						if (st.Current != '}')
							throw new CodeEE(GameMessages.T("\'{\' was used but a matching \'}\' was not found."));
						break;
					case '*':
					case '+':
					case '=':
					case '/':
					case '$':
						if (!Config.SystemIgnoreTripleSymbol && st.TripleSymbol())
						{
							strs.Add(buffer.ToString());
							buffer.Remove(0, buffer.Length);
							st.Jump(3);
							SWTs.Add(new TripleSymbolSubWord(cur));
							continue;
						}
						else
							buffer.Append(cur);
						break;
					case '\\'://Escape character usage

						st.ShiftNext();
						cur = st.Current;
						switch (cur)
						{
							case '\0':
								throw new CodeEE(GameMessages.T("No character follows the escape character \\."));
							case '\n': break;
							case 's': buffer.Append(' '); break;
							case 'S': buffer.Append('　'); break;
							case 't': buffer.Append('\t'); break;
							case 'n': buffer.Append('\n'); break;
							case '@'://\@~~?~~#~~\@
								{
									if ((endWith == FormStrEndWith.YenAt) || (endWith == FormStrEndWith.Sharp))
										goto end;
									strs.Add(buffer.ToString());
									buffer.Remove(0, buffer.Length);
									st.ShiftNext();
									SWTs.Add(AnalyseYenAt(st));
									continue;
								}
							default:
								buffer.Append(cur);
								st.ShiftNext();
								continue;
						}
						break;
					default:
						buffer.Append(cur);
						break;
				}
				st.ShiftNext();
			}
		end:
			strs.Add(buffer.ToString());

			string[] retStr = new string[strs.Count];
			SubWord[] retSWTs = new SubWord[SWTs.Count];
			strs.CopyTo(retStr);
			SWTs.CopyTo(retSWTs);
			if (trim && retStr.Length > 0)
			{
				retStr[0] = retStr[0].TrimStart(new char[] { ' ', '\t' });
				retStr[retStr.Length - 1] = retStr[retStr.Length - 1].TrimEnd(new char[] { ' ', '\t' });
			}
			return new StrFormWord(retStr, retSWTs);
		}



		/// <summary>
		/// Starts from right after \@, and the character right after \@ becomes the Current
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static YenAtSubWord AnalyseYenAt(StringStream st)
		{
			WordCollection w = Analyse(st, LexEndWith.Question, LexAnalyzeFlag.None);
			if (st.Current != '?')
				throw new CodeEE(GameMessages.T("\'\\@\' was used but a matching \'?\' was not found."));
			st.ShiftNext();
			StrFormWord left = AnalyseFormattedString(st, FormStrEndWith.Sharp, true);
			if (st.Current != '#')
			{
				if (st.Current != '@')
					throw new CodeEE(GameMessages.T("\'\\@\',\'?\' was used but a matching \'#\' was not found."));
				st.ShiftNext();
				ParserMediator.Warn(GameMessages.T("\'\\@\',\'?\' was used but a matching \'#\' was not found."), GlobalStatic.Process.GetScaningLine(), 1, false, false);
				return new YenAtSubWord(w, left, null);
			}
			st.ShiftNext();
			StrFormWord right = AnalyseFormattedString(st, FormStrEndWith.YenAt, true);
			if (st.Current != '@')
				throw new CodeEE(GameMessages.T("\'\\@\',\'?\',\'#\' was used but a matching \'\\@\' was not found."));
			st.ShiftNext();
			return new YenAtSubWord(w, left, right);
		}

		#endregion

	}
}