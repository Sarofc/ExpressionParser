using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Saro.Expression
{
    public sealed class ExpressionParser
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FString : IEquatable<FString>
        {
            public int size;
            public fixed char buffer[k_MaxFStringLength];

            public override string ToString()
            {
                fixed (char* pBuffer = buffer)
                {
                    var result = new ReadOnlySpan<char>(pBuffer, size);
                    return result.ToString();
                }
            }

            public bool Equals(FString other)
            {
                if (size != other.size) return false;

                for (int i = 0; i < size; i++)
                {
                    if (buffer[i] != other.buffer[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object? obj)
            {
                if (obj is FString str)
                {
                    return Equals(str);
                }

                return false;
            }

            public override int GetHashCode()
            {
                fixed (char* pBuffer = buffer)
                {
                    return (int)Hash((byte*)pBuffer, size * sizeof(char));
                }
            }

            // <summary>
            /// Returns a (non-cryptographic) hash of a memory block.
            /// </summary>
            /// <remarks>The hash function used is [djb2](http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html).</remarks>
            /// <param name="ptr">A buffer.</param>
            /// <param name="bytes">The number of bytes to hash.</param>
            /// <returns>A hash of the bytes.</returns>
            public static uint Hash(void* ptr, int bytes)
            {
                // djb2 - Dan Bernstein hash function
                // http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html
                byte* str = (byte*)ptr;
                ulong hash = 5381;
                while (bytes > 0)
                {
                    ulong c = str[--bytes];
                    hash = ((hash << 5) + hash) + c;
                }
                return (uint)hash;
            }

            public static bool operator ==(FString left, FString right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(FString left, FString right)
            {
                return !(left == right);
            }
        }

        public const int k_MaxFStringLength = 12;

        private readonly static Dictionary<FString, (int priority, int argNum, Func<float, float, float> func)> s_OperatorMap = new Dictionary<FString, (int priority, int argNum, Func<float, float, float> func)>();

        private readonly Stack<FString> m_Stack1;
        private readonly Stack<FString> m_Stack2;

        /// <summary>
        /// get/set local variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public float this[string name]
        {
            get
            {
                FString str = default;
                PutString(name, ref str);
                return m_LocalVariableMap[str];
            }
            set
            {
                FString str = default;
                PutString(name, ref str);
                m_LocalVariableMap[str] = value;
            }
        }

        private readonly static Dictionary<FString, float> s_ConstVariableMap = new Dictionary<FString, float>();

        private Dictionary<FString, float> m_LocalVariableMap;
        private Stack<float> m_ResultStack;

        static ExpressionParser()
        {
            // operator
            FString add = default;
            FString sub = default;
            FString mul = default;
            FString div = default;
            FString pow = default;
            FString lb = default;
            FString rb = default;
            PutString('+', ref add);
            PutString('-', ref sub);
            PutString('*', ref mul);
            PutString('/', ref div);
            PutString('^', ref pow);
            PutString('(', ref lb);
            PutString(')', ref rb);
            s_OperatorMap.Add(add, (1, -1, (x, y) => x + y));
            s_OperatorMap.Add(sub, (1, -1, (x, y) => x - y));
            s_OperatorMap.Add(mul, (2, -1, (x, y) => x * y));
            s_OperatorMap.Add(div, (2, -1, (x, y) => x / y));
            s_OperatorMap.Add(pow, (3, -1, (x, y) => (float)Math.Pow(x, y)));
            s_OperatorMap.Add(lb, (100, -1, null));
            s_OperatorMap.Add(rb, (100, -1, null));

            // arg 1
            FString sin = default;
            FString cos = default;
            FString tan = default;
            FString sqrt = default;
            PutString("sin", ref sin);
            PutString("cos", ref cos);
            PutString("tan", ref tan);
            PutString("sqrt", ref sqrt);
            s_OperatorMap.Add(sin, (4, 1, (x, _) => (float)Math.Sin(x)));
            s_OperatorMap.Add(cos, (4, 1, (x, _) => (float)Math.Cos(x)));
            s_OperatorMap.Add(tan, (4, 1, (x, _) => (float)Math.Tan(x)));
            s_OperatorMap.Add(sqrt, (4, 1, (x, _) => (float)Math.Sqrt(x)));

            // arg 2
            FString rand = default;
            PutString("rand", ref rand);
            s_OperatorMap.Add(rand, (4, 2, (x, y) => x + (float)Random.Shared.NextDouble() * (y - x)));

            // global variables
            FString pi = default;
            PutString("pi", ref pi);
            s_ConstVariableMap.Add(pi, (float)Math.PI);

            //Console.WriteLine($"\ndic: {string.Join("\n", dic)}\n");
            //Console.WriteLine($"\nglobal: {string.Join("\n", s_OperatorMap)}");
        }

        public ExpressionParser() : this(16) { }

        public ExpressionParser(int capacity)
        {
            m_LocalVariableMap = new Dictionary<FString, float>();
            m_Stack1 = new Stack<FString>(capacity);
            m_Stack2 = new Stack<FString>(capacity);
            m_ResultStack = new Stack<float>(capacity);
        }

        public static float Eval(string expr)
        {
            var exp = new ExpressionParser();
            var ret = exp.ToRPN(expr);
            //if (ret == false) throw
            return exp.CalRPN(exp.m_Stack1);
        }

        /// <summary>
        /// 设置 自定义方法，支持0-2个参数
        /// </summary>
        /// <param name="funcName">方法名</param>
        /// <param name="func">方法委托</param>
        /// <param name="argNum">参数个数</param>
        public static void SetFunc(string funcName, Func<float, float, float> func, int argNum)
        {
            FString name = default;
            PutString(funcName, ref name);

            s_OperatorMap[name] = (4, argNum, func);
        }

        /// <summary>
        /// 设置 全局常量
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="val"></param>
        public static void SetConst(string variableName, float val)
        {
            FString name = default;
            PutString(variableName, ref name);

            s_ConstVariableMap[name] = val;
        }

        public float Evalute(string expr)
        {
            ToRPN(expr);
            return CalRPN(m_Stack1);
        }

        private unsafe bool ToRPN(string expr)
        {
            m_Stack1.Clear();
            m_Stack2.Clear();

            var spanExpr = expr.AsSpan();

            var pTmpChr = stackalloc char[k_MaxFStringLength];
            var tmpChrLen = 0;

            for (int i = 0; i < spanExpr.Length; i++)
            {
                var chr = spanExpr[i];
                if (chr == ' ')
                {
                    continue;
                }

                if (chr == '(')
                {
                    Push(m_Stack1, chr);
                }
                else if (chr == ')')
                {
                    // push nunber
                    if (tmpChrLen > 0)
                    {
                        Push(m_Stack2, new ReadOnlySpan<char>(pTmpChr, tmpChrLen));
                        tmpChrLen = 0;
                    }

                    while (true)
                    {
                        var top = m_Stack1.Pop();
                        if (top.buffer[0] == '(') break;
                        else m_Stack2.Push(top);
                    }

                    if (m_Stack1.Count > 0)
                    {
                        var top = m_Stack1.Peek();
                        if (IsFunction(ref top, out _))
                        {
                            m_Stack2.Push(m_Stack1.Pop());
                        }
                    }
                }
                else if (IsOperator(chr))
                {
                    // push nunber
                    if (tmpChrLen > 0)
                    {
                        Push(m_Stack2, new ReadOnlySpan<char>(pTmpChr, tmpChrLen));
                        tmpChrLen = 0;
                    }

                    FString opR = m_Stack1.Count > 0 ? m_Stack1.Peek() : default;
                    FString opL = default;
                    PutString(chr, ref opL);
                    if (m_Stack1.Count == 0 || (opR.size > 0 && (opR.buffer[0] == '(' || (ComOp(ref opL, ref opR) > 0))))
                    {
                        Push(m_Stack1, ref opL);
                    }
                    else
                    {
                        while (m_Stack1.Count > 0)
                        {
                            FString opR2 = m_Stack1.Peek();
                            FString opL2 = default;
                            PutString(chr, ref opL2);
                            if (opR2.buffer[0] == '(' || ComOp(ref opL2, ref opR2) > 0)
                            {
                                break;
                            }
                            else
                            {
                                var pop = m_Stack1.Pop();
                                Push(m_Stack2, ref pop);
                            }
                        }
                        Push(m_Stack1, chr);
                    }
                }
                else
                {
                    for (; i < spanExpr.Length; i++)
                    {
                        var newChr = spanExpr[i];

                        if (newChr == ' ')
                        {
                            continue;
                        }

                        if (char.IsUpper(newChr))
                        {
                            newChr = char.ToLower(newChr);
                        }

                        if (char.IsDigit(newChr) || newChr == '.')
                        {
                            if (tmpChrLen < k_MaxFStringLength)
                            {
                                *(pTmpChr + tmpChrLen) = newChr;
                                ++tmpChrLen;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (tmpChrLen == 0)
                    {
                        for (; i < spanExpr.Length; i++)
                        {
                            var newChr = spanExpr[i];

                            if (newChr == ' ')
                            {
                                continue;
                            }

                            if (newChr == ')' || newChr == '(' || IsOperator(newChr))
                            {
                                break;
                            }
                            else
                            {
                                if (tmpChrLen < k_MaxFStringLength)
                                {
                                    // 开启可以支持大小写无关，不然方法字符串，得严格按照小写
                                    //if (char.IsUpper(newChr))
                                    //{
                                    //    newChr = char.ToLower(newChr);
                                    //}

                                    *(pTmpChr + tmpChrLen) = newChr;
                                    ++tmpChrLen;
                                }
                            }
                        }

                        if (tmpChrLen > 0)
                        {
                            FString str = default;
                            PutString(new ReadOnlySpan<char>(pTmpChr, tmpChrLen), ref str);

                            // push custom func
                            if (IsFunction(ref str, out _))
                            {
                                Push(m_Stack1, ref str);
                                tmpChrLen = 0;
                            }
                            // push local parameter
                            else if (TryGetLocalParameter(ref str, out _))
                            {
                                Push(m_Stack2, ref str);
                                tmpChrLen = 0;
                            }
                            else if (TryGetConstParameter(ref str, out _))
                            {
                                Push(m_Stack2, ref str);
                                tmpChrLen = 0;
                            }

                            if (tmpChrLen > 0)
                            {
                                throw new ExpressionException($"unhandle str: {str}");
                            }
                        }
                    }
                    --i;
                }
            }

            if (tmpChrLen > 0)
                Push(m_Stack2, new ReadOnlySpan<char>(pTmpChr, tmpChrLen));

            while (m_Stack2.Count > 0)
            {
                var pop = m_Stack2.Pop();
                Push(m_Stack1, ref pop);
            }

#if DEBUG
            Console.WriteLine($"CalcRPN: {string.Join(",", m_Stack1)}");
#endif

            return true;
        }

        private unsafe float CalRPN(Stack<FString> input)
        {
            m_ResultStack.Clear();

            while (input.Count > 0)
            {
                var t = input.Pop();
                if (IsOperator(t.buffer[0]))
                {
                    var right = m_ResultStack.Pop();
                    var left = m_ResultStack.Pop();
                    m_ResultStack.Push(GetOpValue(t.buffer[0], left, right));
                }
                else if (IsFunction(ref t, out int argNum))
                {
                    if (argNum == 1)
                    {
                        var right = m_ResultStack.Pop();
                        m_ResultStack.Push(GetFuncValue(ref t, right, 0));
                    }
                    else if (argNum == 2)
                    {
                        var right = m_ResultStack.Pop();
                        var left = m_ResultStack.Pop();
                        m_ResultStack.Push(GetFuncValue(ref t, left, right));
                    }
                    else if (argNum == 0)
                    {
                        m_ResultStack.Push(GetFuncValue(ref t, 0, 0));
                    }
                }
                else
                {
                    if (TryGetLocalParameter(ref t, out float val1))
                    {
                        m_ResultStack.Push(val1);
                    }
                    else if (TryGetConstParameter(ref t, out float val2))
                    {
                        m_ResultStack.Push(val2);
                    }
                    else
                    {
                        var str = new ReadOnlySpan<char>(t.buffer, t.size);
                        m_ResultStack.Push(float.Parse(str));
                    }
                }
            }

            return m_ResultStack.Pop();
        }

        private static int ComOp(ref FString op1, ref FString op2)
        {
            return s_OperatorMap[op1].priority - s_OperatorMap[op2].priority;
        }

        private static float GetOpValue(char op, float val1, float val2)
        {
            //Console.WriteLine($"CalValue: {op} {val1} {val2}");

            float ret;
            switch (op)
            {
                case '+':
                    ret = val1 + val2;
                    break;
                case '-':
                    ret = val1 - val2;
                    break;
                case '*':
                    ret = val1 * val2;
                    break;
                case '/':
                    ret = val1 / val2;
                    break;
                case '^':
                    ret = (float)Math.Pow(val1, val2);
                    break;
                default:
                    throw new ExpressionException($"unhandle op: {op}");
            }
            return ret;
        }

        private static float GetFuncValue(ref FString str, float val1, float val2)
        {
            if (s_OperatorMap.TryGetValue(str, out var o))
            {
                var func = o.func;
                return func(val1, val2);
            }

            throw new ExpressionException($"func not found: {str}");
        }

        private static bool TryGetConstParameter(ref FString str, out float val)
        {
            return s_ConstVariableMap.TryGetValue(str, out val);
        }

        private bool TryGetLocalParameter(ref FString str, out float val)
        {
            return m_LocalVariableMap.TryGetValue(str, out val);
        }

        private static bool IsFunction(ref FString str, out int argNum)
        {
            if (s_OperatorMap.TryGetValue(str, out var o))
            {
                argNum = o.argNum;
                return o.priority == 4;
            }

            argNum = -1;
            return false;
        }

        private static bool IsOperator(char chr)
        {
            bool ret;
            switch (chr)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '^':
                case '(':
                case ')':
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;
        }

        private unsafe static void PutString(ReadOnlySpan<char> chrs, ref FString fString)
        {
            fString.size = chrs.Length;
            var bytesSize = chrs.Length * sizeof(char);
            fixed (char* pBuffer = fString.buffer)
            fixed (char* pChrs = chrs)
            {
                Buffer.MemoryCopy(pChrs, pBuffer, bytesSize, bytesSize);
            }
        }

        private unsafe static void PutString(char chr, ref FString fString)
        {
            fString.size = 1;
            fixed (char* pBuffer = fString.buffer)
            {
                *pBuffer = chr;
            }
        }

        private static void Push(Stack<FString> stack, ref FString name)
        {
            stack.Push(name);
        }

        private static void Push(Stack<FString> stack, ReadOnlySpan<char> chrs)
        {
            FString name = default;
            PutString(chrs, ref name);
            stack.Push(name);
        }

        private static void Push(Stack<FString> stack, char chr)
        {
            FString name = default;
            PutString(chr, ref name);
            stack.Push(name);
        }
    }

    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message)
        {
        }
    }
}