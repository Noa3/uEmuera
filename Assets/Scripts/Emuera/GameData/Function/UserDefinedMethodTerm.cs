using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Function
{
	internal abstract class SuperUserDefinedMethodTerm : IOperandTerm
	{
		protected SuperUserDefinedMethodTerm(Type returnType)
			: base(returnType)
		{
		}
		public abstract UserDefinedFunctionArgument Argument { get;}
		public abstract CalledFunction Call { get; }
		public override long GetIntValue(ExpressionMediator exm)
		{
			SingleTerm term = exm.Process.GetValue(this);
			if (term == null)
				return 0;
			return term.Int;
		}
		public override string GetStrValue(ExpressionMediator exm)
		{
			SingleTerm term = exm.Process.GetValue(this);
			if (term == null)
				return "";
			return term.Str;
		}
		public override SingleTerm GetValue(ExpressionMediator exm)
		{
			SingleTerm term = exm.Process.GetValue(this);
			if (term == null)
			{
				if (GetOperandType() == typeof(Int64))
					return new SingleTerm(0);
				else
					return new SingleTerm("");
			}
			return term;
		}
	}

	/// <summary>
	/// Placeholder term for an in-expression call to a user #FUNCTION that was not
	/// loaded/sorted yet when the expression was parsed (progressive loading).
	/// Resolves lazily at runtime: if the background loader is still running it waits
	/// for the file containing the function, then behaves like UserDefinedMethodTerm.
	/// </summary>
	internal sealed class PendingUserDefinedMethodTerm : IOperandTerm
	{
		private readonly string name;
		private readonly IOperandTerm[] srcArgs;
		private SuperUserDefinedMethodTerm resolved = null;
		private bool warned = false;

		public PendingUserDefinedMethodTerm(string name, IOperandTerm[] srcArgs)
			// Phase 6 #8 fix: use the FunctionCatalog to determine the correct return
			// type instead of always defaulting to typeof(Int64). A #FUNCTIONS function
			// that is referenced before its body loads used to be incorrectly typed as
			// integer, producing wrong expression type-checking results.
			: base(GetCorrectReturnType(name))
		{
			this.name = name;
			this.srcArgs = srcArgs;
		}

		/// <summary>
		/// Resolves the return type from FunctionCatalog if available, or falls back
		/// to typeof(Int64) (matches the original behaviour as last resort).
		/// </summary>
		static Type GetCorrectReturnType(string name)
		{
			var catalog = GameProc.FunctionCatalog.Instance;
			if (catalog != null && catalog.IsReady)
			{
				Type t = catalog.GetClrReturnType(name);
				if (t != typeof(void))
					return t;
			}
			// Fallback: default to Int64 (matches original behaviour).
			return typeof(Int64);
		}

		public override long GetIntValue(ExpressionMediator exm)
		{
			SuperUserDefinedMethodTerm t = ResolveMethod();
			if (t == null)
				return 0;
			return t.GetIntValue(exm);
		}
		public override string GetStrValue(ExpressionMediator exm)
		{
			SuperUserDefinedMethodTerm t = ResolveMethod();
			if (t == null)
				return "";
			return t.GetStrValue(exm);
		}
		public override SingleTerm GetValue(ExpressionMediator exm)
		{
			SuperUserDefinedMethodTerm t = ResolveMethod();
			if (t == null)
			{
				if (GetOperandType() == typeof(Int64))
					return new SingleTerm(0);
				else
					return new SingleTerm("");
			}
			return t.GetValue(exm);
		}

		private SuperUserDefinedMethodTerm ResolveMethod()
		{
			if (resolved != null)
				return resolved;
			FunctionLabelLine func = FunctionResolver.ResolveNormalLabel(GlobalStatic.LabelDictionary, name);
			if (func != null && func.IsMethod)
			{
				string errMes;
				UserDefinedMethodTerm term = UserDefinedMethodTerm.Create(func, srcArgs, out errMes);
				if (term != null)
				{
					resolved = term;
					return resolved;
				}
			}
			// Function is definitely missing: nothing is loading anymore and the
			// function still does not exist (or exists but has no #FUNCTION).
			if (!warned && !ErbOnDemand.AnythingLoading())
			{
				warned = true;
				ScriptPosition pos = GlobalStatic.Process.GetScaningLine()?.Position ?? new ScriptPosition();
				ParserMediator.Warn(string.Format(GameMessages.UnrecognizedIdentifier, name), pos, 1);
			}
			return null;
		}
	}

	internal sealed class UserDefinedMethodTerm : SuperUserDefinedMethodTerm
	{
		
		/// <summary>
		/// Returns null if there is an error.
		/// </summary>
		public static UserDefinedMethodTerm Create(FunctionLabelLine targetLabel, IOperandTerm[] srcArgs, out string errMes)
		{
			CalledFunction call = CalledFunction.CreateCalledFunctionMethod(targetLabel, targetLabel.LabelName);
			UserDefinedFunctionArgument arg = call.ConvertArg(srcArgs, out errMes);
			if (arg == null)
				return null;
			return new UserDefinedMethodTerm(arg, call.TopLabel.MethodType, call);
		}

		private UserDefinedMethodTerm(UserDefinedFunctionArgument arg, Type returnType, CalledFunction call)
			: base(returnType)
		{
			argment = arg;
			called = call;
		}
		public override UserDefinedFunctionArgument Argument { get { return argment; } }
		public override CalledFunction Call { get { return called; } }
		private readonly UserDefinedFunctionArgument argment;
		private readonly CalledFunction called;

		public override IOperandTerm Restructure(ExpressionMediator exm)
		{
			Argument.Restructure(exm);
			return this;
		}


		
	}
	internal sealed class UserDefinedRefMethodTerm : SuperUserDefinedMethodTerm
	{
		public UserDefinedRefMethodTerm(UserDefinedRefMethod reffunc, IOperandTerm[] srcArgs)
			: base(reffunc.RetType)
		{
			this.srcArgs = srcArgs;
			this.reffunc = reffunc;
		}
		IOperandTerm[] srcArgs = null;
		readonly UserDefinedRefMethod reffunc = null;
		public override UserDefinedFunctionArgument Argument
		{
			get
			{
				if (reffunc.CalledFunction == null)
					throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which does not reference anything"));
				string errMes;
				UserDefinedFunctionArgument arg = reffunc.CalledFunction.ConvertArg(srcArgs, out errMes);
				if (arg == null)
					throw new CodeEE(errMes);
				return arg;
			}
		}
		public override CalledFunction Call
		{
			get
			{
				if (reffunc.CalledFunction == null)
					throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc .Name+ GameMessages.T(" which does not reference anything"));
				return reffunc.CalledFunction;
			}
		}

		public override IOperandTerm Restructure(ExpressionMediator exm)
		{
			for (int i = 0; i < srcArgs.Length; i++)
			{
				if ((reffunc.ArgTypeList[i] & UserDifinedFunctionDataArgType.__Ref) == UserDifinedFunctionDataArgType.__Ref)
					srcArgs[i].Restructure(exm);
				else
					srcArgs[i] = srcArgs[i].Restructure(exm);
			}
			return this;
		}


	}

	internal sealed class UserDefinedRefMethodNoArgTerm : SuperUserDefinedMethodTerm
	{
		public UserDefinedRefMethodNoArgTerm(UserDefinedRefMethod reffunc)
			: base(reffunc.RetType)
		{
			this.reffunc = reffunc;
		}
		readonly UserDefinedRefMethod reffunc = null;
		public override UserDefinedFunctionArgument Argument
		{ get { throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which has no arguments")); } }
		public override CalledFunction Call
		{ get { throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which has no arguments")); } }
		public string GetRefName()
		{
			if (reffunc.CalledFunction == null)
				return "";
			return reffunc.CalledFunction.TopLabel.LabelName;
		}
		public override long GetIntValue(ExpressionMediator exm)
		{ throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which has no arguments")); }
		public override string GetStrValue(ExpressionMediator exm)
		{ throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which has no arguments")); }
		public override SingleTerm GetValue(ExpressionMediator exm)
		{ throw new CodeEE(GameMessages.T("Called the function reference ") + reffunc.Name + GameMessages.T(" which has no arguments")); }
		public override IOperandTerm Restructure(ExpressionMediator exm)
		{
			return this;
		}
	}
}
