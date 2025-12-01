using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Function;
//using System.Windows.Forms;

namespace MinorShift.Emuera.GameData.Expression
{

	internal enum ArgsEndWith
	{
		None,
		EoL,
		RightParenthesis,//)終端
		RightBracket,//]終端
	}

	internal enum TermEndWith
	{
		None = 0x0000,
		EoL = 0x0001,
		Comma = 0x0002,//','終端
		RightParenthesis = 0x0004,//')'終端
		RightBracket = 0x0008,//')'終端
		Assignment = 0x0010,//')'終端

		RightParenthesis_Comma = RightParenthesis | Comma,//',' or ')'終端
		RightBracket_Comma = RightBracket | Comma,//',' or ']'終端
		Comma_Assignment = Comma | Assignment,//',' or '='終端
		RightParenthesis_Comma_Assignment = RightParenthesis | Comma | Assignment,//',' or ')' or '='終端
		RightBracket_Comma_Assignment = RightBracket | Comma | Assignment,//',' or ']' or '='終端
	}

    internal static class ExpressionParser
	{
		#region public Reduce
		/// <summary>
		/// カンマで区切られたargumentを一括して取得.
		/// returnwhenにはendWithのnext 文字がCurrentになっているはず.終端の適切さの検証はExpressionParserががlineう.
		/// calloriginalはCodeEEを適切にprocessdothis
		/// </summary>
		/// <returns></returns>
		public static IOperandTerm[] ReduceArguments(WordCollection wc, ArgsEndWith endWith, bool isDefine)
		{
			if(wc == null)
				throw new ExeEE("空のストリームを渡された");
			List<IOperandTerm> terms = new List<IOperandTerm>();
			TermEndWith termEndWith = TermEndWith.EoL;
			switch (endWith)
			{
				case ArgsEndWith.EoL:
					termEndWith = TermEndWith.Comma;
					break;
                //case ArgsEndWith.RightBracket:
                //    termEndWith = TermEndWith.RightBracket_Comma;
                //    break;
				case ArgsEndWith.RightParenthesis:
					termEndWith = TermEndWith.RightParenthesis_Comma;
					break;
			}
			TermEndWith termEndWith_Assignment = termEndWith | TermEndWith.Assignment;
			while (true)
			{
				Word word = wc.Current;
				switch (word.Type)
				{
					case '\0':
                        if (endWith == ArgsEndWith.RightBracket)
                            throw new CodeEE("'['に対応do']'not found");
						if (endWith == ArgsEndWith.RightParenthesis)
							throw new CodeEE("'('に対応do')'not found");
						goto end;
					case ')':
						if (endWith == ArgsEndWith.RightParenthesis)
						{
							wc.ShiftNext();
							goto end;
						}
						throw new CodeEE("構文parseduring 予期しnot')'を発見しました");
                    case ']':
                        if (endWith == ArgsEndWith.RightBracket)
                        {
                            wc.ShiftNext();
                            goto end;
                        }
                        throw new CodeEE("構文parseduring 予期しnot']'を発見しました");
				}
				if(!isDefine)
					terms.Add(ReduceExpressionTerm(wc, termEndWith));
				else
				{
					terms.Add(ReduceExpressionTerm(wc, termEndWith_Assignment));
                    if (terms[terms.Count - 1] == null)
                        throw new CodeEE("functiondefinitionのargumentは省略できません");
					if (wc.Current is OperatorWord)
					{//=がexist
						wc.ShiftNext();
						IOperandTerm term = reduceTerm(wc, false, termEndWith, VariableCode.__NULL__);
						if (term == null)
							throw new CodeEE("'='のafter 式がdoes not exist");
						if (term.GetOperandType() != terms[terms.Count - 1].GetOperandType())
							throw new CodeEE("'='の前afterでtypeが一致しません");
						terms.Add(term);
					}
					else
					{
						if (terms[terms.Count - 1].GetOperandType() == typeof(Int64))
							terms.Add(new NullTerm(0));
						else
							terms.Add(new NullTerm(""));
					}
				}
				if (wc.Current.Type == ',')
					wc.ShiftNext();
			}
		end:
            IOperandTerm[] ret = new IOperandTerm[terms.Count];
			terms.CopyTo(ret);
			return ret;
		}


		/// <summary>
		/// 数式orstring式.CALLのargumentetcを扱う.nullを返すthisがexist.
		/// returnwhenにはendWithの文字がCurrentになっているはず.終端の適切さの検証はcalloriginalがlineう.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
        public static IOperandTerm ReduceExpressionTerm(WordCollection wc, TermEndWith endWith)
        {
			IOperandTerm term = reduceTerm(wc, false, endWith, VariableCode.__NULL__);
            return term;
        }


