using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Function
{
	internal abstract class FunctionMethod
	{
		public Type ReturnType { get; protected set; }
		protected Type[] argumentTypeArray;
		protected string Name { get; private set; }

		//argumentの数-typeが一致doかどうかのテスト
		//正しくnotcaseはErrormessageを返す.
		//argumentの数が不定でexistcaseやargumentの省略を許すcaseにはoverridedothis.
		public virtual string CheckArgumentType(string name, IOperandTerm[] arguments)
		{
			if (arguments.Length != argumentTypeArray.Length)
				return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);
			for (int i = 0; i < argumentTypeArray.Length; i++)
			{
				if (arguments[i] == null)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i+1);
				if (argumentTypeArray[i] != arguments[i].GetOperandType())
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
			}
			return null;
		}
		
		//Argumentがall定数のwhenにMethodを解体してよいかどうか.RANDやCharaを参照dothingetcは不可
		public bool CanRestructure { get; protected set; }

		//FunctionMethodが固有のRestructure()を持つかどうか
		public bool HasUniqueRestructure { get; protected set; }

		//実際のcalculate.
		public virtual Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) { throw new ExeEE("戻りvalueのtypeがdifferent or 未実装"); }
		public virtual string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments) { throw new ExeEE("戻りvalueのtypeがdifferent or 未実装"); }
		public virtual SingleTerm GetReturnValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			if (ReturnType == typeof(Int64))
				return new SingleTerm(GetIntValue(exm, arguments));
			else
				return new SingleTerm(GetStrValue(exm, arguments));
		}

		/// <summary>
		/// 戻りvalueは全体をRestructurecanかどうか
		/// </summary>
		/// <param name="exm"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public virtual bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
		{ throw new ExeEE("未実装？"); }


		internal void SetMethodName(string name)
		{
			Name = name;
		}
	}
}
