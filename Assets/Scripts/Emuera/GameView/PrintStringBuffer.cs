using System;
using System.Collections.Generic;
using System.Text;
//using System.Drawing;
//using System.Windows.Forms;
using MinorShift.Emuera.Sub;
using uEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{
	/*
	 * ConsoleStyledString = string + StringStyle
	 * ConsoleButtonString = (ConsoleStyledString) * n + ButtonValue
	 * ConsoleDisplayLine = (ConsoleButtonString) * n
	 * PrintStringBuffer creates ConsoleDisplayLines from ERB PRINT commands
	*/

	/// <summary>
	/// Class that accumulates PRINT commands and finally resolves them
	/// </summary>
	internal sealed class PrintStringBuffer
	{
		public PrintStringBuffer(EmueraConsole parent)
		{
			this.parent = parent;
		}
		readonly EmueraConsole parent;
		StringBuilder builder = new StringBuilder();
		List<AConsoleDisplayPart> m_stringList = new List<AConsoleDisplayPart>();
		StringStyle lastStringStyle = new StringStyle();
		List<ConsoleButtonString> m_buttonList = new List<ConsoleButtonString>();

		public int BufferStrLength
		{
			get
			{
				int length = 0;

                var count = m_stringList.Count;
                AConsoleDisplayPart css = null;
                for(var i=0; i<count; ++i)
				{
                    css = m_stringList[i];
					if (css is ConsoleStyledString)
						length += css.Str.Length;
					else
						length += 1;
				}
				return length;
			}
		}

		public void Append(AConsoleDisplayPart part)
		{
			if (builder.Length != 0)
			{
				m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
				builder.Remove(0, builder.Length);
			}
			m_stringList.Add(part);
		}

		public void Append(string str, StringStyle style)
		{
			Append(str, style, false);
		}

		public void Append(string str, StringStyle style, bool force_button)
		{
			if (BufferStrLength > 2000)
				return;
			if (force_button)
				fromCssToButton();
			if ((builder.Length == 0) || (lastStringStyle == style))
			{
				if (builder.Length > 2000)
					return;
				if (builder.Length + str.Length > 2000)
					str = str.Substring(0, 2000 - builder.Length) + GameMessages.T("※※※Buffer exceeds 2000 characters (1000 full-width). The rest cannot be displayed.※※※");
				builder.Append(str);
				lastStringStyle = style;
			}
			else
			{
				m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
				builder.Remove(0, builder.Length);
				builder.Append(str);
				lastStringStyle = style;
			}
			if (force_button)
				fromCssToButton();
		}

		public void AppendButton(string str, StringStyle style, string input)
		{
			fromCssToButton();
			m_stringList.Add(new ConsoleStyledString(str, style));
			if (m_stringList.Count == 0)
				return;
			m_buttonList.Add(createButton(m_stringList, input));
			m_stringList.Clear();
		}



		public void AppendButton(string str, StringStyle style, long input)
		{
			fromCssToButton();
			m_stringList.Add(new ConsoleStyledString(str, style));
			if (m_stringList.Count == 0)
				return;
			m_buttonList.Add(createButton(m_stringList, input));
			m_stringList.Clear();
		}

		public void AppendPlainText(string str, StringStyle style)
		{
			fromCssToButton();
			m_stringList.Add(new ConsoleStyledString(str, style));
			if (m_stringList.Count == 0)
				return;
			m_buttonList.Add(createPlainButton(m_stringList));
			m_stringList.Clear();
		}

		public bool IsEmpty
		{
			get
			{
				return ((m_buttonList.Count == 0) && (builder.Length == 0) && (m_stringList.Count == 0));
			}
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			foreach (ConsoleButtonString button in m_buttonList)
				buf.Append(button.ToString());
			foreach (AConsoleDisplayPart css in m_stringList)
				buf.Append(css.Str);
			buf.Append(builder);
			return buf.ToString();
		}

		public ConsoleDisplayLine AppendAndFlushErrButton(string str, StringStyle style, string input, ScriptPosition pos, StringMeasure sm)
		{
			fromCssToButton();
			m_stringList.Add(new ConsoleStyledString(str, style));
			if (m_stringList.Count == 0)
				return null;
			m_buttonList.Add(createButton(m_stringList, input, pos));
			m_stringList.Clear();
			return FlushSingleLine(sm, false);
		}

		public ConsoleDisplayLine FlushSingleLine(StringMeasure stringMeasure, bool temporary)
		{
			fromCssToButton();
			setWidthToButtonList(m_buttonList, stringMeasure, true);
			ConsoleButtonString[] dispLineButtonArray = new ConsoleButtonString[m_buttonList.Count];
			m_buttonList.CopyTo(dispLineButtonArray);
			ConsoleDisplayLine line = new ConsoleDisplayLine(dispLineButtonArray, true, temporary);
			this.clearBuffer();
			return line;
		}

		public ConsoleDisplayLine[] Flush(StringMeasure stringMeasure, bool temporary)
		{
			fromCssToButton();
			ConsoleDisplayLine[] ret = PrintStringBuffer.ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
			this.clearBuffer();
			return ret;
		}

		private static ConsoleDisplayLine m_buttonsToDisplayLine(List<ConsoleButtonString> lineButtonList, bool firstLine, bool temporary)
		{
			ConsoleButtonString[] dispLineButtonArray = new ConsoleButtonString[lineButtonList.Count];
			lineButtonList.CopyTo(dispLineButtonArray);
			lineButtonList.Clear();
			return new ConsoleDisplayLine(dispLineButtonArray, firstLine, temporary);
		}

		public static ConsoleDisplayLine[] ButtonsToDisplayLines(List<ConsoleButtonString> buttonList, StringMeasure stringMeasure, bool nobr, bool temporary)
		{
			if (buttonList.Count == 0)
				return new ConsoleDisplayLine[0];
			setWidthToButtonList(buttonList, stringMeasure, nobr);
			List<ConsoleDisplayLine> lineList = new List<ConsoleDisplayLine>();
			List<ConsoleButtonString> lineButtonList = new List<ConsoleButtonString>();
			int windowWidth = Config.DrawableWidth;
			bool firstLine = true;
			for (int i = 0; i < buttonList.Count; i++)
			{
				if (buttonList[i] == null)
				{//forced line-break flag
					lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
					firstLine = false;
					buttonList.RemoveAt(i);
					i--;
					continue;
				}
				if (nobr || ((buttonList[i].PointX + buttonList[i].Width <= windowWidth)))
				{//no-break mode, or it fits in the drawable area, so it can be left as is
					lineButtonList.Add(buttonList[i]);
					continue;
				}
				//create a new display line

				//split the button?
				//if "do not wrap a line in the middle of a button" is false, split it
				//this button alone exceeds the drawable area, so a split is mandatory
				//non-clickable buttons are split too. However, if "reproduce pre-ver1739 non-button wrapping" is set, clickability is not distinguished
				if ((!Config.ButtonWrap) || (lineButtonList.Count == 0) || (!buttonList[i].IsButton && !Config.CompatiLinefeedAs1739))
				{//split the button
					int divIndex = getDivideIndex(buttonList[i], stringMeasure);
					if (divIndex > 0)
					{
						ConsoleButtonString newButton = buttonList[i].DivideAt(divIndex, stringMeasure);
						//newButton.CalcPointX(buttonList[i].PointX + buttonList[i].Width);
						buttonList.Insert(i + 1, newButton);
						lineButtonList.Add(buttonList[i]);
						i++;
					}
					else if (divIndex == 0 && (lineButtonList.Count > 0))
					{//send the whole thing to the next line
					}
					else//a button made only of non-divisible elements cannot be split
					{
						lineButtonList.Add(buttonList[i]);
						continue;
					}
				}
				lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
				firstLine = false;
				//position adjustment
//				shiftX = buttonList[i].PointX;
				int pointX = 0;
				for (int j = i; j < buttonList.Count; j++)
				{
					if (buttonList[j] == null)//no adjustment needed after a forced line break
						break;
					buttonList[j].CalcPointX(pointX);
					pointX += buttonList[j].Width;
				}
				i--;//buttonList[i] is not included in the new line, so it must be reconsidered for the next line (offset by the following i++)
			}
			if (lineButtonList.Count > 0)
			{
				lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
			}
			ConsoleDisplayLine[] ret = new ConsoleDisplayLine[lineList.Count];
			lineList.CopyTo(ret);
			return ret;
		}

		/// <summary>
		/// New in 1810beta003: performs Append and Flush together for markup
		/// </summary>
		/// <param name="str"></param>
		/// <param name="stringMeasure"></param>
		/// <returns></returns>
		public ConsoleDisplayLine[] PrintHtml(string str, StringMeasure stringMeasure)
		{
			if (string.IsNullOrEmpty(str))
				return Array.Empty<ConsoleDisplayLine>();

			// This legacy helper is still part of the buffer API. Route markup
			// through the same authoritative parser used by EmueraConsole.PrintHtml
			// instead of leaving a player-reachable NotImplementedException.
			ConsoleDisplayLine[] buffered = Flush(stringMeasure, false);
			ConsoleDisplayLine[] html = HtmlManager.Html2DisplayLine(str, stringMeasure, parent);
			if (buffered.Length == 0)
				return html;
			if (html.Length == 0)
				return buffered;
			var result = new ConsoleDisplayLine[buffered.Length + html.Length];
			Array.Copy(buffered, 0, result, 0, buffered.Length);
			Array.Copy(html, 0, result, buffered.Length, html.Length);
			return result;
		}

		#region Private methods for Flush

		private void clearBuffer()
		{
			builder.Remove(0, builder.Length);
			m_stringList.Clear();
			m_buttonList.Clear();
		}

		/// <summary>
		/// Convert cssList into buttons and add them to buttonList.
		/// Width and other properties are not considered at this point.
		/// </summary>
		private void fromCssToButton()
		{
			if (builder.Length != 0)
			{
				m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
				builder.Remove(0, builder.Length);
			}
			if (m_stringList.Count == 0)
				return;
			m_buttonList.AddRange(createButtons(m_stringList));
			m_stringList.Clear();
		}

		/// <summary>
		/// Convert a physical line into a single button.
		/// </summary>
		/// <returns></returns>
		private ConsoleButtonString createButton(List<AConsoleDisplayPart> cssList, string input)
		{
			AConsoleDisplayPart[] cssArray = new AConsoleDisplayPart[cssList.Count];
			cssList.CopyTo(cssArray);
			cssList.Clear();
			return new ConsoleButtonString(parent, cssArray, input);
		}
		private ConsoleButtonString createButton(List<AConsoleDisplayPart> cssList, string input, ScriptPosition pos)
		{
			AConsoleDisplayPart[] cssArray = new AConsoleDisplayPart[cssList.Count];
			cssList.CopyTo(cssArray);
			cssList.Clear();
			return new ConsoleButtonString(parent, cssArray, input, pos);
		}
		private ConsoleButtonString createButton(List<AConsoleDisplayPart> cssList, long input)
		{
			AConsoleDisplayPart[] cssArray = new AConsoleDisplayPart[cssList.Count];
			cssList.CopyTo(cssArray);
			cssList.Clear();
			return new ConsoleButtonString(parent, cssArray, input);
		}
		private ConsoleButtonString createPlainButton(List<AConsoleDisplayPart> cssList)
		{
			AConsoleDisplayPart[] cssArray = new AConsoleDisplayPart[cssList.Count];
			cssList.CopyTo(cssArray);
			cssList.Clear();
			return new ConsoleButtonString(parent, cssArray);
		}

		/// <summary>
		/// Split a physical line into button units. The contents of the argument cssList may be modified.
		/// </summary>
		/// <returns></returns>
		private ConsoleButtonString[] createButtons(List<AConsoleDisplayPart> cssList)
		{
			StringBuilder buf = new StringBuilder();
			for (int i = 0; i < cssList.Count; i++)
			{
				buf.Append(cssList[i].Str);
			}
			List<ButtonPrimitive> bpList = ButtonStringCreator.SplitButton(buf.ToString());
			ConsoleButtonString[] ret = new ConsoleButtonString[bpList.Count];
			AConsoleDisplayPart[] cssArray = null;
			if (ret.Length == 1)
			{
				cssArray = new AConsoleDisplayPart[cssList.Count];
				cssList.CopyTo(cssArray);
				if (bpList[0].CanSelect)
					ret[0] = new ConsoleButtonString(parent, cssArray, bpList[0].Input);
				else
					ret[0] = new ConsoleButtonString(parent, cssArray);
				return ret;
			}
			int cssStartCharIndex = 0;
			int buttonEndCharIndex = 0;
			int cssIndex = 0;
			List<AConsoleDisplayPart> buttonCssList = new List<AConsoleDisplayPart>();
			for (int i = 0; i < ret.Length; i++)
			{
				ButtonPrimitive bp = bpList[i];
				buttonEndCharIndex += bp.Str.Length;
				while (true)
				{
					if (cssIndex >= cssList.Count)
						break;
					AConsoleDisplayPart css = cssList[cssIndex];
					if (cssStartCharIndex + css.Str.Length >= buttonEndCharIndex)
					{//button end found
						int used = buttonEndCharIndex - cssStartCharIndex;
						if (used > 0 && css.CanDivide)
						{//a button boundary falls in the middle of a css boundary
							
							ConsoleStyledString newCss = ((ConsoleStyledString)css).DivideAt(used);
							if (newCss != null)
							{
								cssList.Insert(cssIndex + 1, newCss);
								newCss.PointX = css.PointX + css.Width;
							}
						}
						buttonCssList.Add(css);
						cssStartCharIndex += css.Str.Length;
						cssIndex++;
						break;
					}
					//the button end is still further ahead.
					buttonCssList.Add(css);
					cssStartCharIndex += css.Str.Length;
					cssIndex++;
				}
				cssArray = new AConsoleDisplayPart[buttonCssList.Count];
				buttonCssList.CopyTo(cssArray);
				if (bp.CanSelect)
					ret[i] = new ConsoleButtonString(parent, cssArray, bp.Input);
				else
					ret[i] = new ConsoleButtonString(parent, cssArray);
				buttonCssList.Clear();
			}
			return ret;

		}


		//add PointX and Width to stringList
		private static void setWidthToButtonList(List<ConsoleButtonString> buttonList, StringMeasure stringMeasure, bool nobr)
		{
			int pointX = 0;
			//int count = buttonList.Count;
			//1.824 fix. setting the initial subpixel to 0.5f instead of 0 absorbs rounding of fractions
			float subPixel = 0.5f;
			for (int i = 0; i < buttonList.Count; i++)
			{
				ConsoleButtonString button = buttonList[i];
				if (button == null)
				{//line-break flag
					pointX = 0;
					continue;
				}
				button.CalcWidth(stringMeasure, subPixel);
				button.CalcPointX(pointX);
				pointX = button.PointX + button.Width;
				//What is this trying to do...
				if (button.PointXisLocked)
					subPixel = 0;
				//pointX += button.Width;
				subPixel = button.XsubPixel;
			}
			return;
			
			//1815 bug-prone, so commented out. Omitting the Width measurement is something to be done eventually
			////1815 new approach based on alignLeft and nobr
			////allows direct PointX specification and partially omits the Width measurement
			//ConsoleStyledString lastCss = null;
			//for (int i = 0; i < buttonList.Count; i++)
			//{
			//    ConsoleButtonString button = buttonList[i];
			//    if (button == null)
			//    {//line-break flag
			//        pointX = 0;
			//        lastCss = null;
			//        continue;
			//    }
			//    for (int j = 0; j < button.StrArray.Length; j++)
			//    {
			//        ConsoleStyledString css = button.StrArray[j];
			//        if (css.PointXisLocked)//position-locked flag
			//        {//if position-locked, omit the Width measurement of the previous css
			//            pointX = css.PointX;
			//            if (lastCss != null)
			//            {
			//                lastCss.Width = css.PointX - lastCss.PointX;
			//                if (lastCss.Width < 0)
			//                    lastCss.Width = 0;
			//            }
			//        }
			//        else
			//        {
			//            if (lastCss != null)
			//            {
			//                lastCss.SetWidth(stringMeasure);
			//                pointX += lastCss.Width;
			//            }
			//            css.PointX = pointX;
			//        }
			//    }
			//}
			////determine the position and width of ConsoleButtonString (needed to determine the clickable area)
			//for (int i = 0; i < buttonList.Count; i++)
			//{
			//    ConsoleButtonString button = buttonList[i];
			//    if (button == null || button.StrArray.Length == 0)
			//        continue;
			//    button.PointX = button.StrArray[0].PointX;
			//    lastCss = button.StrArray[button.StrArray.Length - 1];
			//    if (lastCss.Width >= 0)
			//        button.Width = lastCss.PointX - button.PointX + lastCss.Width;
			//    else if (i >= buttonList.Count - 1 || buttonList[i+1] == null || buttonList[i+1].StrArray.Length == 0)//end of line
			//        button.Width = Config.WindowX;//for the rightmost button, make the whole right side into a button area
			//    else
			//        button.Width = buttonList[i+1].StrArray[0].PointX - button.PointX;
			//    if (button.Width < 0)
			//        button.Width = 0;//depending on the pos specification, a button that cannot be clicked may result. oh well
			//}
		}

		private static int getDivideIndex(ConsoleButtonString button, StringMeasure sm)
		{
			AConsoleDisplayPart divCss = null;
			int pointX = button.PointX;
			int strLength = 0;
			int index = 0;

            int count = button.StrArray.Length;
            AConsoleDisplayPart css = null;
            for(var i=0; i<count; ++i)
			{
                css = button.StrArray[i];
				if (pointX + css.Width > Config.DrawableWidth)
				{
					if (index == 0 && !css.CanDivide)
						continue;
					divCss = css;
					break;
				}
				index++;
				strLength += css.Str.Length;
				pointX += css.Width;
			}
			if (divCss != null)
			{
				int cssDivIndex = getDivideIndex(divCss, sm);
				if (cssDivIndex > 0)
					strLength += cssDivIndex;
			}
			return strLength;
		}

		private static int getDivideIndex(AConsoleDisplayPart part, StringMeasure sm)
		{
			if (!part.CanDivide)
				return -1;
			ConsoleStyledString css = part as ConsoleStyledString;
			if (part == null)
				return -1;
			int widthLimit = Config.DrawableWidth - css.PointX;
			string str = css.Str;
			Font font = css.Font;
            int highLength = str.Length;//lowest char index exceeding widthLimit (char count - 1).
			int lowLength = 0;//largest char index that does not exceed.
			//int i = (int)(widthLimit / fontDisplaySize);//estimate the approximate number of chars
			//if (i > str.Length - 1)//so as not to reference outside the array.
			//	i = str.Length - 1;
			int i = lowLength;//estimate the approximate number of chars <- abandoned

			int point;
			string test = null;
			while ((highLength - lowLength) > 1)//repeat until the difference is one char or less.
			{
				test = str.Substring(0, i);
				point = sm.GetDisplayLength(test, font);
				if (point <= widthLimit)//if within the size, update lowLength. increase the char count.
				{
					lowLength = i;
					i++;
				}
				else//if outside the size, update highLength. decrease the char count.
				{
					highLength = i;
					i--;
				}
			}
			return lowLength;
		}
		#endregion

	}
}
