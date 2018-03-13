``` ini

BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 2 [1703, Creators Update] (10.0.15063.850)
Intel Core i7-6500U CPU 2.50GHz (Skylake), 1 CPU, 4 logical cores and 2 physical cores
Frequency=2531251 Hz, Resolution=395.0616 ns, Timer=TSC
  [Host]     : .NET Framework 4.6.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2115.0  [AttachedDebugger]
  DefaultJob : .NET Framework 4.6.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2115.0


```
|           Method |       Mean |      Error |     StdDev |     Median |
|----------------- |-----------:|-----------:|-----------:|-----------:|
|        InsertADO | 214.770 ms | 19.1165 ms | 56.3656 ms | 216.004 ms |
|        UpdateADO | 190.411 ms | 17.4957 ms | 51.5863 ms | 174.565 ms |
|        SelectADO | 115.190 ms |  5.0756 ms | 14.6442 ms | 110.837 ms |
|    SelectListADO |   2.849 ms |  0.0661 ms |  0.1908 ms |   2.800 ms |
|       InsertVega | 140.862 ms |  3.6547 ms | 10.3081 ms | 139.770 ms |
|       UpdateVega | 130.668 ms |  2.4800 ms |  2.1985 ms | 130.596 ms |
|       SelectVega | 109.210 ms |  2.3608 ms |  4.6600 ms | 106.875 ms |
|   SelectListVega |   2.900 ms |  0.0552 ms |  0.0489 ms |   2.919 ms |
|     InsertDapper | 117.883 ms |  2.3551 ms |  2.4185 ms | 116.426 ms |
|     UpdateDapper | 119.415 ms |  2.3864 ms |  2.5534 ms | 119.255 ms |
|     SelectDapper | 109.603 ms |  2.8505 ms |  8.1325 ms | 107.702 ms |
| SelectListDapper |   3.085 ms |  0.0691 ms |  0.1441 ms |   3.044 ms |
