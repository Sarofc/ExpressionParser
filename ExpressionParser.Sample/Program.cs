#define BENCH

using BenchmarkDotNet.Running;
using ExpressionParser = B83.ExpressionParser.ExpressionParser;
using Saro.Expression;

#if BENCH
BenchmarkRunner.Run<BenchmarkExpr>();
return;
#endif

var (testExpr, result) = (("cos(900-3*10*30)+123.45+30*30-0.45+tan(0)", 1024));
// 900,3,10,*,30,*,-,COS,123.45,+,30,30,*,+,0.45,-,0,TAN,+
var (testExpr1, v1, v2, v3, result1) = (("cos(v1-3*10*30)+v2+30*30-0.45+tan(v3)", 900, 123.45, 0, 1024));

//var (testExpr, result) = ("3+4*2/(5-3)^2+3*(4-2)", 11d);
// 3,4,2,*,5,3,-,2,^,/,+,3,4,2,-,*,+
Console.WriteLine($"ExpressionParser: {testExpr} : {ExpressionParser.Eval(testExpr)} == {result}");
Console.WriteLine($"UnsafeExpressionTree: {testExpr} : {Saro.Expression.ExpressionParser.Eval(testExpr)} == {result}");

var exp = new ExpressionParser();
exp.AddConst("v1", () => v1);
exp.AddConst("v2", () => v2);
exp.AddConst("v3", () => v3);
Console.WriteLine($"ExpressionParser: {testExpr1} : {exp.Evaluate(testExpr1)} == {result1}");

var unsafeExp = new Saro.Expression.ExpressionParser();
unsafeExp["v1"] = v1;
unsafeExp["v2"] = (float)v2;
unsafeExp["v3"] = v3;
Console.WriteLine($"UnsafeExpressionTree: {testExpr1} : {unsafeExp.Evalute(testExpr1)} == {result1}");


Console.ReadKey();