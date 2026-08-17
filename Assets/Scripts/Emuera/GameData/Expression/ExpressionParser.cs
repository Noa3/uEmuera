using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Function;

namespace MinorShift.Emuera.GameData.Expression
{
    /// <summary>
    /// Expression parser for ERA script language.
    /// Converts token sequences into evaluable expression trees,
    /// handling operators, function calls, variable references, and literals.
    /// Supports complex expressions with proper operator precedence and type checking.
    /// </summary>
    internal enum ArgsEndWith
    {
        None,
        EoL,
        RightParenthesis,//')' terminator
        RightBracket,//']' terminator
    }

    internal enum TermEndWith
    {
        None = 0x0000,
        EoL = 0x0001,
        Comma = 0x0002,//',' terminator
        RightParenthesis = 0x0004,//')' terminator
        RightBracket = 0x0008,//')' terminator
        Assignment = 0x0010,//')' terminator

        RightParenthesis_Comma = RightParenthesis | Comma,//',' or ')' terminator
        RightBracket_Comma = RightBracket | Comma,//',' or ']' terminator
        Comma_Assignment = Comma | Assignment,//',' or '=' terminator
        RightParenthesis_Comma_Assignment = RightParenthesis | Comma | Assignment,//',' or ')' or '=' terminator
        RightBracket_Comma_Assignment = RightBracket | Comma | Assignment,//',' or ']' or '=' terminator
    }

