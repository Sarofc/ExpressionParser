using BenchmarkDotNet.Attributes;
using Saro.Expression;
using Z.Expressions;
using MidExpressionParser = B83.ExpressionParser.ExpressionParser;
using RPNExpressionParser = Saro.Expression.ExpressionParser;

[MemoryDiagnoser]
public class BenchmarkExpr
{
    IList<(string, double)> m_TestExpr = new List<(string, double)>
    {
        (("(2 + 4) * 3", 18f)),
        (("(2 + 4) / 3", 2f)),
        (("3+4*2/(5-3)^2+3*(4-2)", 11d)),
        (("cos(900-3*10*30)+123.45+30*30-0.45+tan(0)", 1024)),
        //("(2 + pi) * 3", 15.424777d),
    };

    private int m_LoopCount = 1000;

    private RPNExpressionParser m_RPNExp = new RPNExpressionParser(48);

    [GlobalSetup]
    public void Setup()
    {
        //m_UnsafeExp.Evalute(m_TestExpr[0].Item1);
        //Z.Expressions.Eval.Execute<double>(m_TestExpr[0].Item1);
    }

    [Benchmark]
    public void B83_ExpressionParser()
    {
        for (int i = 0; i < m_LoopCount; i++)
        {
            for (int i1 = 0; i1 < m_TestExpr.Count; i1++)
            {
                (string, double) item = m_TestExpr[i1];
                var result = MidExpressionParser.Eval(item.Item1);
            }
        }
    }

    [Benchmark]
    public void Saro_ExpressionParser()
    {
        for (int i = 0; i < m_LoopCount; i++)
        {
            for (int i1 = 0; i1 < m_TestExpr.Count; i1++)
            {
                (string, double) item = m_TestExpr[i1];
                var result = RPNExpressionParser.Eval(item.Item1);
            }
        }
    }

    [Benchmark]
    public void Z_Expression()
    {
        for (int i = 0; i < m_LoopCount; i++)
        {
            for (int i1 = 0; i1 < m_TestExpr.Count; i1++)
            {
                (string, double) item = m_TestExpr[i1];
                var exp = Eval.Execute<double>(item.Item1);
            }
        }
    }

    [Benchmark]
    public void Saro_ExpressionParser_Cache()
    {
        for (int i = 0; i < m_LoopCount; i++)
        {
            for (int i1 = 0; i1 < m_TestExpr.Count; i1++)
            {
                (string, double) item = m_TestExpr[i1];
                var result = m_RPNExp.Evalute(item.Item1);
            }
        }
    }
}
