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

		//test whether the number and types of arguments match
		//if incorrect, returns an Error message.
		//override when the number of arguments is indefinite or when argument omission is allowed.
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
		
		//whether the Method may be dismantled when all Arguments are constants. Not allowed for those referencing RAND or Chara etc.
		public bool CanRestructure { get; protected set; }

		//whether FunctionMethod has its own Restructure()
		public bool HasUniqueRestructure { get; protected set; }

		//actual calculation.
		public virtual Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) { throw new ExeEE(GameMessages.T("Return type mismatch or not implemented")); }
		public virtual string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments) { throw new ExeEE(GameMessages.T("Return type mismatch or not implemented")); }
		public virtual SingleTerm GetReturnValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			if (ReturnType == typeof(Int64))
				return new SingleTerm(GetIntValue(exm, arguments));
			else
				return new SingleTerm(GetStrValue(exm, arguments));
		}

		/// <summary>
		/// Whether the whole can be Restructured for the return value
		/// </summary>
		/// <param name="exm"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public virtual bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
		{ throw new ExeEE(GameMessages.T("Not implemented?")); }


		internal void SetMethodName(string name)
		{
			Name = name;
		}
	}
}
