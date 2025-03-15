using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Celeste.Mod.MacroRoutingTool.Logic;

/// <summary>
/// An expression that returns a numeric value when evaluated.
/// </summary>
public class NumericExpression {
    /// <summary>
    /// The source code from which this expression was compiled.
    /// </summary>
    public string Source;

    /// <summary>
    /// Wrapper class for a function's <c cref="MethodBase">MethodBase</c> that contains additional info about it.
    /// </summary>
    public class FunctionInfo
    {
        /// <summary>
        /// Original reflection info about the function.
        /// </summary>
        public MethodBase Function; //(i wanted to just have the FunctionInfo class inherit from MethodBase instead, but turns out that's really complicated)
        /// <summary>
        /// Number of parameters this function is defined with.
        /// </summary>
        public readonly int ParamCount;
        /// <summary>
        /// Whether the last parameter of the function was declared with the <c>params</c> keyword. 
        /// </summary>
        public readonly bool IsVararg;

        /// <summary>
        /// <inheritdoc cref="FunctionInfo"/>
        /// </summary>
        /// <param name="func">The <c cref="MethodBase">MethodBase</c> to get additional info about.</param>
        public FunctionInfo(MethodBase func) {
            Function = func;
            ParameterInfo[] paramInfos = func.GetParameters();
            ParamCount = paramInfos.Length;
            IsVararg = paramInfos[^1].GetCustomAttribute<ParamArrayAttribute>() != null;
        }
    }

    /// <summary>
    /// Extension of <c cref="FunctionInfo">FunctionInfo</c> that also tracks an operator's precedence group, represented as its priority in a <c cref="PriorityQueue">PriorityQueue</c>.
    /// </summary>
    /// <param name="func">The <c cref="MethodBase">MethodBase</c> to get additional info about.</param>
    /// <param name="priority">Priority, lower meaning more prioritized. All operators in the same precedence group should have the same priority.</param>
    public class OperatorInfo(MethodBase func, int priority) : FunctionInfo(func) {
        public int Priority = priority;
    }

    public static class FunctionDefs {
        /// <summary>
        /// Special case to match the FrostHelper session expression specification.<br/>
        /// If both operands are integers, <c>/</c> will perform integer division, but <c>//</c> will perform float division.<br/>
        /// If either operand is a float, <c>/</c> will still perform float division.
        /// </summary>
        public static float Quotient(float a, float b) {
            if (float.IsInteger(a) && float.IsInteger(b)) return (int)a / (int)b;
            return a / b;
        }

        public static bool And(float a, float b) => a != 0 && b != 0;
        public static bool Or(float a, float b) => a != 0 || b != 0;
        public static bool Not(float a) => a == 0;

        public static float Pi() => (float)Math.PI;
    }

    /// <summary>
    /// A global context where all variables declared in MacroRoutingTool expressions are stored.
    /// </summary>
    public static readonly Dictionary<string, float> Variables;

    /// <summary>
    /// List of globally defined functions to be called using the function calling convention, like <c>sum(a, b)</c>.
    /// </summary>
    public static readonly Dictionary<string, FunctionInfo> Functions = new(){
        {"min", new(typeof(Monocle.Calc).GetMethod("Min"))},
        {"max", new(typeof(Monocle.Calc).GetMethod("Max"))},
        {"clamp", new(typeof(Math).GetMethod("Clamp"))},
        {"abs", new(typeof(Math).GetMethod("Abs"))},
        {"sin", new(typeof(Math).GetMethod("Sin"))},
        {"cos", new(typeof(Math).GetMethod("Cos"))},
        {"tan", new(typeof(Math).GetMethod("Tan"))}
    };

