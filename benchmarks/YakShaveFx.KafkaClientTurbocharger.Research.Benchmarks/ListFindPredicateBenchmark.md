```

BenchmarkDotNet v0.13.10, macOS Sonoma 14.2.1 (23C71) [Darwin 23.2.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.0 (8.0.23.53103), Arm64 RyuJIT AdvSIMD


```
| Method                          | ListSize | Mean          | Error       | StdDev      | Min           | Max           | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |--------- |--------------:|------------:|------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| **FindWithLambda**                  | **10**       |     **18.484 ns** |   **0.3207 ns** |   **0.2843 ns** |     **18.210 ns** |     **19.105 ns** |  **1.00** |    **0.00** |    **3** | **0.0140** |      **88 B** |        **1.00** |
| FindWithWrapperStructWithMethod | 10       |     69.429 ns |   1.4141 ns |   1.3228 ns |     67.536 ns |     71.723 ns |  3.75 |    0.04 |    5 | 0.0139 |      88 B |        1.00 |
| FindWithWrapperClassWithMethod  | 10       |     19.046 ns |   0.3997 ns |   0.4443 ns |     18.421 ns |     20.176 ns |  1.03 |    0.03 |    4 | 0.0140 |      88 B |        1.00 |
| FindWithForeach                 | 10       |     10.259 ns |   0.1551 ns |   0.1375 ns |     10.049 ns |     10.494 ns |  0.56 |    0.01 |    2 |      - |         - |        0.00 |
| FindWithFor                     | 10       |     10.108 ns |   0.1376 ns |   0.1220 ns |      9.972 ns |     10.336 ns |  0.55 |    0.01 |    2 |      - |         - |        0.00 |
| FindWithForeachOnSpan           | 10       |      4.134 ns |   0.0747 ns |   0.0583 ns |      4.025 ns |      4.217 ns |  0.22 |    0.00 |    1 |      - |         - |        0.00 |
| FindWithForOnSpan               | 10       |      4.203 ns |   0.0981 ns |   0.1438 ns |      4.036 ns |      4.544 ns |  0.23 |    0.01 |    1 |      - |         - |        0.00 |
|                                 |          |               |             |             |               |               |       |         |      |        |           |             |
| **FindWithLambda**                  | **100**      |     **96.959 ns** |   **1.5350 ns** |   **1.1984 ns** |     **95.454 ns** |     **98.959 ns** |  **1.00** |    **0.00** |    **4** | **0.0139** |      **88 B** |        **1.00** |
| FindWithWrapperStructWithMethod | 100      |    241.293 ns |   1.0288 ns |   0.8591 ns |    239.967 ns |    242.877 ns |  2.49 |    0.03 |    5 | 0.0138 |      88 B |        1.00 |
| FindWithWrapperClassWithMethod  | 100      |     96.845 ns |   1.6230 ns |   1.3552 ns |     95.316 ns |     99.738 ns |  1.00 |    0.02 |    4 | 0.0139 |      88 B |        1.00 |
| FindWithForeach                 | 100      |     59.145 ns |   0.9273 ns |   1.2379 ns |     57.710 ns |     62.087 ns |  0.62 |    0.02 |    2 |      - |         - |        0.00 |
| FindWithFor                     | 100      |     62.396 ns |   1.2852 ns |   1.6712 ns |     60.919 ns |     67.195 ns |  0.65 |    0.02 |    3 |      - |         - |        0.00 |
| FindWithForeachOnSpan           | 100      |     33.056 ns |   0.4953 ns |   0.6613 ns |     32.057 ns |     34.510 ns |  0.35 |    0.01 |    1 |      - |         - |        0.00 |
| FindWithForOnSpan               | 100      |     32.490 ns |   0.3739 ns |   0.3122 ns |     31.872 ns |     33.058 ns |  0.34 |    0.01 |    1 |      - |         - |        0.00 |
|                                 |          |               |             |             |               |               |       |         |      |        |           |             |
| **FindWithLambda**                  | **1000**     |    **786.510 ns** |  **14.9516 ns** |  **13.9858 ns** |    **771.901 ns** |    **814.359 ns** |  **1.00** |    **0.00** |    **4** | **0.0134** |      **88 B** |        **1.00** |
| FindWithWrapperStructWithMethod | 1000     |  1,960.680 ns |  28.7026 ns |  26.8484 ns |  1,927.715 ns |  2,016.670 ns |  2.49 |    0.05 |    5 | 0.0114 |      88 B |        1.00 |
| FindWithWrapperClassWithMethod  | 1000     |    776.539 ns |   6.7014 ns |   5.5960 ns |    769.904 ns |    790.701 ns |  0.99 |    0.02 |    4 | 0.0134 |      88 B |        1.00 |
| FindWithForeach                 | 1000     |    531.191 ns |   9.9690 ns |  13.9751 ns |    512.869 ns |    555.042 ns |  0.67 |    0.03 |    2 |      - |         - |        0.00 |
| FindWithFor                     | 1000     |    649.990 ns |  10.9409 ns |  10.2341 ns |    637.105 ns |    671.230 ns |  0.83 |    0.02 |    3 |      - |         - |        0.00 |
| FindWithForeachOnSpan           | 1000     |    342.411 ns |   6.7101 ns |   7.4583 ns |    334.808 ns |    363.247 ns |  0.44 |    0.01 |    1 |      - |         - |        0.00 |
| FindWithForOnSpan               | 1000     |    338.880 ns |   6.4922 ns |   5.4213 ns |    334.131 ns |    352.428 ns |  0.43 |    0.01 |    1 |      - |         - |        0.00 |
|                                 |          |               |             |             |               |               |       |         |      |        |           |             |
| **FindWithLambda**                  | **10000**    |  **7,781.011 ns** | **153.5526 ns** | **210.1845 ns** |  **7,576.188 ns** |  **8,479.714 ns** |  **1.00** |    **0.00** |    **4** | **0.0076** |      **88 B** |        **1.00** |
| FindWithWrapperStructWithMethod | 10000    | 19,442.105 ns | 342.6057 ns | 512.7960 ns | 18,773.079 ns | 20,789.794 ns |  2.50 |    0.10 |    5 |      - |      88 B |        1.00 |
| FindWithWrapperClassWithMethod  | 10000    |  7,738.775 ns | 105.1500 ns |  93.2127 ns |  7,617.210 ns |  7,934.842 ns |  0.99 |    0.03 |    4 |      - |      88 B |        1.00 |
| FindWithForeach                 | 10000    |  5,140.467 ns |  86.3044 ns |  80.7292 ns |  5,056.481 ns |  5,283.017 ns |  0.66 |    0.02 |    2 |      - |         - |        0.00 |
| FindWithFor                     | 10000    |  6,392.087 ns |  81.4016 ns |  76.1431 ns |  6,286.171 ns |  6,535.115 ns |  0.82 |    0.03 |    3 |      - |         - |        0.00 |
| FindWithForeachOnSpan           | 10000    |  3,214.347 ns |  40.1176 ns |  37.5261 ns |  3,150.445 ns |  3,283.983 ns |  0.41 |    0.01 |    1 |      - |         - |        0.00 |
| FindWithForOnSpan               | 10000    |  3,231.517 ns |  57.9064 ns |  51.3325 ns |  3,140.871 ns |  3,315.270 ns |  0.41 |    0.01 |    1 |      - |         - |        0.00 |
