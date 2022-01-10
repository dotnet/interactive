Performance test results
========================

# Machine info (under @brettfo's desk)

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.200-preview.21617.4
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-AIICIK : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
```

## 2022-01-10, Version 1.0.260601 (notebook parsing as a separate service)

|                      Method |       Mean |    Error |   StdDev |
|---------------------------- |-----------:|---------:|---------:|
|     ProcessStartToQuickInfo | 3,290.8 ms | 30.50 ms | 27.04 ms |
| ProcessStartToParseNotebook |   443.4 ms |  8.73 ms | 14.58 ms |

## 2021-09-09, Version 1.0.245901 (notebook parsing as a command)

|                      Method |    Mean |    Error |   StdDev |  Median |
|---------------------------- |--------:|---------:|---------:|--------:|
|     ProcessStartToQuickInfo | 3.227 s | 0.0636 s | 0.1487 s | 3.165 s |
| ProcessStartToParseNotebook | 1.472 s | 0.0450 s | 0.1278 s | 1.458 s |