    /// <summary>
    /// Parses ERA expressions into operand terms. Provides argument reduction, integer reduction,
    /// and identifier resolution for functions and variables. Emits warnings for unknown identifiers
    /// to aid debugging and avoids duplicate tracking by using GlobalStatic.tempDic.
    /// </summary>
    internal static class ExpressionParser
    {
        #region public Reduce
        /// <summary>
        /// Reduce a comma-separated list of arguments until the specified end token.
        /// Throws CodeEE for malformed input. Current position is advanced past the end token.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="endWith">Terminal token rule</param>
        /// <param name="isDefine">Indicates function definition style (supports default values)</param>
        /// <returns>Array of reduced operand terms</returns>
        public static IOperandTerm[] ReduceArguments(WordCollection wc, ArgsEndWith endWith, bool isDefine)
        {
            if(wc == null)
                throw new ExeEE(GameMessages.T("An empty stream was passed"));
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
                            throw new CodeEE(GameMessages.T("No closing ']' found for '['"));
                        if (endWith == ArgsEndWith.RightParenthesis)
                            throw new CodeEE(GameMessages.T("No closing ')' found for '('"));
                        goto end;
                    case ')':
                        if (endWith == ArgsEndWith.RightParenthesis)
                        {
                            wc.ShiftNext();
                            goto end;
                        }
                        throw new CodeEE(GameMessages.T("Unexpected ')' found during parsing"));
                    case ']':
                        if (endWith == ArgsEndWith.RightBracket)
                        {
                            wc.ShiftNext();
                            goto end;
                        }
                        throw new CodeEE(GameMessages.T("Unexpected ']' found during parsing"));
                }
                if(!isDefine)
                    terms.Add(ReduceExpressionTerm(wc, termEndWith));
                else
                {
                    terms.Add(ReduceExpressionTerm(wc, termEndWith_Assignment));
                    if (terms[terms.Count - 1] == null)
                        throw new CodeEE(GameMessages.T("The argument of a function definition cannot be omitted"));
                    if (wc.Current is OperatorWord)
                    {//there is '='
                        wc.ShiftNext();
                        IOperandTerm term = reduceTerm(wc, false, termEndWith, VariableCode.__NULL__);
                        if (term == null)
                            throw new CodeEE(GameMessages.T("No expression after '='"));
                        if (term.GetOperandType() != terms[terms.Count - 1].GetOperandType())
                            throw new CodeEE(GameMessages.T("The types before and after '=' do not match"));
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
        /// Reduce a single expression (numeric or string). May return null.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="endWith">Terminal token rule</param>
        /// <returns>Reduced operand term or null</returns>
        public static IOperandTerm ReduceExpressionTerm(WordCollection wc, TermEndWith endWith)
        {
            IOperandTerm term = reduceTerm(wc, false, endWith, VariableCode.__NULL__);
            return term;
        }

        /// <summary>
        /// Reduce an integer expression. Throws CodeEE when not numeric.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="endwith">Terminal token rule</param>
        /// <returns>Reduced integer operand term</returns>
        public static IOperandTerm ReduceIntegerTerm(WordCollection wc, TermEndWith endwith)
        {
            IOperandTerm term = reduceTerm(wc, false, endwith, VariableCode.__NULL__);
            if (term == null)
                throw new CodeEE(GameMessages.T("The syntax cannot be interpreted as an expression"));
            if (term.GetOperandType() != typeof(Int64))
                throw new CodeEE(GameMessages.T("The expression result is not a number"));
            return term;
        }

        /// <summary>
        /// Convert a formatted string token into an operand term. Returns a SingleTerm when constant.
        /// </summary>
        /// <param name="sfw">Formatted string token</param>
        /// <returns>Operand term representing the string</returns>
        public static IOperandTerm ToStrFormTerm(StrFormWord sfw)
        {
            StrForm strf = StrForm.FromWordToken(sfw);
            if(strf.IsConst)
                return new SingleTerm(strf.GetString(null));
            return new StrFormTerm(strf);
        }

        /// <summary>
        /// Reduce CASE expression arguments (comma-separated) until end of line.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <returns>Array of case expressions</returns>
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

        /// <summary>
        /// Reduce a variable's argument following ':' for associative-like access.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="varCode">Variable code</param>
        /// <returns>Operand term for the variable argument</returns>
        public static IOperandTerm ReduceVariableArgument(WordCollection wc, VariableCode varCode)
        {
            IOperandTerm ret = reduceTerm(wc, false, TermEndWith.EoL, varCode);
            if(ret == null)
                throw new CodeEE(GameMessages.T("No argument after the variable's ':'"));
            return ret;
        }

        /// <summary>
        /// Resolve a variable identifier, handling optional '@' local reference.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="idStr">Identifier string</param>
        /// <returns>Resolved variable token or null</returns>
        public static VariableToken ReduceVariableIdentifier(WordCollection wc, string idStr)
        {
            string subId = null;
            if (wc.Current.Type == '@')
            {
                wc.ShiftNext();
                IdentifierWord subidWT = wc.Current as IdentifierWord;
                if (subidWT == null)
                    throw new CodeEE(GameMessages.T("Invalid use of '@'"));
                wc.ShiftNext();
                subId = subidWT.Code;
            }
            return GlobalStatic.IdentifierDictionary.GetVariableToken(idStr, subId, true);
        }

        /// <summary>
        /// Resolve a single identifier (function or variable). Emits a warning and tracks unknown identifiers.
        /// </summary>
        /// <param name="wc">Token stream</param>
        /// <param name="idStr">Identifier string</param>
        /// <param name="varCode">Variable code context for associative access</param>
        /// <returns>Operand term for the identifier</returns>
        private static IOperandTerm reduceIdentifier(WordCollection wc, string idStr, VariableCode varCode)
        {
            wc.ShiftNext();
            SymbolWord symbol = wc.Current as SymbolWord;
            if (symbol != null && symbol.Type == '.')
            {//namespace
                throw new NotImplCodeEE();
            }
            else if (symbol != null && (symbol.Type == '(' || symbol.Type == '['))
            {//function
                wc.ShiftNext();
                if (symbol.Type == '[')//1810 Probably never implemented
                    throw new CodeEE(GameMessages.T("Functionality using '[]' is not implemented yet"));
                //Process the arguments
                IOperandTerm[] args = ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
                IOperandTerm mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, idStr, args, false);
                if (mToken == null)
                {
                    // Progressive/lazy loading: the function may be defined in a file
                    // that has not been compiled yet (Fast boot) or is still loading in
                    // the background (legacy progressive). Turn it into a lazily-resolved
                    // term instead of warning + NullTerm(0), which would silently break
                    // game logic (the call would evaluate to 0/false in every expression).
                    if (FunctionResolver.IsKnownMethod(idStr))
                        return new PendingUserDefinedMethodTerm(idStr, args);
                    // Warn and track unknown function identifier
                    ScriptPosition pos = GlobalStatic.Process.GetScaningLine()?.Position ?? new ScriptPosition();
                    ParserMediator.Warn(string.Format(GameMessages.UnrecognizedIdentifier, idStr), pos, 1);
                    long t = 0;
                    if (GlobalStatic.tempDic.TryGetValue(idStr, out t))
                        GlobalStatic.tempDic[idStr] = t + 1;
                    else
                        GlobalStatic.tempDic.Add(idStr, 1);
                    return new NullTerm(0);
                }
                return mToken;
            }
            else
            {//variable or keyword
                VariableToken id = ReduceVariableIdentifier(wc, idStr);
                if (id != null)//If idStr is a variable name,
                {
                    if (varCode != VariableCode.__NULL__)//A variable's argument never has an argument
                        return VariableParser.ReduceVariable(id, null, null, null);
                    else
                        return VariableParser.ReduceVariable(id, wc);
                }
                //If idStr is not a variable name,
                IOperandTerm refToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, idStr, null, false);
                if (refToken != null)//If a function reference matches the name, return it. Actually using it causes an Error
                    return refToken;
                if (varCode != VariableCode.__NULL__ && GlobalStatic.ConstantData.isDefined(varCode, idStr))//Possibly an associative array-like use
                    return new SingleTerm(idStr);
                // Warn and track unknown variable/keyword identifier
                ScriptPosition pos2 = GlobalStatic.Process.GetScaningLine()?.Position ?? new ScriptPosition();
                ParserMediator.Warn(string.Format(GameMessages.UnrecognizedIdentifier, idStr), pos2, 1);
                long ct = 0;
                if (GlobalStatic.tempDic.TryGetValue(idStr, out ct))
                    GlobalStatic.tempDic[idStr] = ct + 1;
                else
                    GlobalStatic.tempDic.Add(idStr, 1);
                return new NullTerm(0);
            }
            throw new ExeEE(GameMessages.T("Failed to throw an error"));//By this point either a throw or a return should have been made.
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
					throw new CodeEE(GameMessages.T("No operator after the IS keyword"));

				OperatorCode op = opWT.Code;
				if (!OperatorManager.IsBinary(op))
					throw new CodeEE(GameMessages.T("The operator after the IS keyword is not a binary operator"));
				wc.ShiftNext();
				ret.Operator = op;
				ret.LeftTerm = reduceTerm(wc, false, TermEndWith.Comma, VariableCode.__NULL__);
				if (ret.LeftTerm == null)
					throw new CodeEE(GameMessages.T("No expression after the IS keyword"));
				//Type type = ret.LeftTerm.GetOperandType();
				return ret;
			}
			ret.LeftTerm = reduceTerm(wc, true, TermEndWith.Comma, VariableCode.__NULL__);
		if (ret.LeftTerm == null)
			throw new CodeEE(GameMessages.T("The argument of CASE cannot be omitted"));
			id = wc.Current as IdentifierWord;
			if ((id != null) && (id.Code.Equals("TO", Config.SCVariable)))
			{
				ret.CaseType = CaseExpressionType.To;
				wc.ShiftNext();
				ret.RightTerm = reduceTerm(wc, true, TermEndWith.Comma, VariableCode.__NULL__);
		if (ret.RightTerm == null)
				throw new CodeEE(GameMessages.T("No expression after the TO keyword"));
			id = wc.Current as IdentifierWord;
			if ((id != null) && (id.Code.Equals("TO", Config.SCVariable)))
				throw new CodeEE(GameMessages.T("The TO keyword is used twice"));
			if (ret.LeftTerm.GetOperandType() != ret.RightTerm.GetOperandType())
				throw new CodeEE(GameMessages.T("The types before and after the TO keyword do not match"));
				return ret;
			}
			ret.CaseType = CaseExpressionType.Normal;
			return ret;
		}


