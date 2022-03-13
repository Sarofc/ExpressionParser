using NUnit.Framework;
using Saro.Expression;
using System;
using System.Collections.Generic;

namespace ExpressionParser.Test
{
    using MidExpressionParser = B83.ExpressionParser.ExpressionParser;
    using RPNExpressionParser = Saro.Expression.ExpressionParser;

    public class ExpressionParserTest
    {
        static IEnumerable<(string expr, double result)> m_TestExpr = new List<(string expr, double result)>
        {
            ("(2 + 4)", 6d),
            ("(2 + 4) / 3", 2d),
            ("(2 + 4) * 3", 18d),
            ("(2 + floor(5.4)) * 3", 21d),
            ("(2 + pi) * 3", 15.424777d),
            ("2 * (0.5 + pi + 1.5) * 1.5", 15.424777d),
            ("floor(2 * (0.5 + pi + 1.5) * 1.5)", 15d),
            ("3+4*2/(5-3)^2+3*(4-2)", 11d),
            ("cos(900-3*10*30)+123.45+30*30-0.45+tan(0)", 1024),
            ("e * 2", (float)Math.E * 2),
        };
        static IEnumerable<(string expr, double v1, double v2, double v3, double result)> m_TestExpr_LocalParameter = new List<(string expr, double v1, double v2, double v3, double result)>
        {
            ("v1+4*2/(v2-3)^2+3*(v3-2)", 3, 5, 4, 11d),
            ("cos(v1-3*10*30)+v2+30*30-0.45+tan(v3)", 900, 123.45, 0, 1024),
        };

        [SetUp]
        public void Setup()
        {
            RPNExpressionParser.SetFunc("floor", (x, _) => (float)Math.Floor(x), 1);
            Saro.Expression.ExpressionParser.SetConst("e", (float)Math.E);
        }

        [Test]
        [TestCaseSource(nameof(m_TestExpr))]
        public void Parser_Mid((string expr, double result) item)
        {
            var result = MidExpressionParser.Eval(item.expr);
            Assert.AreEqual(item.result, result, 0.0001f, $"{item.expr}");
        }

        [Test]
        [TestCaseSource(nameof(m_TestExpr))]
        public void Parser_RPN((string expr, double result) item)
        {
            var result = RPNExpressionParser.Eval(item.expr);
            Assert.AreEqual(item.result, result, 0.0001f, $"{item.expr}");
        }

        [Test]
        [TestCaseSource(nameof(m_TestExpr_LocalParameter))]
        public void Parser_Mid_LocalParameter((string expr, double v1, double v2, double v3, double result) item)
        {
            var exp = new MidExpressionParser();
            exp.AddConst("v1", () => item.v1);
            exp.AddConst("v2", () => item.v2);
            exp.AddConst("v3", () => item.v3);
            var result = exp.Evaluate(item.expr);
            Assert.AreEqual(item.result, result, 0.0001f, $"{item.expr}");
        }

        [Test]
        [TestCaseSource(nameof(m_TestExpr_LocalParameter))]
        public void Parser_RPN_Unsafe_LocalParameter((string expr, double v1, double v2, double v3, double result) item)
        {
            var exp = new RPNExpressionParser();
            exp["v1"] = (float)item.v1;
            exp["v2"] = (float)item.v2;
            exp["v3"] = (float)item.v3;
            var result = exp.Evalute(item.expr);
            Assert.AreEqual(item.result, result, 0.0001f, $"{item.expr}");
        }
    }
}