		///// <summary>
		///// 単純string,書式付string,string式のうち,string式を取り扱う.
		///// 終端記号が正しいかどうかはcalloriginalで調べるthis
		///// </summary>
		///// <param name="st"></param>
		///// <returns></returns>
		//public static IOperandTerm ReduceStringTerm(WordCollection wc, TermEndWith endWith)
		//{
		//    IOperandTerm term = reduceTerm(wc, false, endWith, VariableCode.__NULL__);
		//    if (term.GetOperandType() != typeof(string))
		//        throw new CodeEE("式の結果がstringではdoes not exist");
		//    return term;
		//}

		public static IOperandTerm ReduceIntegerTerm(WordCollection wc, TermEndWith endwith)
		{
			IOperandTerm term = reduceTerm(wc, false, endwith, VariableCode.__NULL__);
            if (term == null)
                throw new CodeEE("構文を式as解釈できません");
			if (term.GetOperandType() != typeof(Int64))
				throw new CodeEE("式の結果がnumericではdoes not exist");
			return term;
		}

		
        /// <summary>
        /// 結果次第ではSingleTermを返すthisがexist.
        /// </summary>
        /// <returns></returns>
		public static IOperandTerm ToStrFormTerm(StrFormWord sfw)
		{
			StrForm strf = StrForm.FromWordToken(sfw);
			if(strf.IsConst)
				return new SingleTerm(strf.GetString(null));
			return new StrFormTerm(strf);
		}

		/// <summary>
		/// カンマで区切られたCASEのargumentを一括して取得.line端で終わる.
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		public static CaseExpression[] ReduceCaseExpressions(WordCollection wc)
		{
			List<CaseExpression> terms = new List<CaseExpression>();
			while (!wc.EOL)
			{
				terms.Add(reduceCaseExpression(wc));
				wc.ShiftNext();
			}
			CaseExpression[] ret = new CaseExpression[terms.Count];
			terms.CopyTo(ret);
			return ret;
		}

		public static IOperandTerm ReduceVariableArgument(WordCollection wc, VariableCode varCode)
		{
			IOperandTerm ret = reduceTerm(wc, false, TermEndWith.EoL, varCode);
			if(ret == null)
                throw new CodeEE("variableの:のafter argumentがdoes not exist");
			return ret;
		}

		public static VariableToken ReduceVariableIdentifier(WordCollection wc, string idStr)
		{
			string subId = null;
			if (wc.Current.Type == '@')
			{
				wc.ShiftNext();
				IdentifierWord subidWT = wc.Current as IdentifierWord;
				if (subidWT == null)
					throw new CodeEE("@の使い方が不正です");
				wc.ShiftNext();
				subId = subidWT.Code;
			}
			return GlobalStatic.IdentifierDictionary.GetVariableToken(idStr, subId, true);
		}


