using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameView
{
	internal sealed class ButtonPrimitive
	{
		public string Str = "";
		public Int64 Input;
		public bool CanSelect = false;
		public override string ToString()
		{
			return Str;
		}
	}

	internal static class ButtonStringCreator
	{
		public static List<string> Split(string printBuffer)
		{
			List<ButtonPrimitive> list = syn(printBuffer);
			List<string> ret = new List<string>();
			foreach(ButtonPrimitive p in list)
				ret.Add(p.Str);
			return ret;
		}
		public static List<ButtonPrimitive> SplitButton(string printBuffer)
		{
			return syn(printBuffer);
		}

		private static List<ButtonPrimitive> syn(string printBuffer)
		{
			string printString = printBuffer.ToString();
			List<ButtonPrimitive> ret = new List<ButtonPrimitive>();
			if (printString.Length == 0)
				goto nonButton;
			List<string> strs;
			if ((!printString.Contains("[")) || (!printString.Contains("]")))
				goto nonButton;
			strs = lex(new StringStream(printString));
			if (strs == null)
				goto nonButton;
			bool beforeButton = false;//text exists before the first button ("[1]" etc.)
			bool afterButton = false;//text exists after the last button ("[1]" etc.)
			int buttonCount = 0;
			Int64 inpL = 0;
			for (int i = 0; i < strs.Count; i++)
			{
				if (strs[i].Length == 0)
					continue;
				char c = strs[i][0];
				if (LexicalAnalyzer.IsWhiteSpace(c))
				{//just whitespace
				}
				//we decided not to make non-numeric things into buttons.
				//else if ((c == '[') && (!isSymbols(strArray[i])))
				else if (isButtonCore(strs[i], ref inpL))
				{//a string enclosed in []. whether it becomes the core of a choice is not decided at this stage.
					buttonCount++;
					afterButton = false;
				}
				else
{//a string that may become the description of a choice
                    afterButton = true;
					if (buttonCount == 0)
						beforeButton = true;
				}
			}
			if (buttonCount <= 1)
			{
                ButtonPrimitive button = new ButtonPrimitive
                {
                    Str = printBuffer.ToString(),
                    CanSelect = (buttonCount >= 1),
                    Input = inpL
                };
                ret.Add(button);
				return ret;
			}
			buttonCount = 0;
			bool alignmentRight = !beforeButton && afterButton;//description is fixed right of the button
			bool alignmentLeft = beforeButton && !afterButton;//description is fixed left of the button
			bool alignmentEtc = !alignmentRight && !alignmentLeft;//respond flexibly
			bool canSelect = false;
			Int64 input = 0;

			int state = 0;
			StringBuilder buffer = new StringBuilder();
            void reduce()
            {
                if (buffer.Length == 0)
                    return;
                ButtonPrimitive button = new ButtonPrimitive
                {
                    Str = buffer.ToString(),
                    CanSelect = canSelect,
                    Input = input
                };
                ret.Add(button);
                buffer.Remove(0, buffer.Length);
                canSelect = false;
                input = 0;
            }
            for (int i = 0; i < strs.Count; i++)
			{
				if (strs[i].Length == 0)
					continue;
				char c = strs[i][0];
				if (LexicalAnalyzer.IsWhiteSpace(c))
				{//just whitespace
					if (((state & 3) == 3) && (alignmentEtc) && (strs[i].Length >= 2))
					{//once something containing the core and the description is complete, generate a button.
						//spaces of one char or fewer are not cared about. countermeasure for the character purchase screen
                        reduce();
						buffer.Append(strs[i]);
						state = 0;
					}
					else
					{
						buffer.Append(strs[i]);
					}
					continue;
				}
				if(isButtonCore(strs[i], ref inpL))
				{
					buttonCount++;
					if (((state & 1) == 1) || alignmentRight)
					{//buffer already contains the core, or forced right placement
						reduce();
						buffer.Append(strs[i]);
						input = inpL;
						canSelect = true;
						state = 1;
					}//((state & 2) == 2) || 
					else if (alignmentLeft)
					{//buffer contains the description, or forced left placement
						buffer.Append(strs[i]);
						input = inpL;
						canSelect = true;
						reduce();
						state = 0;
					}
					else
					{//buffer is empty or a whitespace string
						buffer.Append(strs[i]);
						input = inpL;
						canSelect = true;
						state = 1;
					}
					continue;
				}
				//else
				//{//a string that may become the description of a choice
					
					buffer.Append(strs[i]);
					state |= 2;
				//}
				
			};
			reduce();
			return ret;
		nonButton:
			ret = new List<ButtonPrimitive>();
            ButtonPrimitive singleButton = new ButtonPrimitive
            {
                Str = printString
            };
            ret.Add(singleButton);
			return ret;
		}
		readonly static Regex numReg = new Regex(@"\[\s*([0][xXbB])?[+-]?[0-9]+([eEpP][0-9]+)?\s*\]");

		/// <summary>
		/// Checks whether a []-enclosed string is numeric
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static bool isNumericWord(string str)
		{
			return numReg.IsMatch(str);
		}

		/// <summary>
		/// Whether it becomes the core of a button. For now, only integers.
		/// The try-catch used makes it somewhat heavy.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private static bool isButtonCore(string str, ref long input)
		{
			if((str == null)||(str.Length < 3)||(str[0] != '[')||(str[str.Length-1] != ']'))
				return false;
			if (!isNumericWord(str))
				return false;
			string buttonStr = str.Substring(1, str.Length - 2);
			StringStream stInt = new StringStream(buttonStr);
			LexicalAnalyzer.SkipAllSpace(stInt);
			try
			{
				input = LexicalAnalyzer.ReadInt64(stInt, false);
			}
			catch
			{
				return false; 
			}
			return true;
		}

		/// <summary>
		/// Lexical splitting
		/// splits "[1] A [2] B " into "[1]"," ", "A"," ","[2]"," ","B"," "
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		private static List<string> lex(StringStream st)
		{
			List<string> strs = new List<string>();
			int state = 0;
			int startIndex = 0;
            void reduce()
            {
                if (st.CurrentPosition == startIndex)
                    return;
                int length = st.CurrentPosition - startIndex;
                strs.Add(st.Substring(startIndex, length));
                startIndex = st.CurrentPosition;
            }
            while (!st.EOS)
			{
				if (st.Current == '[')
				{
					if (state == 1)//inside "["
						goto unanalyzable;
					reduce();
					state = 1;
					st.ShiftNext();
				}
				else if (st.Current == ']')
				{
					if (state != 1)//outside "["
						goto unanalyzable;
					st.ShiftNext();
					reduce();
					state = 0;
				}
				else if ((state == 0) && (LexicalAnalyzer.IsWhiteSpace(st.Current)))
				{
					reduce();
					LexicalAnalyzer.SkipAllSpace(st);
					reduce();
				}
				else
				{
					st.ShiftNext();
				}
			}
			reduce();
			return strs;
		unanalyzable:
			return null;
		}

	}
}