    /// <summary>
    /// List of globally defined functions to be called using the operator calling convention, like <c>a + b</c>, grouped by operator precedence.
    /// </summary>
    public static readonly Dictionary<string, OperatorInfo> Operators = new(){
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/#operator-precedence
        //https://learn.microsoft.com/en-us/dotnet/api/system.single (check the interfaces it implements for math operators)
        //https://stackoverflow.com/questions/649375
        //TODO bitwise and/or. will require supporting method groups (for operator overloads) and refactoring parser to distinguish between ints and floats
        
        {"!", new(typeof(FunctionDefs).GetMethod("Not"), 0)},
        //once u build the mod, if calling a math operator crashes, replace the name here with "op_CheckedMultiply" etc.
        //in .NET 7, the non-checked ones throw BadImageFormatException when called through reflection.
        {"*", new(typeof(IMultiplyOperators<float, float, float>).GetMethod("op_Multiply"), 1)},
        {"/", new(typeof(FunctionDefs).GetMethod("Quotient"), 1)},
        {"//", new(typeof(IDivisionOperators<float, float, float>).GetMethod("op_Division"), 1)},
        {"%", new(typeof(IModulusOperators<float, float, float>).GetMethod("op_Modulus"), 1)},
        {"+", new(typeof(IAdditionOperators<float, float, float>).GetMethod("op_Addition"), 2)},
        {"-", new(typeof(ISubtractionOperators<float, float, float>).GetMethod("op_Subtraction"), 2)},
        {"<", new(typeof(float).GetMethod("op_LessThan"), 3)},
        {"<=", new(typeof(float).GetMethod("op_LessThanOrEqual"), 3)},
        {">", new(typeof(float).GetMethod("op_GreaterThan"), 3)},
        {">=", new(typeof(float).GetMethod("op_GreaterThanOrEqual"), 3)},
        {"==", new(typeof(float).GetMethod("op_Equality"), 4)},
        {"!=", new(typeof(float).GetMethod("op_Inequality"), 4)},
        {"&&", new(typeof(FunctionDefs).GetMethod("And"), 5)},
        {"||", new(typeof(FunctionDefs).GetMethod("Or"), 6)}
    };

    public static readonly Dictionary<string, float> Constants = new(){
        {"pi", (float)Math.PI}
    };

    /// <summary>
    /// Used in parsing and execution of an expression to track details about how a function was called.
    /// </summary>
    public struct CallData {
        /// <summary>
        /// The function name as written in the expression's source code.
        /// </summary>
        public string Name;
        /// <summary>
        /// The number of arguments passed to this function.
        /// </summary>
        public int ArgCount;
        /// <summary>
        /// Additional info about the function.
        /// </summary>
        public FunctionInfo FuncInfo; //TODO support method groups
    }

    public enum OpCodes {
        /// <summary>
        /// This instruction calls a function and pushes its return value onto the stack.<br/>
        /// Its <c cref="Instruction.Value">Value</c> is a <c cref="CallData">FuncData</c> reflecting that function.
        /// </summary>
        Call,
        /// <summary>
        /// This instruction gets the value of a variable and pushes that value onto the stack.<br/>
        /// Its <c cref="Instruction.Value">Value</c> is the name of that variable in <c cref="Variables">Variables</c>. 
        /// </summary>
        GetVar,
        /// <summary>
        /// This instruction pushes a constant value onto the stack.<br/>
        /// Its <c cref="Instruction.Value">Value</c> is the float value to push.
        /// </summary>
        Const
    }

    public struct Instruction {
        /// <summary>
        /// The instruction's opcode. This should be a member of the <c cref="OpCodes">OpCodes</c> enum cast to an <c>int</c>
        /// unless you're IL hooking <c cref="Evaluate">Evaluate</c> to add more opcodes.
        /// </summary>
        public int OpCode;
        
        /// <summary>
        /// The instruction's value. This value's type depends on this instruction's <c cref="OpCode">OpCode</c> --
        /// see the <c cref="OpCodes">OpCodes</c> enum for details. 
        /// </summary>
        public object Value;
    }

    public LinkedList<Instruction> Instructions;

    /// <summary>
    /// Executes the expression. Returns false if the result is exactly 0, true otherwise.
    /// </summary>
    public float Evaluate() {
        //TODO could maybe use/extend the existing instruction types in System.Linq.Expression instead of defining my own?
        Stack<float> stack = new();
        foreach (Instruction instr in Instructions) {
            switch (instr.OpCode) {
            case (int)OpCodes.Call:
                CallData data = (CallData)instr.Value;
                int constArgCount = data.FuncInfo.ParamCount;
                object[] args = new object[data.FuncInfo.ParamCount];
                if (data.FuncInfo.IsVararg) {
                    //handle variable number of arguments (params keyword)
                    constArgCount--;
                    int varArgCount = data.FuncInfo.ParamCount - data.ArgCount;
                    float[] varargs = new float[varArgCount];
                    for (int i = varArgCount - 1; i >= 0; i--) {
                        varargs[i] = stack.Pop();
                    }
                    args[^1] = varargs;
                }
                //handle the rest of the arguments
                for (int i = constArgCount - 1; i >= 0; i++) {
                    args[i] = stack.Pop();
                }
                //call the function
                object retval = data.FuncInfo.Function.Invoke(null, args);
                //put the result on the stack
                if (retval is float retfloat) {
                    stack.Push(retfloat);
                } else if (retval is bool retbool) {
                    stack.Push(retbool ? 1 : 0);
                }
                break;
            case (int)OpCodes.GetVar:
                stack.Push(Variables[(string)instr.Value]);
                break;
            case (int)OpCodes.Const:
                stack.Push((float)instr.Value);
                break;
            }
        }
        return stack.Pop();
    }