		/// <summary>
		/// 識別子一つを解決
		/// </summary>
		/// <param name="wc"></param>
		/// <param name="idStr">識別子string</param>
		/// <param name="varCode">variableのargumentのcaseはそのvariableのCode.連想array的につかう</param>
		/// <returns></returns>
		private static IOperandTerm reduceIdentifier(WordCollection wc, string idStr, VariableCode varCode)
		{
			wc.ShiftNext();
			SymbolWord symbol = wc.Current as SymbolWord;
			if (symbol != null && symbol.Type == '.')
			{//name空間
				throw new NotImplCodeEE();
			}
			else if (symbol != null && (symbol.Type == '(' || symbol.Type == '['))
			{//function
				wc.ShiftNext();
				if (symbol.Type == '[')//1810 多分永久に実装されnot
					throw new CodeEE("[]を使った機能はまだ実装されていません");
				//argumentをprocess
				IOperandTerm[] args = ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
				IOperandTerm mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, idStr, args, false);
				if (mToken == null)
				{
					if (!Program.AnalysisMode)
						GlobalStatic.IdentifierDictionary.ThrowException(idStr, true);
					else
					{
                        long t = 0;
						if (GlobalStatic.tempDic.TryGetValue(idStr, out t))
							GlobalStatic.tempDic[idStr] = t + 1;
						else
							GlobalStatic.tempDic.Add(idStr, 1);
						return new NullTerm(0);
					}
				}
				return mToken;
			}
			else
			{//variable or keyワード
				VariableToken id = ReduceVariableIdentifier(wc, idStr);
				if (id != null)//idStrがvariable名のcase,
				{
					if (varCode != VariableCode.__NULL__)//variableのargumentがargumentを持つthisはnot
						return VariableParser.ReduceVariable(id, null, null, null);
					else
						return VariableParser.ReduceVariable(id, wc);
				}
				//idStrがvariable名でnotcase,
				IOperandTerm refToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, idStr, null, false);
				if (refToken != null)//function参照とnameが一致したらそれを返す.実際に使うとError
					return refToken;
				if (varCode != VariableCode.__NULL__ && GlobalStatic.ConstantData.isDefined(varCode, idStr))//連想array的なpossible性アリ
					return new SingleTerm(idStr);
				GlobalStatic.IdentifierDictionary.ThrowException(idStr, false);
			}
			throw new ExeEE("Error投げ損ねた");//ここuntilでthrowかreturnのどちらかをdoはず.
		}

		#endregion

		#region private reduce
		private static CaseExpression reduceCaseExpression(WordCollection wc)
		{
			CaseExpression ret = new CaseExpression();
			IdentifierWord id = wc.Current as IdentifierWord;
			if ((id != null) && (id.Code.Equals("IS", Config.SCVariable)))
			{
				wc.ShiftNext();
				ret.CaseType = CaseExpressionType.Is;
				OperatorWord opWT = wc.Current as OperatorWord;
				if (opWT == null)
					throw new CodeEE("ISkeyワードのafter 演算子がdoes not exist");

				OperatorCode op = opWT.Code;
				if (!OperatorManager.IsBinary(op))
					throw new CodeEE("ISkeyワードのafterの演算子が2項演算子ではdoes not exist");
				wc.ShiftNext();
				ret.Operator = op;
				ret.LeftTerm = reduceTerm(wc, false, TermEndWith.Comma, VariableCode.__NULL__);
				if (ret.LeftTerm == null)
					throw new CodeEE("ISkeyワードのafter 式がdoes not exist");
				//Type type = ret.LeftTerm.GetOperandType();
				return ret;
			}
			ret.LeftTerm = reduceTerm(wc, true, TermEndWith.Comma, VariableCode.__NULL__);
			if (ret.LeftTerm == null)
				throw new CodeEE("CASEのargumentは省略できません");
			id = wc.Current as IdentifierWord;
			if ((id != null) && (id.Code.Equals("TO", Config.SCVariable)))
			{
				ret.CaseType = CaseExpressionType.To;
				wc.ShiftNext();
				ret.RightTerm = reduceTerm(wc, true, TermEndWith.Comma, VariableCode.__NULL__);
				if (ret.RightTerm == null)
					throw new CodeEE("TOkeyワードのafter 式がdoes not exist");
				id = wc.Current as IdentifierWord;
				if ((id != null) && (id.Code.Equals("TO", Config.SCVariable)))
					throw new CodeEE("TOkeyワードが2度使われています");
				if (ret.LeftTerm.GetOperandType() != ret.RightTerm.GetOperandType())
					throw new CodeEE("TOkeyワードの前afterのtypeが一致していません");
				return ret;
			}
			ret.CaseType = CaseExpressionType.Normal;
			return ret;
		}


		/// <summary>
		/// parse器の本体
		/// </summary>
		/// <param name="wc"></param>
		/// <param name="allowKeywordTo">TOkeyワードが見つかっても良いか</param>
		/// <param name="endWith">終端記号</param>
		/// <returns></returns>
        private static IOperandTerm reduceTerm(WordCollection wc, bool allowKeywordTo, TermEndWith endWith, VariableCode varCode)
        {
            TermStack stack = new TermStack();
            //int termCount = 0;
            int ternaryCount = 0;
            OperatorCode formerOp = OperatorCode.NULL;
			bool varArg = varCode != VariableCode.__NULL__;
			do
			{
				Word token = wc.Current;
				switch (token.Type)
				{
					case '\0':
						goto end;
					case '"'://LiteralStringWT
						stack.Add(((LiteralStringWord)token).Str);
						break;
					case '0'://LiteralIntegerWT
						stack.Add(((LiteralIntegerWord)token).Int);
						break;
					case 'F'://FormattedStringWT
						stack.Add(ToStrFormTerm((StrFormWord)token));
						break;
					case 'A'://IdentifierWT
						{
							string idStr = (((IdentifierWord)token).Code);
							if (idStr.Equals("TO", Config.SCVariable))
							{
								if (allowKeywordTo)
									goto end;
								else
									throw new CodeEE("TOkeyワードはここではuseできません");
							}
							else if (idStr.Equals("IS", Config.SCVariable))
								throw new CodeEE("ISkeyワードはここではuseできません");
							stack.Add(reduceIdentifier(wc, idStr, varCode));
							continue;
						}

					case '='://OperatorWT
						{
							if (varArg)
								throw new CodeEE("variableのargumentの読み取りduring 予期しnot演算子を発見しました");
							OperatorCode op = ((OperatorWord)token).Code;
							if (op == OperatorCode.Assignment)
							{
								if ((endWith & TermEndWith.Assignment) == TermEndWith.Assignment)
									goto end;
								throw new CodeEE("式insideで代入演算子'='が使われています(等価比較には'=='をuseしてください)");
							}

							if (formerOp == OperatorCode.Equal || formerOp == OperatorCode.Greater || formerOp == OperatorCode.Less
								|| formerOp == OperatorCode.GreaterEqual || formerOp == OperatorCode.LessEqual || formerOp == OperatorCode.NotEqual)
							{
								if (op == OperatorCode.Equal || op == OperatorCode.Greater || op == OperatorCode.Less
								|| op == OperatorCode.GreaterEqual || op == OperatorCode.LessEqual || op == OperatorCode.NotEqual)
								{
									ParserMediator.Warn("(構文aboveのcaution)比較演算子が連続しています.", GlobalStatic.Process.GetScaningLine(), 0, false, false);
								}
							}
							stack.Add(op);
							formerOp = op;
							if (op == OperatorCode.Ternary_a)
								ternaryCount++;
							else if (op == OperatorCode.Ternary_b)
							{
								if (ternaryCount > 0)
									ternaryCount--;
								else
									throw new CodeEE("対応do'?'のnot'#'です");
							}
							break;
						}
					case '(':
						wc.ShiftNext();
                        IOperandTerm inTerm = reduceTerm(wc, false, TermEndWith.RightParenthesis, VariableCode.__NULL__);
                        if (inTerm == null)
                            throw new CodeEE("かっこ\"(\"～\")\"のduring 式が含まれていません");
						stack.Add(inTerm);
						if (wc.Current.Type != ')')
							throw new CodeEE("対応do')'のnot'('です");
						//termCount++;
						wc.ShiftNext();
						continue;
					case ')':
						if ((endWith & TermEndWith.RightParenthesis) == TermEndWith.RightParenthesis)
							goto end;
						throw new CodeEE("構文解釈during 予期しnot記号'" + token.Type + "'を発見しました");
					case ']':
						if ((endWith & TermEndWith.RightBracket) == TermEndWith.RightBracket)
							goto end;
						throw new CodeEE("構文解釈during 予期しnot記号'" + token.Type + "'を発見しました");
					case ',':
						if ((endWith & TermEndWith.Comma) == TermEndWith.Comma)
							goto end;
						throw new CodeEE("構文解釈during 予期しnot記号'" + token.Type + "'を発見しました");
					case 'M':
						throw new ExeEE("マクロ解決failed");
					default:
						throw new CodeEE("構文解釈during 予期しnot記号'" + token.Type + "'を発見しました");
				}
				//termCount++;
				wc.ShiftNext();
			} while (!varArg);
		end:
            if (ternaryCount > 0)
                throw new CodeEE("'?'と'#'の数が正しく対応していません");
            return stack.ReduceAll();
        }
        
		#endregion

		/// <summary>
        /// 式解決forclass
        /// </summary>
        private class TermStack
        {
            /// <summary>
            /// 次に来るべきthingの種類.
            /// (前置)単項演算子かvalue待ちなら0,二項-三項演算子待ちなら1,value待ちなら2,++,--,!に対応dovalue待ちのcaseは3.
            /// </summary>
            int state = 0;
            bool hasBefore = false;
            bool hasAfter = false;
            bool waitAfter = false;
            Stack<Object> stack = new Stack<Object>();
            public void Add(OperatorCode op)
            {
                if (state == 2 || state == 3)
                    throw new CodeEE("式が異常です");
                if (state == 0)
                {
                    if (!OperatorManager.IsUnary(op))
                        throw new CodeEE("式が異常です");
                    stack.Push(op);
                    if (op == OperatorCode.Plus || op == OperatorCode.Minus || op == OperatorCode.BitNot)
                        state = 2;
                    else
                        state = 3;
                    return;
                }
                if (state == 1)
                {
                    //after置単項演算子のcaseは特殊processto
                    if (OperatorManager.IsUnaryAfter(op))
                    {
                        if (hasAfter)
                        {
                            hasAfter = false;
                            throw new CodeEE("after置の単項演算子が複数存在しています");
                        }
                        if (hasBefore)
                        {
                            hasBefore = false;
                            throw new CodeEE("インクリメント-デクリメントを前置-after置両方同whenに使うcannot be");
                        }
                        stack.Push(op);
                        reduceUnaryAfter();
                        //前置単項演算子がprocessing 待っているcaseはここで解決
                        if (waitAfter)
                            reduceUnary();
                        hasBefore = false;
                        hasAfter = true;
                        waitAfter = false;
                        return;
                    }
                    if (!OperatorManager.IsBinary(op) && !OperatorManager.IsTernary(op))
                        throw new CodeEE("式が異常です");
                    //aheadに未解決の前置演算子解決
                    if (waitAfter)
                        reduceUnary();
                    int priority = OperatorManager.GetPriority(op);
                    //直previous calculateの優ahead度がsameかheightいなら還original.
                    while (lastPriority() >= priority)
                    {
                        this.reduceLastThree();
                    }
                    stack.Push(op);
                    state = 0;
                    waitAfter = false;
                    hasBefore = false;
                    hasAfter = false;
                    return;
                }
                throw new CodeEE("式が異常です");
            }
            public void Add(Int64 i) { Add(new SingleTerm(i)); }
            public void Add(string s) { Add(new SingleTerm(s)); }
            public void Add(IOperandTerm term)
            {
                stack.Push(term);
                if (state == 1)
                    throw new CodeEE("式が異常です");
                if (state == 2)
                    waitAfter = true;
                if (state == 3)
                {
                    reduceUnary();
                    hasBefore = true;
                }
                state = 1;
                return;
            }


            private int lastPriority()
            {
                if (stack.Count < 3)
                    return -1;
                object temp = (object)stack.Pop();
                OperatorCode opCode = (OperatorCode)stack.Peek();
                int priority = OperatorManager.GetPriority(opCode);
                stack.Push(temp);
                return priority;
            }

            public IOperandTerm ReduceAll()
            {
                if (stack.Count == 0)
                    return null;
                if (state != 1)
                    throw new CodeEE("式が異常です");
                //単項演算子の待ちが未解決のwhenはここで解決
                if (waitAfter)
                    reduceUnary();
                waitAfter = false;
                hasBefore = false;
                hasAfter = false;
                while (stack.Count > 1)
                {
                    reduceLastThree();
                }
                IOperandTerm retTerm = (IOperandTerm)stack.Pop();
                return retTerm;
            }

            private void reduceUnary()
            {
                //if (stack.Count < 2)
                //    throw new ExeEE("Invalid when期のcall");
                IOperandTerm operand = (IOperandTerm)stack.Pop();
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm newTerm = OperatorMethodManager.ReduceUnaryTerm(op, operand);
                stack.Push(newTerm);
            }

            private void reduceUnaryAfter()
            {
                //if (stack.Count < 2)
                //    throw new ExeEE("Invalid when期のcall");
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm operand = (IOperandTerm)stack.Pop();
                
                IOperandTerm newTerm = OperatorMethodManager.ReduceUnaryAfterTerm(op, operand);
                stack.Push(newTerm);
				
            }
            private void reduceLastThree()
            {
                //if (stack.Count < 2)
                //    throw new ExeEE("Invalid when期のcall");
                IOperandTerm right = (IOperandTerm)stack.Pop();//afterfrom入れたほうが右側
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm left = (IOperandTerm)stack.Pop();
                if (OperatorManager.IsTernary(op))
                {
                    if (stack.Count > 1)
                    {
                        reduceTernary(left, right);
                        return;
                    }
                    throw new CodeEE("式の数が不足しています");
                }
                
                IOperandTerm newTerm = OperatorMethodManager.ReduceBinaryTerm(op, left, right);
                stack.Push(newTerm);
			}

            private void reduceTernary(IOperandTerm left, IOperandTerm right)
            {
                _ = (OperatorCode)stack.Pop();
				IOperandTerm newLeft = (IOperandTerm)stack.Pop();
				
                IOperandTerm newTerm = OperatorMethodManager.ReduceTernaryTerm(newLeft, left, right);
                stack.Push(newTerm);
            }

/*			SingleTerm GetSingle(IOperandTerm oprand)
			{
				return (SingleTerm)oprand;
			}
*/        }

    }
}