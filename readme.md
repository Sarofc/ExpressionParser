# ExpressionParser

## 环境

- .net 6
- use unsafe and span

## 如何使用

```csharp
var result = Saro.Expression.ExpressionParser.Eval("cos(900-3*10*30)+123.45+30*30-0.45+tan(0)");

// or
var exp = new Saro.Expression.ExpressionParser();
exp["v1"] = 900f;
exp["v2"] = 123.45f;
exp["v3"] = 0f;
var result1 = exp.Evalute("cos(v1-3*10*30)+v2+30*30-0.45+tan(v3)");

// set custom func
Saro.Expression.ExpressionParser.SetFunc("floor", (x, _) => (float)Math.Floor(x), 1);

// set const variable
Saro.Expression.ExpressionParser.SetConst("e", (float)Math.E);
```

## 性能

TestCase
---
See [BenchmarkExpr.cs](https://github.com/Sarofc/ExpressionParser/blob/master/ExpressionParser.Sample/BenchmarkExpr.cs)

Result
---
| Method                      |      Mean |     Error |    StdDev |     Gen 0 |    Allocated |
| --------------------------- | --------: | --------: | --------: | --------: | -----------: |
| B83_ExpressionParser        | 53.482 ms | 0.6009 ms | 0.5327 ms | 7900.0000 | 16,696,048 B |
| Saro_ExpressionParser       |  5.615 ms | 0.0733 ms | 0.0650 ms | 3718.7500 |  7,784,004 B |
| Z_Expression                |  5.337 ms | 0.0290 ms | 0.0271 ms |  984.3750 |  2,072,003 B |
| Saro_ExpressionParser_Cache |  5.046 ms | 0.0252 ms | 0.0210 ms |         - |          4 B |