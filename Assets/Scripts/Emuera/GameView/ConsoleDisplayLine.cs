using System;
using System.Collections.Generic;
using System.Text;
//using System.Drawing;
using MinorShift._Library;
//using System.Windows.Forms;
using uEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude=false)]
	internal enum DisplayLineLastState
	{
		None = 0,
		Normal = 1,
		Selected = 2,
		BackLog = 3,
	}
	
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude=false)]
	internal enum DisplayLineAlignment
	{
		LEFT = 0,
		CENTER = 1,
		RIGHT = 2,
	}
	/// <summary>
	/// A display line. consists of one or more buttons (ConsoleButtonString).
	/// </summary>
	internal sealed class ConsoleDisplayLine
	{
		//public ConsoleDisplayLine(EmueraConsole parentWindow, ConsoleButtonString[] buttons, bool isLogical, bool temporary)
		public ConsoleDisplayLine(ConsoleButtonString[] buttons, bool isLogical, bool temporary)
		{
			//parent = parentWindow;
			if (buttons == null)
			{
				buttons = new ConsoleButtonString[0];
				return;
			}
			this.buttons = buttons;
			for(var i=0; i<buttons.Length; ++i)
                buttons[i].ParentLine = this;
			IsLogicalLine = isLogical;
			IsTemporary = temporary;
		}
		public int LineNo = -1;
		
		///true only when it is the first logical line; from the 2nd line onward, split for display purposes, it is false
		readonly public bool IsLogicalLine = true;
		readonly public bool IsTemporary = false;
		//EmueraConsole parent;
		ConsoleButtonString[] buttons;
		DisplayLineAlignment align;
		public ConsoleButtonString[] Buttons{get{return buttons;}}
		public DisplayLineAlignment Align{get{return align;}}
		bool aligned = false;
		public void SetAlignment(DisplayLineAlignment align)
		{
			if (aligned)
				return;
			this.aligned = true;
			this.align = align;
			if (buttons.Length == 0)
				return;
			//DisplayLine width
			int width = 0;
            for(var i = 0; i < buttons.Length; ++i)
                width += buttons[i].Width;
			//current position
			int pointX = buttons[0].PointX;

			//target position
			int movetoX = 0;
			if (align == DisplayLineAlignment.LEFT)
			{
				//supports fixed positions
				if (IsLogicalLine)
					return;
				movetoX = 0;
			}
			else if (align == DisplayLineAlignment.CENTER)
				movetoX = Config.WindowX / 2 - width / 2;
			else if (align == DisplayLineAlignment.RIGHT)
				movetoX = Config.WindowX - width;

			//movement distance
			int shiftX = movetoX - pointX;
			if(shiftX != 0)
				this.ShiftPositionX(shiftX);
		}

		public void ShiftPositionX(int shiftX)
		{
            for(var i = 0; i < buttons.Length; ++i)
                buttons[i].ShiftPositionX(shiftX);
		}

		public void ChangeStr(ConsoleButtonString[] newButtons)
        {
            buttons = null;
            for(var i = 0; i < newButtons.Length; ++i)
                newButtons[i].ParentLine = this;
			buttons = newButtons;
        }

		public void Clear(Brush brush, Graphics graph, int pointY)
		{
            //Rectangle rect = new Rectangle(0, pointY, Config.WindowX, Config.LineHeight);
            ////graph.FillRectangle(brush, rect);
            ////TODO clear
            //graph.Clear();
		}

		//public ConsoleButtonString GetPointingButton(int pointX)
		//{
		//	////1815 reverse the priority
		//	////so that buttons drawn later are given priority
		//	for (int i = 0; i < buttons.Length; i++)
		//	{
		//		ConsoleButtonString button = buttons[buttons.Length - i - 1];
		//		if ((button.PointX <= pointX) && (button.PointX + button.Width >= pointX))
		//			return button;
		//	}
		//	//foreach (ConsoleButtonString button in buttons)
		//	//{
		//	//    if ((button.PointX <= pointX) && (button.PointX + button.Width >= pointX))
		//	//        return button;
		//	//}
		//	return null;
		//}

		public void DrawTo(Graphics graph, int pointY, bool isBackLog, bool force, TextDrawingMode mode)
		{
            //foreach (ConsoleButtonString button in buttons)
            //    button.DrawTo(graph, pointY, isBackLog, mode);
		}
		
		public void GDIDrawTo(int pointY, bool isBackLog)
		{
			//foreach (ConsoleButtonString button in buttons)
			//	button.GDIDrawTo(pointY, isBackLog);
			//1819 since everything is cleared every time, the gap-filling process is no longer needed
			//int pointX = 0;
			//foreach (ConsoleButtonString button in buttons)
			//{
			//	if (button.Width == 0)
			//		continue;
			//	if (pointX < button.PointX)
			//	{
			//		Rectangle rect = new Rectangle(pointX, pointY, button.PointX - pointX, Config.LineHeight);
			//		GDI.FillRectBGColor(rect);
			//	}
			//	button.GDIDrawTo(pointY, isBackLog);
			//	//fill-in process to prevent gaps when the actual font height < line spacing
			//	GDI.FillGap(Config.LineHeight, button.Width + (button.PointX - pointX), new Point(pointX, pointY));
			//	pointX = button.PointX + button.Width;
			//}
			//if (pointX < Config.WindowX)
			//{
			//	Rectangle rect = new Rectangle(pointX, pointY, Config.WindowX - pointX, Config.LineHeight);
			//	GDI.FillRectBGColor(rect);
			//}
		}
		
		public override string ToString()
		{
			if (buttons == null)
				return "";
			StringBuilder builder = new StringBuilder();
			for(var i=0; i<buttons.Length; ++i)
				builder.Append(buttons[i].ToString());
			return builder.ToString();
		}
	}
}