		/// <summary>
		/// Core expression reduction engine. Builds a stack of operands and operators and reduces based on precedence.
		/// </summary>
		/// <param name="wc">Token stream</param>
		/// <param name="allowKeywordTo">If true, allows the TO keyword as a terminator</param>
		/// <param name="endWith">Terminal token rule</param>
		/// <param name="varCode">Variable code context</param>
		/// <returns>Reduced operand term or null</returns>
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
									throw new CodeEE(GameMessages.T("The TO keyword cannot be used here"));
							}
							else if (idStr.Equals("IS", Config.SCVariable))
								throw new CodeEE(GameMessages.T("The IS keyword cannot be used here"));
							stack.Add(reduceIdentifier(wc, idStr, varCode));
							continue;
						}

					case '='://OperatorWT
						{
							if (varArg)
								throw new CodeEE(GameMessages.T("An unexpected operator was found while reading the variable argument"));
							OperatorCode op = ((OperatorWord)token).Code;
							if (op == OperatorCode.Assignment)
							{
								if ((endWith & TermEndWith.Assignment) == TermEndWith.Assignment)
									goto end;
								throw new CodeEE(GameMessages.T("Assignment operator '=' was used in an expression (use '==' for equality comparison)"));
							}

							if (formerOp == OperatorCode.Equal || formerOp == OperatorCode.Greater || formerOp == OperatorCode.Less
								|| formerOp == OperatorCode.GreaterEqual || formerOp == OperatorCode.LessEqual || formerOp == OperatorCode.NotEqual)
							{
								if (op == OperatorCode.Equal || op == OperatorCode.Greater || op == OperatorCode.Less
								|| op == OperatorCode.GreaterEqual || op == OperatorCode.LessEqual || op == OperatorCode.NotEqual)
								{
									ParserMediator.Warn(GameMessages.T("(Syntax note) Comparison operators are consecutive."), GlobalStatic.Process.GetScaningLine(), 0, false, false);
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
									throw new CodeEE(GameMessages.T("'#' without a matching '?'"));
							}
							break;
						}
					case '(':
						wc.ShiftNext();
                        IOperandTerm inTerm = reduceTerm(wc, false, TermEndWith.RightParenthesis, VariableCode.__NULL__);
                        if (inTerm == null)
                            throw new CodeEE(GameMessages.T("No expression inside parentheses '(' ~ ')'"));
						stack.Add(inTerm);
					if (wc.Current.Type != ')')
						throw new CodeEE(GameMessages.T("'(' without a matching ')'"));
						//termCount++;
						wc.ShiftNext();
						continue;
					case ')':
						if ((endWith & TermEndWith.RightParenthesis) == TermEndWith.RightParenthesis)
							goto end;
						throw new CodeEE(GameMessages.T("Unexpected symbol '") + token.Type + GameMessages.T("' found during parsing"));
					case ']':
						if ((endWith & TermEndWith.RightBracket) == TermEndWith.RightBracket)
							goto end;
						throw new CodeEE(GameMessages.T("Unexpected symbol '") + token.Type + GameMessages.T("' found during parsing"));
					case ',':
						if ((endWith & TermEndWith.Comma) == TermEndWith.Comma)
							goto end;
						throw new CodeEE(GameMessages.T("Unexpected symbol '") + token.Type + GameMessages.T("' found during parsing"));
				case 'M':
					throw new ExeEE(GameMessages.T("Macro expansion failed"));
					default:
						throw new CodeEE(GameMessages.T("Unexpected symbol '") + token.Type + GameMessages.T("' found during parsing"));
				}
				//termCount++;
				wc.ShiftNext();
			} while (!varArg);
		end:
            if (ternaryCount > 0)
                throw new CodeEE(GameMessages.T("The number of '?' and '#' do not match"));
            return stack.ReduceAll();
        }
        
		#endregion

		/// <summary>
        /// Term stack used by the expression reducer. Manages operator precedence and unary/binary/ternary operations.
        /// </summary>
        private class TermStack
        {
            /// <summary>
            /// Next expected token type: 0 for unary or value, 1 for binary/ternary, 2 for value (after '+', '-', '~'), 3 for value for '++', '--', '!';
            /// </summary>
            int state = 0;
            bool hasBefore = false;
            bool hasAfter = false;
            bool waitAfter = false;
            Stack<Object> stack = new Stack<Object>();
            public void Add(OperatorCode op)
            {
                if (state == 2 || state == 3)
                    throw new CodeEE(GameMessages.T("The expression is invalid"));
                if (state == 0)
                {
                    if (!OperatorManager.IsUnary(op))
                        throw new CodeEE(GameMessages.T("The expression is invalid"));
                    stack.Push(op);
                    if (op == OperatorCode.Plus || op == OperatorCode.Minus || op == OperatorCode.BitNot)
                        state = 2;
                    else
                        state = 3;
                    return;
                }
                if (state == 1)
                {
                    //Redirect to special handling for postfix unary operators
                    if (OperatorManager.IsUnaryAfter(op))
                    {
                        if (hasAfter)
                        {
                            hasAfter = false;
                            throw new CodeEE(GameMessages.T("Multiple postfix unary operators exist"));
                        }
                        if (hasBefore)
                        {
                            hasBefore = false;
                            throw new CodeEE(GameMessages.T("Increment/decrement cannot be used as both prefix and postfix simultaneously"));
                        }
                        stack.Push(op);
                        reduceUnaryAfter();
                        //If a prefix unary operator is awaiting processing, resolve it here
                        if (waitAfter)
                            reduceUnary();
                        hasBefore = false;
                        hasAfter = true;
                        waitAfter = false;
                        return;
                    }
                    if (!OperatorManager.IsBinary(op) && !OperatorManager.IsTernary(op))
                        throw new CodeEE(GameMessages.T("The expression is invalid"));
                    //Resolve unresolved prefix operators first
                    if (waitAfter)
                        reduceUnary();
                    int priority = OperatorManager.GetPriority(op);
                    //Reduce when the priority of the preceding operation is equal or higher.
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
                throw new CodeEE(GameMessages.T("The expression is invalid"));
            }
            public void Add(Int64 i) { Add(new SingleTerm(i)); }
            public void Add(string s) { Add(new SingleTerm(s)); }
            public void Add(IOperandTerm term)
            {
                stack.Push(term);
                if (state == 1)
                    throw new CodeEE(GameMessages.T("The expression is invalid"));
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
                    throw new CodeEE(GameMessages.T("The expression is invalid"));
                //If a unary operator is pending unresolved, resolve it here
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
                //    throw new ExeEE("Invalid call timing");
                IOperandTerm operand = (IOperandTerm)stack.Pop();
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm newTerm = OperatorMethodManager.ReduceUnaryTerm(op, operand);
                stack.Push(newTerm);
            }

            private void reduceUnaryAfter()
            {
                //if (stack.Count < 2)
                //    throw new ExeEE("Invalid call timing");
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm operand = (IOperandTerm)stack.Pop();
                
                IOperandTerm newTerm = OperatorMethodManager.ReduceUnaryAfterTerm(op, operand);
                stack.Push(newTerm);
                
            }
            private void reduceLastThree()
            {
                //if (stack.Count < 2)
                //    throw new ExeEE("Invalid call timing");
                IOperandTerm right = (IOperandTerm)stack.Pop();//The one pushed later is on the right side
                OperatorCode op = (OperatorCode)stack.Pop();
                IOperandTerm left = (IOperandTerm)stack.Pop();
                if (OperatorManager.IsTernary(op))
                {
                    if (stack.Count > 1)
                    {
                        reduceTernary(left, right);
                        return;
                    }
                    throw new CodeEE(GameMessages.T("Insufficient number of expressions"));
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

/*            SingleTerm GetSingle(IOperandTerm oprand)
            {
                return (SingleTerm)oprand;
            }
*/        }

    }
}
