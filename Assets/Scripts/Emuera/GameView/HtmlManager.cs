using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MinorShift.Emuera.Sub;
//using System.Drawing;
using MinorShift.Emuera.GameData.Expression;
using uEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{
	/* Emuera HTML-like markup implementation - core markup features implemented
	 * (Aim for 1:1 correspondence between HTML and ConsoleDisplayLine. Don't duplicate tags that produce the same result like <b> and <strong>)
	 * ✅ <p align=""></p> Equivalent to ALIGNMENT command - line start to line end only, end tag can be omitted
	 * ✅ <nobr></nobr> Equivalent to PRINTSINGLE - line start to line end only, end tag can be omitted
	 * ✅ <b><i><u><s> Various font styles
	 * ✅ <button value=""></button> Button creation
	 * ✅ <font face="" color="" bcolor=""></font> Font specification, color specification, button selection color specification
	 * ✅ <!-- --> Comment
	 * ✅ <nonbutton title='～～'> Non-button text with title attribute
	 * ✅ <img src='～～' srcb='～～' srcm='～～' height='' width='' ypos=''> Image with button variant
	 * ✅ <shape type='rect' param='0,0,0,0' color='' bcolor=''> Shape drawing (rect, space, polygon)
	 * ✅ Escape sequences: &amp; &gt; &lt; &quot; &apos;
	 * ✅ &#nn; &#xnn; Unicode character references
	 */
	/* What this class should support:
	 * HTML to ConsoleDisplayLine[] - Mainly for display
	 * ConsoleDisplayLine[] to HTML - Convert current display to string for saving?
	 * HTML to ConsoleDisplayLine[] to HTML - Check where line breaks occur without displaying
	 * HTML to PlainText (unescaped)
	 * Text to escaped Text
	 */
	/// <summary>
	/// HTML-like markup parser for EmueraConsole.
	/// Handles parsing of HTML-like tags for text formatting, buttons, images, and shapes.
	/// </summary>
	internal static class HtmlManager
	{
		static HtmlManager()
		{
			repDic.Add('&', "&amp;");
			repDic.Add('>', "&gt;");
			repDic.Add('<', "&lt;");
			repDic.Add('\"', "&quot;");
			repDic.Add('\'', "&apos;");
		}
		static readonly char[] rep = new char[] { '&', '>', '<', '\"', '\'' };
		static readonly Dictionary<char, string> repDic = new Dictionary<char, string>();
		private sealed class HtmlAnalzeStateFontTag
		{
			public int Color = -1;
			public int BColor = -1;
			public string FontName = null;
			//public int PointX = 0;
			//public bool PointXisLocked = false;
		}

		private sealed class HtmlAnalzeStateButtonTag
		{
			public bool IsButton = true;
			public bool IsButtonTag = true;
			public Int64 ButtonValueInt = 0;
			public string ButtonValueStr = null;
			public string ButtonTitle = null;
			public bool ButtonIsInteger = false;
			public int PointX = 0;
			public bool PointXisLocked = false;
		}

		private sealed class HtmlAnalzeState
		{
			public bool LineHead = true;//line-head flag. the state where no text has appeared yet
			public FontStyle FontStyle = FontStyle.Regular;
			public List<HtmlAnalzeStateFontTag> FonttagList = new List<HtmlAnalzeStateFontTag>();
			public bool FlagNobr = false;//Errors when </nobr> is used while false
			public bool FlagP = false;//Errors when </p> is used while false
			public bool FlagNobrClosed = false;//Errors when </nobr> is used while true
			public bool FlagPClosed = false;//Errors when </p> is used while true
			public DisplayLineAlignment Alignment = DisplayLineAlignment.LEFT;

			/// <summary>
			/// Button tag information about the string(s) added so far
			/// </summary>
			public HtmlAnalzeStateButtonTag LastButtonTag = null;
			/// <summary>
			/// The latest button tag information
			/// </summary>
			public HtmlAnalzeStateButtonTag CurrentButtonTag = null;
			// <clearbutton> temporarily suppresses button activation and optionally titles.
			public bool FlagClearButton = false;
			public bool FlagClearButtonTooltip = false;

			public bool FlagBr = false;//reservation of a forced line break by <br>
			public bool FlagButton = false;//reservation of buttonification by <button></button>

			public StringStyle GetSS()
			{
				Color c = Config.ForeColor;
				Color b = Config.FocusColor;
				string fontname = null;
				bool colorChanged = false;
				if (FonttagList.Count > 0)
				{
					HtmlAnalzeStateFontTag font = FonttagList[FonttagList.Count - 1];
					fontname = font.FontName;
					if (font.Color >= 0)
					{
						colorChanged = true;
						c = Color.FromArgb(font.Color >> 16, (font.Color >> 8) & 0xFF, font.Color & 0xFF);
					}
					if (font.BColor >= 0)
					{
						b = Color.FromArgb(font.BColor >> 16, (font.BColor >> 8) & 0xFF, font.BColor & 0xFF);
					}
				}
				return new StringStyle(c, colorChanged, b, FontStyle, fontname);
			}
		}

		/// <summary>
		/// Conversion from display lines to html
		/// </summary>
		/// <param name="lines"></param>
		/// <returns></returns>
		public static string DisplayLine2Html(ConsoleDisplayLine[] lines, bool needPandN)
		{
			if (lines == null || lines.Length == 0)
				return "";
			StringBuilder b = new StringBuilder();
			if (needPandN)
			{
				switch (lines[0].Align)
				{
					case DisplayLineAlignment.LEFT:
						b.Append("<p align='left'>");
						break;
					case DisplayLineAlignment.CENTER:
						b.Append("<p align='center'>");
						break;
					case DisplayLineAlignment.RIGHT:
						b.Append("<p align='right'>");
						break;
				}
				b.Append("<nobr>");
			}
			for (int dispCounter = 0; dispCounter < lines.Length; dispCounter++)
			{
				if (dispCounter != 0)
					b.Append("<br>");
				ConsoleButtonString[] buttons = lines[dispCounter].Buttons;
				for (int buttonCounter = 0; buttonCounter < buttons.Length; buttonCounter++)
				{
					string titleValue = null;
					if (!string.IsNullOrEmpty(buttons[buttonCounter].Title))
						titleValue = Escape(buttons[buttonCounter].Title);
					bool hasTag = buttons[buttonCounter].IsButton || titleValue != null
						|| buttons[buttonCounter].PointXisLocked;
					if (hasTag)
					{
						if (buttons[buttonCounter].IsButton)
						{
							string attrValue = Escape(buttons[buttonCounter].Inputs);
							b.Append("<button value='");
							b.Append(attrValue);
							b.Append("'");
						}
						else
						{
							b.Append("<nonbutton");
						}
						if (titleValue != null)
						{
							b.Append(" title='");
							b.Append(titleValue);
							b.Append("'");
						}
						if (buttons[buttonCounter].PointXisLocked)
						{
							b.Append(" pos='");
							b.Append(buttons[buttonCounter].RelativePointX.ToString());
							b.Append("'");
						}
						b.Append(">");
					}
					AConsoleDisplayPart[] parts = buttons[buttonCounter].StrArray;
					for (int cssCounter = 0; cssCounter < parts.Length; cssCounter++)
					{
						if (parts[cssCounter] is ConsoleStyledString)
						{
							ConsoleStyledString css = parts[cssCounter] as ConsoleStyledString;
							b.Append(getStringStyleStartingTag(css.StringStyle));
							b.Append(Escape(css.Str));
							b.Append(getClosingStyleStartingTag(css.StringStyle));
						}
						else if (parts[cssCounter] is ConsoleImagePart)
						{
							b.Append(parts[cssCounter].AltText);
							//ConsoleImagePart img = (ConsoleImagePart)parts[cssCounter];
							//b.Append("<img src='");
							//b.Append(Escape(img.ResourceName));
							//if(img.ButtonResourceName != null)
							//{
							//	b.Append("' srcb='");
							//	b.Append(Escape(img.ButtonResourceName));
							//}
							//b.Append("'>");
						}
						else if (parts[cssCounter] is ConsoleShapePart)
						{
							b.Append(parts[cssCounter].AltText);
						}

					}
					if (hasTag)
					{
						if (buttons[buttonCounter].IsButton)
							b.Append("</button>");
						else
							b.Append("</nonbutton>");
					}

				}
			}
			if(needPandN)
			{
				b.Append("</nobr>");
				b.Append("</p>");
			}
			return b.ToString();
		}

		public static string[] HtmlTagSplit(string str)
		{
			List<string> strList = new List<string>();
			StringStream st = new StringStream(str);
			int found = -1;
			while (!st.EOS)
			{
				found = st.Find('<');
				if (found < 0)
				{
					strList.Add(st.Substring());
					break;
				}
				else if (found > 0)
				{
					strList.Add(st.Substring(st.CurrentPosition, found));
					st.CurrentPosition += found;
				}
				found = st.Find('>');
				if(found < 0)
					return null;
				found++;
				strList.Add(st.Substring(st.CurrentPosition, found));
				st.CurrentPosition += found;
			}
			string[] ret = new string[strList.Count];
			strList.CopyTo(ret);
			return ret;
		}
		
		/// <summary>
		/// Creation of display lines from html
		/// </summary>
		/// <param name="str">html text</param>
		/// <param name="sm"></param>
		/// <param name="console">set to null when not used for actual display</param>
		/// <returns></returns>
		public static ConsoleDisplayLine[] Html2DisplayLine(string str, StringMeasure sm, EmueraConsole console)
		{
			List<AConsoleDisplayPart> cssList = new List<AConsoleDisplayPart>();
			List<ConsoleButtonString> buttonList = new List<ConsoleButtonString>();
			StringStream st = new StringStream(str);
			int found;
			bool hasComment = str.IndexOf("<!--") >= 0;
			bool hasReturn = str.IndexOf('\n') >= 0;
			HtmlAnalzeState state = new HtmlAnalzeState();
			while (!st.EOS)
			{
				found = st.Find('<');
				if (hasReturn)
				{
					int rFound = st.Find('\n');
					if (rFound >= 0 && (found > rFound || found < 0))
						found = rFound;
				}
				if (found < 0)
				{
					string txt = Unescape(st.Substring());
					cssList.Add(new ConsoleStyledString(txt, state.GetSS()));
					if (state.FlagPClosed)
						throw new CodeEE(GameMessages.T("There is text after </p>"));
					if (state.FlagNobrClosed)
						throw new CodeEE(GameMessages.T("There is text after </nobr>"));
					break;
				}
				else if (found > 0)
				{
					string txt = Unescape(st.Substring(st.CurrentPosition, found));
					cssList.Add(new ConsoleStyledString(txt, state.GetSS()));
					state.LineHead = false;
					st.CurrentPosition += found;
				}
				//only comment tags are given special treatment
				if (hasComment && st.CurrentEqualTo("<!--"))
				{
					st.CurrentPosition += 4;
					found = st.Find("-->");
					if (found < 0)
						throw new CodeEE(GameMessages.T("Comment end tag \"-->\" was not found"));
					st.CurrentPosition += found + 3;
					continue;
				}
				if (hasReturn && st.Current == '\n')//treats \n in text as <br>
				{
					state.FlagBr = true;
					st.ShiftNext();
				}
				else//tag analysis
				{
					st.ShiftNext();
					AConsoleDisplayPart part = tagAnalyze(state, st);
					if (st.Current != '>')
						throw new CodeEE(GameMessages.T("Tag terminator '>' was not found"));
					if (part != null)
						cssList.Add(part);
					st.ShiftNext();
				}

				if (state.FlagBr)
				{
					state.LastButtonTag = state.CurrentButtonTag;
					if (cssList.Count > 0)
						buttonList.Add(cssToButton(cssList, state, console));
					buttonList.Add(null);
				}
				if (state.FlagButton && cssList.Count > 0)
				{
					buttonList.Add(cssToButton(cssList, state, console));
				}
				state.FlagBr = false;
				state.FlagButton = false;
				state.LastButtonTag = state.CurrentButtonTag;
			}
			//omitting </nobr></p> is permitted
			if (state.CurrentButtonTag != null || state.FontStyle != FontStyle.Regular || state.FonttagList.Count > 0)
				throw new CodeEE(GameMessages.T("There are unclosed tags"));
			if (cssList.Count > 0)
				buttonList.Add(cssToButton(cssList, state, console));

			foreach(ConsoleButtonString button in buttonList)
			{
				if (button != null && button.PointXisLocked)
				{
					if (!state.FlagNobr)
						throw new CodeEE(GameMessages.T("The pos attribute cannot be used on lines where <nobr> is not set"));
					if (state.Alignment != DisplayLineAlignment.LEFT)
						throw new CodeEE(GameMessages.T("The pos attribute cannot be used on lines where align is not left"));
					break;
				}
			}
			ConsoleDisplayLine[] ret = PrintStringBuffer.ButtonsToDisplayLines(buttonList, sm, state.FlagNobr, false);

			foreach (ConsoleDisplayLine dl in ret)
			{
				dl.SetAlignment(state.Alignment);
			}
			return ret;
		}

		public static string Html2PlainText(string str)
		{
			string ret = Regex.Replace(str, "\\<[^<]*\\>", "");
			return Unescape(ret);
		}

		public static string Escape(string str)
		{
			//it seems there is a convenient class in Net4.5
			//return System.Web.HttpUtility.HtmlEncode(str);

			int index = 0;
			int found = 0;
			StringBuilder b = new StringBuilder();
			while (index < str.Length)
			{
				found = str.IndexOfAny(rep, index);
				if (found < 0)//if nothing is found, append the rest and end
				{
					b.Append(str.Substring(index));
					break;
				}
				if (found > index)//if there are unescaped characters in between, add them first
					b.Append(str.Substring(index, found - index));
				string repnew = repDic[str[found]];
				b.Append(repnew);
				index = found + 1;
			}
			return b.ToString();
		}

		public static string Unescape(string str)
		{
			int index = 0;
			int found = str.IndexOf('&', index);
			if (found < 0)
				return str;
			StringBuilder b = new StringBuilder();
			//just keeps replacing &～;
			while (index < str.Length)
			{
				found = str.IndexOf('&', index);
				if (found < 0)//if nothing is found, append the rest and end
				{
					b.Append(str.Substring(index));
					break;
				}
				if (found > index)//if there are unescaped characters in between, add them first
					b.Append(str.Substring(index, found - index));
				index = found;
				found = str.IndexOf(';', index);
				if (found <= index + 1)
				{
					if (found < 0)
						throw new CodeEE(GameMessages.T("No ';' corresponding to '&' was found"));
					throw new CodeEE(GameMessages.T("'&' and ';' are consecutive"));
				}
				string escWordRow = str.Substring(index + 1, found - index - 1);
				index = found + 1;
				string escWord = escWordRow.ToLower();
				int unicode = 0;
				switch (escWord)
				{
					case "nbsp": b.Append(" "); break;
					case "amp": b.Append("&"); break;
					case "gt": b.Append(">"); break;
					case "lt": b.Append("<"); break;
					case "quot": b.Append("\""); break;
					case "apos": b.Append("\'"); break;
					default:
						{
							int iBbase = 10;
							if (escWord[0] != '#')
								throw new CodeEE(GameMessages.T("\"&") + escWordRow + GameMessages.T(";\" is not a valid character reference"));
							if (escWord.Length > 1 && escWord[1] == 'x')
							{
								iBbase = 16;
								escWord = escWord.Substring(2);
							}
							else
								escWord = escWord.Substring(1);
							try
							{
								unicode = Convert.ToInt32(escWord, iBbase);
							}
							catch
							{

								throw new CodeEE(GameMessages.T("\"&") + escWordRow + GameMessages.T(";\" is not a valid character reference"));
							}

							if (unicode < 0 || unicode > 0xFFFF)
								throw new CodeEE(GameMessages.T("\"&") + escWordRow + GameMessages.T(";\" is outside the Unicode range (surrogate pairs cannot be used)"));
							b.Append((char)unicode);
							break;
						}
				}
			}
			return b.ToString();
		}

		/// <summary>
		/// Convert the css collected so far into a button. Caused by br tags, end-of-line, and button tags
		/// </summary>
		/// <param name="cssList"></param>
		/// <param name="isbutton"></param>
		/// <param name="state"></param>
		/// <param name="console"></param>
		/// <returns></returns>
		private static ConsoleButtonString cssToButton(List<AConsoleDisplayPart> cssList, HtmlAnalzeState state, EmueraConsole console)
		{
			AConsoleDisplayPart[] css = new AConsoleDisplayPart[cssList.Count];
			cssList.CopyTo(css);
			cssList.Clear();
			ConsoleButtonString ret = null;
			if (state.LastButtonTag != null && state.LastButtonTag.IsButton)
			{
				if (state.LastButtonTag.ButtonIsInteger)
					ret = new ConsoleButtonString(console, css, state.LastButtonTag.ButtonValueInt, state.LastButtonTag.ButtonValueStr);
				else
					ret = new ConsoleButtonString(console, css, state.LastButtonTag.ButtonValueStr);
			}
			else
			{
				ret = new ConsoleButtonString(console, css);
				ret.Title = null;
			}
			if (state.LastButtonTag != null)
			{
				ret.Title = state.LastButtonTag.ButtonTitle;
				if(state.LastButtonTag.PointXisLocked)
				{
					ret.LockPointX(state.LastButtonTag.PointX);
				}
			}
			return ret;
		}

		public static string GetColorToString(Color color)
		{
			StringBuilder b = new StringBuilder();
			b.Append("#");
			int colorValue = color.R * 0x10000 + color.G * 0x100 + color.B;
			b.Append(colorValue.ToString("X6"));
			return b.ToString();
		}
		private static string getStringStyleStartingTag(StringStyle style)
		{
			bool fontChanged = !((style.Fontname == null || style.Fontname == Config.FontName)&& !style.ColorChanged && (style.ButtonColor == Config.FocusColor));
			if (!fontChanged && style.FontStyle == FontStyle.Regular)
				return "";
			StringBuilder b = new StringBuilder();
			if (fontChanged)
			{
				b.Append("<font");
				if (style.Fontname != null && style.Fontname != Config.FontName)
				{
					b.Append(" face='");
					b.Append(HtmlManager.Escape(style.Fontname));
					b.Append("'");
				}
				if (style.ColorChanged)
				{
					b.Append(" color='#");
					int colorValue = style.Color.R * 0x10000 + style.Color.G * 0x100 + style.Color.B;
					b.Append(colorValue.ToString("X6"));
					b.Append("'");
				}
				if (style.ButtonColor != Config.FocusColor)
				{
					b.Append(" bcolor='#");
					int colorValue = style.ButtonColor.R * 0x10000 + style.ButtonColor.G * 0x100 + style.ButtonColor.B;
					b.Append(colorValue.ToString("X6"));
					b.Append("'");
				}
				b.Append(">");
			}
			if (style.FontStyle != FontStyle.Regular)
			{
				if ((style.FontStyle & FontStyle.Strikeout) != FontStyle.Regular)
					b.Append("<s>");
				if ((style.FontStyle & FontStyle.Underline) != FontStyle.Regular)
					b.Append("<u>");
				if ((style.FontStyle & FontStyle.Italic) != FontStyle.Regular)
					b.Append("<i>");
				if ((style.FontStyle & FontStyle.Bold) != FontStyle.Regular)
					b.Append("<b>");
			}

			return b.ToString();
		}

		private static string getClosingStyleStartingTag(StringStyle style)
		{
			bool fontChanged = !((style.Fontname == null || style.Fontname == Config.FontName) && !style.ColorChanged && (style.ButtonColor == Config.FocusColor));
			if (!fontChanged && style.FontStyle == FontStyle.Regular)
				return "";
			StringBuilder b = new StringBuilder();
			if (style.FontStyle != FontStyle.Regular)
			{
				if ((style.FontStyle & FontStyle.Bold) != FontStyle.Regular)
					b.Append("</b>");
				if ((style.FontStyle & FontStyle.Italic) != FontStyle.Regular)
					b.Append("</i>");
				if ((style.FontStyle & FontStyle.Underline) != FontStyle.Regular)
					b.Append("</u>");
				if ((style.FontStyle & FontStyle.Strikeout) != FontStyle.Regular)
					b.Append("</s>");
			}
			if (fontChanged)
				b.Append("</font>");
			return b.ToString();
		}

		private static AConsoleDisplayPart tagAnalyze(HtmlAnalzeState state, StringStream st)
		{
			bool endTag = (st.Current == '/');
			string tag;
			if (endTag)
			{
				st.ShiftNext();
				int found = st.Find('>');
				if (found < 0)
				{
					st.CurrentPosition = st.RowString.Length;
					return null;//the Error is raised at the return point
				}
				tag = st.Substring(st.CurrentPosition, found).Trim();
				st.CurrentPosition += found;
				FontStyle endStyle = FontStyle.Strikeout;
				switch (tag.ToLower())
				{
					case "b": endStyle = FontStyle.Bold; goto case "s";
					case "i": endStyle = FontStyle.Italic; goto case "s";
					case "u": endStyle = FontStyle.Underline; goto case "s";
					case "s":
						if ((state.FontStyle & endStyle) == FontStyle.Regular)
							throw new CodeEE("</" + tag + GameMessages.T("> before <") + tag + GameMessages.T("> is missing"));
						state.FontStyle ^= endStyle;
						return null;
					case "p":
						if ((!state.FlagP) || (state.FlagPClosed))
							throw new CodeEE(GameMessages.T("There is no <p> before </p>"));
						state.FlagPClosed = true;
						return null;
					case "nobr":
						if ((!state.FlagNobr) || (state.FlagNobrClosed))
							throw new CodeEE(GameMessages.T("There is no <nobr> before </nobr>"));
						state.FlagNobrClosed = true;
						return null;
					case "font":
						if (state.FonttagList.Count == 0)
							throw new CodeEE(GameMessages.T("There is no <font> before </font>"));
						state.FonttagList.RemoveAt(state.FonttagList.Count - 1);
						return null;
					case "button":
						if (state.CurrentButtonTag == null || !state.CurrentButtonTag.IsButtonTag)
							throw new CodeEE(GameMessages.T("There is no <button> before </button>"));
						state.CurrentButtonTag = null;
						state.FlagButton = true;
						return null;
					case "nonbutton":
						if (state.CurrentButtonTag == null || state.CurrentButtonTag.IsButtonTag)
							throw new CodeEE(GameMessages.T("There is no <nonbutton> before </nonbutton>"));
						state.CurrentButtonTag = null;
						state.FlagButton = true;
						return null;
					case "clearbutton":
						if (!state.FlagClearButton)
							throw new CodeEE(GameMessages.T("There is no <clearbutton> before </clearbutton>"));
						state.FlagClearButton = false;
						state.FlagClearButtonTooltip = false;
						return null;
				case "div":
					// Keep the pre-existing graceful fallback until the display-part model
					// can represent a positioned subdivision. The content remains inline.
					return null;
				default:
					throw new CodeEE(GameMessages.T("End tag </")+tag+GameMessages.T("> cannot be interpreted"));
				}
				//goto error;
			}
			//from here on are opening tags

			bool tempUseMacro = LexicalAnalyzer.UseMacro;
			WordCollection wc = null;
			try
			{
				LexicalAnalyzer.UseMacro = false;//temporarily stop macro expansion
				tag = LexicalAnalyzer.ReadSingleIdentifier(st);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (st.Current != '>')
					wc = LexicalAnalyzer.Analyse(st, LexEndWith.GreaterThan, LexAnalyzeFlag.AllowAssignment | LexAnalyzeFlag.AllowSingleQuotationStr);
			}
			finally
			{
				LexicalAnalyzer.UseMacro = tempUseMacro;
			}
			if (string.IsNullOrEmpty(tag))
				goto error;
			IdentifierWord word;
			FontStyle newStyle = FontStyle.Strikeout;
            switch (tag.ToLower())
			{
				case "b": newStyle = FontStyle.Bold; goto case "s";
				case "i": newStyle = FontStyle.Italic; goto case "s";
				case "u": newStyle = FontStyle.Underline; goto case "s";
				case "s":
					if (wc != null)
						throw new CodeEE("<" + tag + GameMessages.T("> tag has attributes set"));
					if ((state.FontStyle & newStyle) != FontStyle.Regular)
						throw new CodeEE("<" + tag + GameMessages.T("> is used twice"));
					state.FontStyle |= newStyle;
						return null;
				case "br":
					if (wc != null)
						throw new CodeEE("<" + tag + GameMessages.T("> tag has attributes set"));
					state.FlagBr = true;
						return null;
				case "nobr":
					if (wc != null)
						throw new CodeEE("<" + tag + GameMessages.T("> tag has attributes set"));
					if (!state.LineHead)
						throw new CodeEE(GameMessages.T("<nobr> is used other than at the line head"));
					if (state.FlagNobr)
						throw new CodeEE(GameMessages.T("<nobr> is used more than once"));
					state.FlagNobr = true;
						return null;
				case "p":
					{
						if (wc == null)
							throw new CodeEE("<" + tag + GameMessages.T("> tag has no attributes set"));
						if (!state.LineHead)
							throw new CodeEE(GameMessages.T("<p> is used other than at the line head"));
						if (state.FlagNobr)
							throw new CodeEE(GameMessages.T("<p> is used more than once"));
						word = wc.Current as IdentifierWord;
						wc.ShiftNext();
						OperatorWord op = wc.Current as OperatorWord;
						wc.ShiftNext();
						LiteralStringWord attr = wc.Current as LiteralStringWord;
						wc.ShiftNext();
						if (!wc.EOL || word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
							goto error;
						if (!word.Code.Equals("align", StringComparison.OrdinalIgnoreCase))
							throw new CodeEE(GameMessages.T("<p> tag attribute name ") + word.Code + GameMessages.T(" cannot be interpreted"));
						string attrValue = Unescape(attr.Str);
						switch (attrValue.ToLower())
						{
							case "left":
								state.Alignment = DisplayLineAlignment.LEFT;
								break;
							case "center":
								state.Alignment = DisplayLineAlignment.CENTER;
								break;
							case "right":
								state.Alignment = DisplayLineAlignment.RIGHT;
								break;
							default:
								throw new CodeEE(GameMessages.T("Attribute value ") + attr.Str + GameMessages.T(" cannot be interpreted"));
						}
						state.FlagP = true;
						return null;
					}
				case "img":
					{
						if (wc == null)
							throw new CodeEE($"<{tag}> tag has no attributes specified");
						string attrValue = null;
					string src = null;
					string srcb = null;
					string srcm = null;
						MixedNum height = new MixedNum(); ;
						MixedNum width = new MixedNum(); ;
						MixedNum ypos = new MixedNum(); ;
						while (wc != null && !wc.EOL)
						{
							word = wc.Current as IdentifierWord;
							wc.ShiftNext();
							OperatorWord op = wc.Current as OperatorWord;
							wc.ShiftNext();
							LiteralStringWord attr = wc.Current as LiteralStringWord;
							wc.ShiftNext();
							if (word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
								goto error;
							attrValue = Unescape(attr.Str);
							if (word.Code.Equals("src", StringComparison.OrdinalIgnoreCase))
							{
								if (src != null)
									throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
								src = attrValue;
							}
						else if (word.Code.Equals("srcb", StringComparison.OrdinalIgnoreCase))
						{
							if (srcb != null)
								throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
							srcb = attrValue;
						}
						else if (word.Code.Equals("srcm", StringComparison.OrdinalIgnoreCase))
						{
							if (srcm != null)
								throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
							srcm = attrValue;
						}
						else if (word.Code.Equals("height", StringComparison.OrdinalIgnoreCase))
							{
								if (height.num != 0)
									throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
								if (attrValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
								{
									height.isPx = true;
									attrValue = attrValue.Substring(0, attrValue.Length - 2);
								}
								if (!int.TryParse(attrValue, out height.num))
									throw new CodeEE($"<{tag}> tag: 'height' attribute value cannot be parsed as a number");
							}
							else if (word.Code.Equals("width", StringComparison.OrdinalIgnoreCase))
							{
								if (width.num != 0)
									throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
								if (attrValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
								{
									width.isPx = true;
									attrValue = attrValue.Substring(0, attrValue.Length - 2);
								}
								if (!int.TryParse(attrValue, out width.num))
									throw new CodeEE($"<{tag}> tag: 'width' attribute value cannot be parsed as a number");
							}
							else if (word.Code.Equals("ypos", StringComparison.OrdinalIgnoreCase))
							{
								if (ypos.num != 0)
									throw new CodeEE($"<{tag}> tag: '{word.Code}' attribute specified more than once");
								if (attrValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
								{
									ypos.isPx = true;
									attrValue = attrValue.Substring(0, attrValue.Length - 2);
								}
								if (!int.TryParse(attrValue, out ypos.num))
									throw new CodeEE($"<{tag}> tag: 'ypos' attribute value cannot be parsed as a number");
							}
							else
								throw new CodeEE($"<{tag}> tag: attribute name '{word.Code}' cannot be interpreted");
						}
						if (src == null)
							throw new CodeEE($"<{tag}> tag requires 'src' attribute to specify image resource name");
					// Create ConsoleImagePart which will load the image from resources folder via AppContents.GetSprite()
					return new ConsoleImagePart(src, srcb, srcm, height, width, ypos);
					}

				case "div":
					{
						if (wc != null)
							while (!wc.EOL)
								wc.ShiftNext();
						return null;
					}
			case "shape":
					{
						if (wc == null)
							throw new CodeEE("<" + tag + GameMessages.T("> tag has no attributes set"));
						int[] param = null;
						string type = null;
						int color = -1;
						int bcolor = -1;
						while (!wc.EOL)
						{
							word = wc.Current as IdentifierWord;
							wc.ShiftNext();
							OperatorWord op = wc.Current as OperatorWord;
							wc.ShiftNext();
							LiteralStringWord attr = wc.Current as LiteralStringWord;
							wc.ShiftNext();
							if (word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
								goto error;
							string attrValue = Unescape(attr.Str);
							switch (word.Code.ToLower())
							{
								case "color":
									if (color >= 0)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									color = stringToColorInt32(attrValue);
									break;
								case "bcolor":
									if (bcolor >= 0)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									bcolor = stringToColorInt32(attrValue);
									break;
								case "type":
									if (type != null)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									type = attrValue;
									break;
								case "param":
									if (param != null)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									{
										string[] tokens = attrValue.Split(',');
										param = new int[tokens.Length];
										for (int i = 0; i < tokens.Length; i++)
										{
											if (!int.TryParse(tokens[i], out param[i]))
												throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute value cannot be parsed as a number"));
										}
										break;
									}
								default:
									throw new CodeEE("<" + tag + GameMessages.T("> tag attribute name ") + word.Code + GameMessages.T(" cannot be interpreted"));
							}
						}
						if (param == null)
							throw new CodeEE("<" + tag + GameMessages.T("> tag has no param attribute set"));
						if (type == null)
							throw new CodeEE("<" + tag + GameMessages.T("> tag has no type attribute set"));
						Color c = Config.ForeColor;
						Color b = Config.FocusColor;
						if (color >= 0)
						{
							c = Color.FromArgb(color >> 16, (color >> 8) & 0xFF, color & 0xFF);
						}
						if (bcolor >= 0)
						{
							b = Color.FromArgb(bcolor >> 16, (bcolor >> 8) & 0xFF, bcolor & 0xFF);
						}
						return ConsoleShapePart.CreateShape(type, param, c, b, color >= 0);
					}
				case "button":
				case "nonbutton":
					{
						if (state.CurrentButtonTag != null)
							throw new CodeEE(GameMessages.T("<button> or <nonbutton> is nested"));
						HtmlAnalzeStateButtonTag buttonTag = new HtmlAnalzeStateButtonTag();
						bool isButton = tag.ToLower() == "button";
						string attrValue = null;
						string value = null;
						//if (wc == null)
						//	throw new CodeEE("<" + tag + "> tag has no attributes set");
						while (wc != null && !wc.EOL)
						{
							word = wc.Current as IdentifierWord;
							wc.ShiftNext();
							OperatorWord op = wc.Current as OperatorWord;
							wc.ShiftNext();
							LiteralStringWord attr = wc.Current as LiteralStringWord;
							wc.ShiftNext();
							if (word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
								goto error;
							attrValue = Unescape(attr.Str);
							if (word.Code.Equals("value", StringComparison.OrdinalIgnoreCase))
							{
								if (!isButton)
									throw new CodeEE("<" + tag + GameMessages.T("> tag has a value attribute set"));
								if (value != null)
                                    throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
								value = attrValue;
							}
							else if (word.Code.Equals("title", StringComparison.OrdinalIgnoreCase))
							{
								if (buttonTag.ButtonTitle != null)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
								buttonTag.ButtonTitle = attrValue;
							}
							else if (word.Code.Equals("pos", StringComparison.OrdinalIgnoreCase))
							{
                                //throw new NotImplCodeEE();
                                if (buttonTag.PointXisLocked)
                                    throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
                                if (!int.TryParse(attrValue, out int pos))
									throw new CodeEE("<" + tag + GameMessages.T("> tag: the pos attribute value cannot be parsed as a number"));
								buttonTag.PointX = pos;
								buttonTag.PointXisLocked = true;
							}
							else
								throw new CodeEE("<" + tag + GameMessages.T("> tag attribute name ") + word.Code + GameMessages.T(" cannot be interpreted"));
						}
						if (isButton)
						{
                            //if (value == null)
                            //	throw new CodeEE("<" + tag + "> tag has a value attribute set");
                            buttonTag.ButtonIsInteger = (Int64.TryParse(value, out long intValue));
                            buttonTag.ButtonValueInt = intValue;
							buttonTag.ButtonValueStr = value;
							}
							if (state.FlagClearButton)
							{
								buttonTag.IsButton = false;
								if (state.FlagClearButtonTooltip)
									buttonTag.ButtonTitle = null;
							}
							else
								buttonTag.IsButton = value != null;
							buttonTag.IsButtonTag = isButton;
							state.CurrentButtonTag = buttonTag;
							state.FlagButton = true;
							return null;
							}
							case "clearbutton":
							{
							if (state.FlagClearButton)
								throw new CodeEE(GameMessages.T("<clearbutton> tag is nested"));
							while (wc != null && !wc.EOL)
							{
								word = wc.Current as IdentifierWord;
								wc.ShiftNext();
								OperatorWord op = wc.Current as OperatorWord;
								wc.ShiftNext();
								LiteralStringWord attr = wc.Current as LiteralStringWord;
								wc.ShiftNext();
								if (word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
									goto error;
								if (!word.Code.Equals("notooltip", StringComparison.OrdinalIgnoreCase))
									throw new CodeEE(GameMessages.T("<clearbutton> tag attribute name ") + word.Code + GameMessages.T(" cannot be interpreted"));
								string value = Unescape(attr.Str);
								if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
									state.FlagClearButtonTooltip = true;
								else if (!value.Equals("false", StringComparison.OrdinalIgnoreCase))
									throw new CodeEE(GameMessages.T("<clearbutton> tag notooltip attribute value must be true or false"));
							}
							state.FlagClearButton = true;
							return null;
							}
							case "font":
					{
						if (wc == null)
							throw new CodeEE("<" + tag + GameMessages.T("> tag has no attributes set"));
						HtmlAnalzeStateFontTag font = new HtmlAnalzeStateFontTag();
						while (!wc.EOL)
						{
							word = wc.Current as IdentifierWord;
							wc.ShiftNext();
							OperatorWord op = wc.Current as OperatorWord;
							wc.ShiftNext();
							LiteralStringWord attr = wc.Current as LiteralStringWord;
							wc.ShiftNext();
							if (word == null || op == null || op.Code != OperatorCode.Assignment || attr == null)
								goto error;
							string attrValue = Unescape(attr.Str);
							switch (word.Code.ToLower())
							{
								case "color":
									if (font.Color >= 0)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									font.Color = stringToColorInt32(attrValue);
									break;
								case "bcolor":
									if (font.BColor >= 0)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									font.BColor = stringToColorInt32(attrValue);
									break;
								case "face":
									if (font.FontName != null)
										throw new CodeEE("<" + tag + GameMessages.T("> tag: ") + word.Code + GameMessages.T(" attribute specified more than once"));
									font.FontName = attrValue;
									break;
								//case "pos":
								//	{
								//		//throw new NotImplCodeEE();
								//		if (font.PointXisLocked)
								//			throw new CodeEE("<" + tag + "> tag: " + word.Code + " attribute specified more than once");
								//		int pos = 0;
								//		if (!int.TryParse(attrValue, out pos))
								//			throw new CodeEE("<font> tag: the pos attribute value cannot be parsed as a number");
								//		font.PointX = pos;
								//		font.PointXisLocked = true;
								//		break;
								//	}
								default:
								throw new CodeEE("<" + tag + GameMessages.T("> tag attribute name ") + word.Code + GameMessages.T(" cannot be interpreted"));
							}
						}
						//if inside another font tag, inherit the unset items from the font tag outside (pos excluded)
						if (state.FonttagList.Count > 0)
						{
							HtmlAnalzeStateFontTag oldFont = state.FonttagList[state.FonttagList.Count - 1];
							if (font.Color < 0)
								font.Color = oldFont.Color;
							if (font.BColor < 0)
								font.BColor = oldFont.BColor;
							if (font.FontName == null)
								font.FontName = oldFont.FontName;
						}
						state.FonttagList.Add(font);
						return null;
					}
				default:
					goto error;
			}


		error:
			throw new CodeEE(GameMessages.T("An error occurred while parsing the html string \"") + st.RowString + GameMessages.T("\""));
		}

		private static int stringToColorInt32(string str)
		{
			if(str.Length == 0)
				throw new CodeEE(GameMessages.T("A color word or #RRGGBB value is required"));
			int i = 0;
			if (str[0] == '#')
			{
				string colorvalue = str.Substring(1);
				try
				{
					i = Convert.ToInt32(colorvalue, 16);
					if (i < 0 || i > 0xFFFFFF)
						throw new CodeEE(colorvalue + GameMessages.T(" is outside the valid color specification range"));
				}
				catch
				{
					throw new CodeEE(colorvalue + GameMessages.T(" cannot be interpreted as a number"));
				}
			}
			else
			{
				Color color = Color.FromName(str);
				if (color.A == 0)//failed to interpret as a color name. Error confirmed
				{
					if(str.Equals("transparent", StringComparison.OrdinalIgnoreCase))
						throw new CodeEE(GameMessages.T("Transparent cannot be specified as a color"));
					try
					{
						i = Convert.ToInt32(str, 16);
					}
					catch//not even hexadecimal
					{
						throw new CodeEE(GameMessages.T("The specified color name \"") + str + GameMessages.T("\" is not a valid color name"));
					}
					//maybe they intended #RRGGBB
					throw new CodeEE(GameMessages.T("The specified color name \"") + str + GameMessages.T("\" is not a valid color name (when specifying a color in hexadecimal, put # before the number)"));
				}
				i = color.R * 0x10000 + color.G * 0x100 + color.B;
			}
			return i;
		}

		/// <summary>
		/// Splits an HTML string at a given half-width-character-equivalent width boundary.
		/// Returns [before, after] where before fits within length and after is the remainder.
		/// </summary>
		public static string[] HtmlSubString(string str, int length)
		{
			Stack<string[]> beginStack = new Stack<string[]>();
			Stack<string[]> endStack = new Stack<string[]>();

			length = length * Config.FontSize / 2;
			str = Unescape(str);
			int found = -1, last = 0, delbr = 0;
			bool content = false;
			while (true)
			{
				string tstr;
				int tmp;
				found = str.IndexOf('<', last);
				if (found != last)
				{
					string pref = "", suff = "";
					foreach (string[] s in beginStack)
						if (s[1] == "style") pref += s[0];
					Stack<string[]> arr = new Stack<string[]>(endStack);
					while (arr.Count > 0)
						if (arr.Peek()[1] == "style") suff += arr.Pop()[0];
						else arr.Pop();
					if (found < 0)
						tstr = str.Substring(last, str.Length - last);
					else
						tstr = str.Substring(last, found - last);
					tmp = GetSubStr(pref, suff, tstr, ref length);
					last += tmp + 1;
					content = true;
					if (found < 0 || tmp < tstr.Length) break;
				}
				else last++;
				found = str.IndexOf('>', last);
				if (found <= 0) break;
				if (str[last] == '/')
				{
					if (beginStack.Count > 0) beginStack.Pop();
					if (endStack.Count > 0) endStack.Pop();
				}
				else
				{
					int fspace = str.IndexOf(' ', last, found - last);
					if (fspace < 0) fspace = found;
					string tag = str.Substring(last, fspace - last);
					if (tag == "br") { delbr = 1; break; }
					if (tag == "img" || tag == "shape")
					{
						var pos = last - 1;
						tstr = str.Substring(pos, found - pos + 1);
						tmp = HtmlLength(tstr);
						length -= tmp;
						if (length < 0 && content) break;
					}
					else
					{
						bool ist = tag == "b" || tag == "i" || tag == "s";
						beginStack.Push(new string[] { string.Concat("<", str.Substring(last, found - last), ">"), ist ? "style" : "" });
						endStack.Push(new string[] { "</" + tag + ">", ist ? "style" : "" });
					}
				}
				last = found + 1;
			}
			string[] ret = new string[2];
			ret[0] = "";
			ret[1] = "";
			if (last == 0) return new string[] { "", str };
			ret[0] = str.Substring(0, last - 1);
			while (endStack.Count > 0) ret[0] += endStack.Pop()[0];
			while (beginStack.Count > 0) ret[1] = beginStack.Pop()[0] + ret[1];
			ret[1] += str.Substring(last - 1 + delbr * 4, str.Length - last + 1 - delbr * 4);
			return ret;
		}

		/// <summary>
		/// Measures how many characters fit within the remaining pixel budget.
		/// Full-width chars (> U+007F) cost 2 units; half-width cost 1.
		/// Returns the number of characters consumed.
		/// </summary>
		private static int GetSubStr(string pref, string suff, string text, ref int length)
		{
			int used = 0;
			for (int i = 0; i < text.Length; i++)
			{
				int cw = text[i] > 0x7F ? 2 : 1;
				if (length < cw && used > 0) break;
				length -= cw;
				used++;
				if (length <= 0) break;
			}
			return used;
		}

		/// <summary>
		/// Estimates the pixel width of an img or shape tag (stub: 2 × FontSize).
		/// </summary>
		internal static int HtmlLength(string htmlTag)
		{
			return Config.FontSize * 2;
		}

	}
}