    /// <summary>
    /// Try to parse the expression written in <c>source</c> into a new <c>BoolExp</c>.
    /// Return true if successful and false if <c>source</c> contains invalid code.
    /// </summary>
    public static bool TryParse(string source, out NumericExpression expression, out string errorMsg) {
        expression = new(){Source = source};

        //error reporting
        bool valid = true;
        string syntaxErrMsg = "";
        string fullErrMsg = $"Failed to parse expression: {source}";
        List<string> constsNotFound = [];
        List<string> funcsNotFound = [];
        Dictionary<int, CallData> funcArgMismatches = [];

        //generic parsing
        int startIndex = 0;
        int curIndex = 0;
        int length = 1;

        //everything under this comment is quite hardcoded, but a generic (e.g. ANTLR-like) parsing system seems unnecessary for now

        Stack<Queue<LinkedListNode<Instruction>>> groupValSections = []; //start and end of each grouping level when accessed
        expression.Instructions.AddFirst(new Instruction()); //the first node of each pair in groupSections will be populated with the instruction before the first one in that grouping.
                                                             //due to this, a dummy node is needed to be able to access the very first instruction. it then needs to be removed when parsing is complete.
        groupValSections.Push([]);
        groupValSections.Peek().Enqueue(expression.Instructions.First);

        Stack<Instruction> funcStack = []; //track non-operator function calls separately so they can be postfixed
        Stack<Queue<Instruction>> opGroups = []; //track operator calls separately so they can be postfixed in the order respecting operator precedence

        bool expectValue = true;
        bool symFuncConst = false; //the symbol about to be processed is for either a function or constant

        void distribOps(NumericExpression expression) {
            groupValSections.Peek().Enqueue(expression.Instructions.Last);

            //i wont support operator chaining (i.e. "ternary operators") for now. the only one i know of is c?t:f which already has the workaround t*c+f*!c
            Queue<Instruction> groupOps = opGroups.Pop();
            PriorityQueue<Instruction?, int> activeOps = new();
            LinkedListNode<Instruction> curInstr;
            Instruction curOp;
            Instruction? prevOp = null;
            int prevPrio = 0;
            Queue<LinkedListNode<Instruction>> sectionVals = groupValSections.Pop();
            while (sectionVals.TryDequeue(out curInstr)) {
                LinkedListNode<Instruction> endInstr = sectionVals.Dequeue();
                if (curInstr == endInstr) {
                    break;
                }
                LinkedListNode<Instruction> nextValInstr = curInstr.Next;
                while ((curInstr = nextValInstr) != endInstr.Next) {
                    nextValInstr = curInstr.Next;
                    curOp = groupOps.Dequeue();
                    OperatorInfo info = (OperatorInfo)curOp.Value;
                    if (info.ParamCount == 1) {
                        //unary operator, add immediately after this instruction
                        expression.Instructions.AddAfter(curInstr, curOp);
                    } else {
                        //binary operator, respect precedence
                        if (prevPrio == 0) {
                            //first binary operator in this grouping
                            activeOps.Enqueue(curOp, info.Priority);
                        } else if (prevPrio < info.Priority) {
                            //the operator just encountered has higher precedence, so the previous one will come later
                            activeOps.Enqueue(prevOp, prevPrio);
                        } else {
                            //the operator just encountered has lower precedence, so this is where all the queued ones go
                            while (activeOps.TryDequeue(out Instruction? queuedOp, out _) && ((OperatorInfo)queuedOp?.Value).Priority >= prevPrio) {
                                curInstr = expression.Instructions.AddAfter(curInstr, (Instruction)queuedOp);
                            }
                            activeOps.Enqueue(curOp, info.Priority);
                        }
                        prevOp = curOp;
                        prevPrio = info.Priority;
                    }
                }
                curInstr = curInstr.Previous;
            }
            //end of grouping
            while (activeOps.TryDequeue(out Instruction? queuedOp, out _)) {
                curInstr = expression.Instructions.AddAfter(curInstr, (Instruction)queuedOp);
            }
        }

        void processLitSym(NumericExpression expression) {
            string litsym = source.Substring(startIndex, length).Trim();
            if (litsym.Length > 1) {
                //there is a literal or symbol to be processed
                expectValue = false;
                litsym = litsym[..^2];
                if (symFuncConst) {
                    //constant
                    symFuncConst = false;
                    if (Constants.TryGetValue(litsym, out float literal)) {
                        //a constant with this name exists
                        if (!valid) {goto Next;}
                        expression.Instructions.AddLast(new Instruction(){
                            OpCode = (int)OpCodes.Const,
                            Value = literal
                        });
                    } else {
                        //a constant with this name does NOT exist, log and continue in case of more errors
                        constsNotFound.Add(litsym);
                        valid = false;
                    }
                } else if (!valid) {
                    //if there's an error earlier, other symbols dont matter
                    goto Next;
                } else if (float.TryParse(litsym, NumberStyles.Float, CultureInfo.InvariantCulture, out float literal)) {
                    //literal
                    expression.Instructions.AddLast(new Instruction(){
                        OpCode = (int)OpCodes.Const,
                        Value = literal
                    });
                } else {
                    //variable
                    if (!Variables.ContainsKey(litsym)) {
                        Variables.Add(litsym, 0);
                    }
                    expression.Instructions.AddLast(new Instruction(){
                        OpCode = (int)OpCodes.GetVar,
                        Value = litsym
                    });
                }
            }
            //go next
            Next:
            startIndex = curIndex;
            length = 1;
        }

        while (true) {
            curIndex = startIndex + length - 1;
            if (curIndex >= source.Length) {
                //end of source code:
                processLitSym(expression);
                if (expectValue || !expression.Instructions.Any()) {
                    syntaxErrMsg = "A value is missing at the end of the expression.";
                    goto SyntaxError;
                }
                distribOps(expression);
                if (funcStack.Count > 0) {
                    syntaxErrMsg = "Expression ends before all parentheses are closed.";
                    goto SyntaxError;
                }
                break;
            }

            char next = source[curIndex];

            if (next == '$') {
                if (!expectValue) {
                    syntaxErrMsg = string.Format("'$' is not allowed to be part of a symbol (encountered at index ${0})", curIndex);
                    goto SyntaxError;
                }

                //start of symbol for either constant or function
                symFuncConst = true;

                //go next
                startIndex = curIndex + 1;
                length = 1;
                continue;
            }
            
            if (next == '(') {
                //start of grouping and/or call
                if (symFuncConst) {
                    //start of call only:
                    symFuncConst = false;
                    CallData funcData = new(){
                        Name = source.Substring(startIndex, length - 1).Trim().ToLower(),
                        ArgCount = 1
                    };
                    if (Functions.TryGetValue(funcData.Name, out FunctionInfo funcInfo)) {
                        funcData.FuncInfo = funcInfo;
                    } else {
                        //function with this name does NOT exist, log and continue in case there are more errors
                        funcsNotFound.Add(funcData.Name);
                        valid = false;
                        funcData.FuncInfo = null;
                    }
                    funcStack.Push(new(){
                        OpCode = (int)OpCodes.Call,
                        Value = funcData
                    });
                }
                //regardless:
                expectValue = true;
                opGroups.Push([]);
                groupValSections.Peek().Enqueue(expression.Instructions.Last);
                Queue<LinkedListNode<Instruction>> nextGroup = [];
                nextGroup.Enqueue(expression.Instructions.Last);
                groupValSections.Push(nextGroup);

                //go next
                startIndex = curIndex + 1;
                length = 1;
                continue;
            }

            if (next == ',' || next == ')') {
                //end of grouping and/or call
                processLitSym(expression);
                if (expectValue) {
                    syntaxErrMsg = string.Format("A value is missing before the '${0}' at index ${1}", curIndex < source.Length ? source[curIndex] : "EOS", curIndex);
                    goto SyntaxError;
                }
                if (valid) {
                    distribOps(expression);
                    groupValSections.Peek().Enqueue(expression.Instructions.Last);
                }

                if (next == ')') {
                    //end of call only:
                    //check if the function exists and expects the given number of arguments, add to compilation if so, error if not
                    Instruction funcInstr = funcStack.Pop();
                    CallData funcData = (CallData)funcInstr.Value;
                    if (funcData.FuncInfo != null) {
                        //the specified function exists
                        if (funcData.ArgCount == funcData.FuncInfo.ParamCount || (funcData.ArgCount > funcData.FuncInfo.ParamCount && funcData.FuncInfo.IsVararg)) {
                            //the specified function can be called with the number of arguments given
                            //no more checks needed -- add the instruction and we're done with this call!
                            funcInstr.Value = funcData.FuncInfo;
                            expression.Instructions.AddLast(funcInstr);
                        } else {
                            //the specified function can NOT be called with the number of arguments given, log and continue in case of more errors
                            funcArgMismatches.Add(curIndex, funcData);
                            valid = false;
                        }
                    } //(function not existing is already handled when starting a new grouping)
                } else {
                    //argument separator only:
                    Instruction funcInstr = funcStack.Peek();
                    CallData funcData = (CallData)funcInstr.Value;
                    funcData.ArgCount++;
                    funcInstr.Value = funcData;
                    opGroups.Push([]);
                }

                //go next
                startIndex = curIndex + 1;
                length = 1;
                continue;
            }

            //check for operators (do this as late as possible, it's probably expensive)
            IEnumerable<string> possibleOps = Operators.Keys.Where(key => key.StartsWith(next));
            if (possibleOps.Any()) {
                //`next` is the first character of an operator
                
                processLitSym(expression);

                //determine which operator this is
                string fullOpText;
                do {
                    fullOpText = possibleOps.First();
                    length++;
                } while ((possibleOps = possibleOps.Where(str => str.StartsWith(source.Substring(startIndex, length)))).Any());

                //check if the operator can be called with the given number of arguments
                if (fullOpText == "-") {
                    //negative is handled by float.TryParse, so treat the minus sign as part of the value
                    goto Value;
                } else {
                    FunctionInfo info = Operators[fullOpText];
                    if (info.ParamCount == 1 && !expectValue) {
                        syntaxErrMsg = string.Format("Encountered unary operator '${0}' at index ${1} where a value was not expected.", fullOpText, curIndex);
                        goto SyntaxError;
                    }
                    if (info.ParamCount == 2 && expectValue) {
                        syntaxErrMsg = string.Format("A value is missing on the left side of the '${0}' at index ${1}.", fullOpText, curIndex);
                        goto SyntaxError;
                    }

                    //all good, add the instruction
                    opGroups.Peek().Enqueue(new(){
                        OpCode = (int)OpCodes.Call,
                        Value = Operators[fullOpText]
                    });
                }

                //go next
                expectValue = true;
                symFuncConst = false;
                startIndex += length;
                length = 1;
                continue;
            }

            //if `next` didn't match any of the above, it's part of a literal, symbol, or whitespace.
            //those are all processed by calling processLitSym() elsewhere, so do nothing for now.
            //note: since evaluation of these expressions is independent from the game, the symbols in FrostHelper expressions
            //that would reference session data (flags, counters, some "commands") are just treated like variables in these expressions.
            Value:
            expectValue = false;
            length++;
            continue;
        }

        if (valid) {
            expression.Instructions.RemoveFirst(); //see the definition of groupSections
            errorMsg = "";
            return true;
        } else {
            goto ReturnFail;
        }

        SyntaxError:
        syntaxErrMsg = "\nAborted -- syntax error: " + syntaxErrMsg;
        ReturnFail:
        bool semerrs = false;
        if (constsNotFound.Any()) {
            if (!semerrs) {
                semerrs = true;
                fullErrMsg += "\nSemantic errors:";
            }
            fullErrMsg += "\nThe following named constants don't exist: " + string.Join(", ", constsNotFound);
        }
        if (constsNotFound.Any()) {
            if (!semerrs) {
                semerrs = true;
                fullErrMsg += "\nSemantic errors:";
            }
            fullErrMsg += "\nThe following functions don't exist: " + string.Join(", ", funcsNotFound);
        }
        if (constsNotFound.Any()) {
            if (!semerrs) {
                semerrs = true;
                fullErrMsg += "\nSemantic errors:";
            }
            fullErrMsg += "\n" + string.Join("\n", funcArgMismatches.Select(mm => string.Format("Index ${0}: function '${1}' received ${2} arguments, expected ${3}", mm.Key, mm.Value.Name, mm.Value.ArgCount, mm.Value.FuncInfo.ParamCount + (mm.Value.FuncInfo.IsVararg ? "+" : ""))));
        }
        errorMsg = fullErrMsg + syntaxErrMsg;
        Logger.Log(LogLevel.Error, "MacroRoutingTool/ExpressionParser", errorMsg);
        expression = null;
        return false;
    }
}