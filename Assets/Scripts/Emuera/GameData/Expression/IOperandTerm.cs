using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Expression
{
	internal abstract class IOperandTerm
	{
        public IOperandTerm(Type t)
        {
            type = t;
        }
		public Type GetOperandType()
        {
            return type;
        }

        public virtual Int64 GetIntValue(ExpressionMediator exm)
        {
            return 0;
        }
        public virtual string GetStrValue(ExpressionMediator exm)
        {
            return "";
        }
        public virtual SingleTerm GetValue(ExpressionMediator exm)
        {
            if (type == typeof(Int64))
                return new SingleTerm(0);
            else
                return new SingleTerm("");
        }
        public bool IsInteger
        {
            get { return type == typeof(Int64); }
        }
        public bool IsString
        {
            get { return type == typeof(string); }
        }
        readonly Type type;
        
		/// <summary>
		/// 定数を解体してpossibleならSingleTerm化do
		/// defineの都合above,2回以above呼ばれるpossible性がexist
		/// </summary>
        public virtual IOperandTerm Restructure(ExpressionMediator exm)
        {
			return this;
        }
	}
}
