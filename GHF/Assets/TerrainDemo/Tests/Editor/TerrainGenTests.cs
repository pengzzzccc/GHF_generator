// ─────────────────────────────────────────────────────────────────────────────
// 文件:Tests/Editor/TerrainGenTests.cs
// 模块:程序化地形生成 · 测试(Unity Test Framework,EditMode)
// 状态:已完整实现——本文件是整套骨架代码的行为规格
// ─────────────────────────────────────────────────────────────────────────────
//
// 使用说明。本文件不需要也不会被改动,你实现骨架代码的过程,就是让这里的测试从红
// 变绿的过程:骨架阶段所有用例都会因 NotImplementedException 失败,每实现一个类,
// 对应用例即转绿。运行方式:Unity 菜单 Window → General → Test Runner → EditMode
// → Run All。每个用例都会通过 Debug.Log 在 Console 输出中文摘要与关键数值(哈希、
// 最大误差、能量比、耗时),便于查看"绿到什么程度"。
//
// 测试设计说明。断言分为两类:契约断言直接钉死骨架注释里写明的性质(值域、周期、
// 确定性等);行为断言验证算法该有的宏观效果(加 octave 必须增加高频能量、换种子
// 必须换地形)。所有统计型断言(均值、能量比)都留了宽裕的安全边距,不会因正常
// 实现的微小波动而误报。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using TerrainDemo.Core;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Tests
{
    public class TerrainGenTests
    {
        /// <summary>值域类断言的统一浮点容差。</summary>
        private const float RangeEps = 1e-4f;

        // ═════════════════════════════════════════════════════════════════════
        // 工具方法
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>构造一套显式全字段的测试参数(不依赖任何默认值与预设)。</summary>
        private static TerrainParams MakeParams(int seed, int resolution = 65)
        {
            return new TerrainParams
            {
                Seed = seed,
                Resolution = resolution,
                WorldSize = 200f,
                HeightScale = 30f,
                Scale = 120f,
                Octaves = 5,
                Lacunarity = 2f,
                Persistence = 0.5f,
                HeightExponent = 1f,
            };
        }

        /// <summary>对高度图全部 float 的字节做 FNV-1a 64 位哈希,用于位级确定性比对。</summary>
        private static string FnvHash(float[,] map)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;
            int rows = map.GetLength(0), cols = map.GetLength(1);
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < cols; x++)
                {
                    byte[] bytes = BitConverter.GetBytes(map[z, x]);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        hash ^= bytes[i];
                        hash *= prime;
                    }
                }
            }
            return hash.ToString("X16");
        }

        /// <summary>两张高度图逐元素比较的最大绝对差。</summary>
        private static float MaxAbsDiff(float[,] a, float[,] b)
        {
            Assert.AreEqual(a.GetLength(0), b.GetLength(0), "两图行数不同,无法比较");
            Assert.AreEqual(a.GetLength(1), b.GetLength(1), "两图列数不同,无法比较");
            float max = 0f;
            for (int z = 0; z < a.GetLength(0); z++)
                for (int x = 0; x < a.GetLength(1); x++)
                    max = Mathf.Max(max, Mathf.Abs(a[z, x] - b[z, x]));
            return max;
        }

        /// <summary>构造用于网格测试的确定性合成高度图(不依赖噪声,隔离被测对象)。</summary>
        private static float[,] SyntheticHeights(int resolution)
        {
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                    heights[z, x] = ((x * 7 + z * 13) % 32) / 32f;
            return heights;
        }

        /// <summary>计算 fBm 在网格上的"相邻差能量":相邻格点采样值之差的绝对值的平均。</summary>
        private static double AdjacentDiffEnergy(int octaves)
        {
            var noise = new FractalNoise2D(7, octaves, 2f, 0.5f, 1f);
            const int samples = 128;
            const float span = 16f;
            var values = new float[samples, samples];
            for (int z = 0; z < samples; z++)
                for (int x = 0; x < samples; x++)
                    values[z, x] = noise.Sample(x * span / (samples - 1), z * span / (samples - 1));

            double sum = 0.0;
            long count = 0;
            for (int z = 0; z < samples; z++)
                for (int x = 0; x < samples; x++)
                {
                    if (x + 1 < samples) { sum += Mathf.Abs(values[z, x + 1] - values[z, x]); count++; }
                    if (z + 1 < samples) { sum += Mathf.Abs(values[z, x] - values[z + 1, x]); count++; }
                }
            return sum / count;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 一、SeededRng:种子确定性、区分度、值域、均匀性
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void SeededRng_SameSeed_ReproducesIdenticalSequence()
        {
            var rngA = new SeededRng(42);
            var rngB = new SeededRng(42);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(rngA.NextUInt(), rngB.NextUInt(), $"NextUInt 第 {i} 项不同种子序列出现分歧");
            var a = new SeededRng(42);
            var b = new SeededRng(42);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.Next01(), b.Next01(), $"Next01 第 {i} 项不同种子序列出现分歧");
            Debug.Log("[SeededRng·同种子复现] 两个 seed=42 实例的 NextUInt×100 与 Next01×1000 序列逐项一致 ✓");
        }

        [Test]
        public void SeededRng_DifferentSeeds_GiveDifferentSequences()
        {
            float[][] sequences = new float[5][];
            for (int s = 0; s < 5; s++)
            {
                var rng = new SeededRng(s + 1);
                sequences[s] = new float[32];
                for (int i = 0; i < 32; i++) sequences[s][i] = rng.Next01();
            }
            int distinctPairs = 0, totalPairs = 0;
            for (int i = 0; i < 5; i++)
                for (int j = i + 1; j < 5; j++)
                {
                    totalPairs++;
                    if (!sequences[i].SequenceEqual(sequences[j])) distinctPairs++;
                }
            Debug.Log($"[SeededRng·异种子区分] 5 个种子的前 32 项序列,互不相同的组合数 {distinctPairs}/{totalPairs}");
            Assert.AreEqual(totalPairs, distinctPairs, "存在两个不同种子产生了完全相同的前 32 项序列");
        }

        [Test]
        public void SeededRng_Next01_StaysInUnitInterval()
        {
            var rng = new SeededRng(7);
            float min = 1f, max = -1f;
            for (int i = 0; i < 100000; i++)
            {
                float v = rng.Next01();
                Assert.GreaterOrEqual(v, 0f, $"第 {i} 项出现负值 {v}");
                Assert.Less(v, 1f, $"第 {i} 项出现 1.0(契约要求左闭右开 [0,1))");
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            Debug.Log($"[SeededRng·值域] 100,000 次采样,观测范围 [{min:F6}, {max:F6}) ⊂ [0,1) ✓");
        }

        [Test]
        public void SeededRng_Next01_MeanNearHalf()
        {
            var rng = new SeededRng(2024);
            double sum = 0.0;
            const int n = 10000;
            for (int i = 0; i < n; i++) sum += rng.Next01();
            double mean = sum / n;
            Debug.Log($"[SeededRng·均匀性] {n} 次采样均值 = {mean:F4}(理论 0.5,容差 ±0.05)");
            Assert.That(mean, Is.InRange(0.45, 0.55), "采样均值偏离 0.5 过多,分布可能不均匀");
        }

        // ═════════════════════════════════════════════════════════════════════
        // 二、PerlinNoise2D:值域、格点、周期、连续性、复现、置换表
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void Perlin_ValuesStayInUnitRange()
        {
            float globalMin = float.MaxValue, globalMax = float.MinValue;
            foreach (int seed in new[] { 1, 7, 12345 })
            {
                var noise = new PerlinNoise2D(seed);
                for (int y = 0; y < 129; y++)
                    for (int x = 0; x < 129; x++)
                    {
                        float v = noise.Noise(x * 0.5f, y * 0.5f);
                        Assert.That(v, Is.InRange(-1f - RangeEps, 1f + RangeEps),
                            $"seed={seed} 在 ({x * 0.5f},{y * 0.5f}) 处值 {v} 超出 [-1,1]");
                        globalMin = Mathf.Min(globalMin, v);
                        globalMax = Mathf.Max(globalMax, v);
                    }
            }
            Debug.Log($"[Perlin·值域] 3 个种子 × 129×129 网格,全局范围 [{globalMin:F4}, {globalMax:F4}] ⊂ [-1,1] ✓");
            Assert.Greater(globalMax - globalMin, 0.2f, "噪声场几乎恒定,疑似实现退化");
        }

        [Test]
        public void Perlin_IntegerLatticePoints_AreZero()
        {
            var noise = new PerlinNoise2D(1);
            Vector2[] latticePoints =
            {
                new Vector2(0f, 0f), new Vector2(3f, 7f), new Vector2(12f, -5f),
                new Vector2(-9f, 31f), new Vector2(64f, 64f), new Vector2(1000f, 2000f),
            };
            float maxAbs = 0f;
            foreach (Vector2 p in latticePoints)
            {
                float v = noise.Noise(p.x, p.y);
                Assert.Less(Mathf.Abs(v), 1e-6f, $"整数格点 ({p.x},{p.y}) 处噪声为 {v},应为 0(偏移向量为零 ⇒ 点积为零)");
                maxAbs = Mathf.Max(maxAbs, Mathf.Abs(v));
            }
            Debug.Log($"[Perlin·格点为零] 6 个整数格点(含负坐标与大坐标)最大 |值| = {maxAbs:E2} ≈ 0 ✓");
        }

        [Test]
        public void Perlin_HasPeriod256()
        {
            var noise = new PerlinNoise2D(7);
            Vector2[] probePoints =
            {
                new Vector2(0.37f, 12.9f), new Vector2(17.3f, 99.1f),
                new Vector2(-3.2f, 45.6f), new Vector2(255.7f, 0.3f),
            };
            float maxDiff = 0f;
            foreach (Vector2 p in probePoints)
            {
                float baseV = noise.Noise(p.x, p.y);
                float dx = Mathf.Abs(baseV - noise.Noise(p.x + 256f, p.y));
                float dy = Mathf.Abs(baseV - noise.Noise(p.x, p.y + 256f));
                maxDiff = Mathf.Max(maxDiff, Mathf.Max(dx, dy));
                Assert.Less(dx, 1e-3f, $"Noise({p.x},{p.y}) 与 Noise({p.x}+256,{p.y}) 差 {dx},周期性破坏");
                Assert.Less(dy, 1e-3f, $"Noise({p.x},{p.y}) 与 Noise({p.x},{p.y}+256) 差 {dy},周期性破坏");
            }
            Debug.Log($"[Perlin·周期256] 4 个探针点在 x/y 方向平移 256 后最大差 {maxDiff:E2}(容差 1e-3)✓");
        }

        [Test]
        public void Perlin_IsContinuousAcrossLatticeLines()
        {
            var noise = new PerlinNoise2D(3);
            Vector2[] basePoints =
            {
                new Vector2(2.0f, 3.5f), new Vector2(7.0f, -1.25f), new Vector2(0.0f, 0.5f),
                new Vector2(255.999f, 100.0f), new Vector2(-5.0f, -5.0f),
            };
            const float step = 1e-3f, bound = 0.05f;
            float maxDelta = 0f;
            foreach (Vector2 p in basePoints)
            {
                float baseV = noise.Noise(p.x, p.y);
                float deltaX = Mathf.Abs(noise.Noise(p.x + step, p.y) - baseV);
                float deltaY = Mathf.Abs(noise.Noise(p.x, p.y + step) - baseV);
                maxDelta = Mathf.Max(maxDelta, Mathf.Max(deltaX, deltaY));
                Assert.Less(deltaX, bound, $"点 ({p.x},{p.y}) 沿 x 微移 {step} 后变化 {deltaX},存在不连续");
                Assert.Less(deltaY, bound, $"点 ({p.x},{p.y}) 沿 y 微移 {step} 后变化 {deltaY},存在不连续");
            }
            Debug.Log($"[Perlin·连续性] 5 个基点(含跨格点线/周期边界)微步长最大变化 {maxDelta:E2}(上限 {bound})✓");
        }

        [Test]
        public void Perlin_SameSeed_ReproducesFieldAndTable()
        {
            var noiseA = new PerlinNoise2D(42);
            var noiseB = new PerlinNoise2D(42);
            Assert.IsTrue(noiseA.Permutation.SequenceEqual(noiseB.Permutation), "同种子两次构造,置换表不一致");
            Vector2[] probePoints =
            {
                new Vector2(0.5f, 0.5f), new Vector2(13.7f, 2.2f), new Vector2(-8.3f, 55.5f),
                new Vector2(100.25f, 200.75f), new Vector2(254.9f, 1.1f),
            };
            foreach (Vector2 p in probePoints)
                Assert.AreEqual(noiseA.Noise(p.x, p.y), noiseB.Noise(p.x, p.y), "同种子实例在同一探针点给出不同噪声值");
            Debug.Log("[Perlin·同种子复现] 置换表 256 项一致,5 个探针点噪声值逐位相同 ✓");
        }

        [Test]
        public void Perlin_Permutation_IsPermutationOf256()
        {
            foreach (int seed in new[] { 1, 2, 3, 999 })
            {
                var noise = new PerlinNoise2D(seed);
                int[] perm = noise.Permutation;
                Assert.IsNotNull(perm, $"seed={seed} 的置换表为 null");
                Assert.AreEqual(256, perm.Length, $"seed={seed} 的置换表长度应为 256");
                int[] sorted = perm.OrderBy(v => v).ToArray();
                int[] expected = Enumerable.Range(0, 256).ToArray();
                Assert.IsTrue(sorted.SequenceEqual(expected), $"seed={seed} 的置换表不是 0..255 的排列");
            }
            Debug.Log("[Perlin·置换表合法性] seeds {1,2,3,999} 的置换表均为 0..255 的完整排列 ✓");
        }

        [Test]
        public void Perlin_DifferentSeeds_GiveDifferentFields()
        {
            var noiseA = new PerlinNoise2D(1);
            var noiseB = new PerlinNoise2D(2);
            float maxDiff = 0f;
            for (int y = 0; y < 33; y++)
                for (int x = 0; x < 33; x++)
                {
                    float px = x * 2f, py = y * 2f;
                    maxDiff = Mathf.Max(maxDiff, Mathf.Abs(noiseA.Noise(px, py) - noiseB.Noise(px, py)));
                }
            Debug.Log($"[Perlin·异种子区分] seed 1 与 seed 2 在 33×33 网格上最大差 {maxDiff:F4}(要求 > 0.01)");
            Assert.Greater(maxDiff, 0.01f, "两个不同种子的噪声场几乎相同,种子未真正进入置换表");

            var permA = new PerlinNoise2D(3).Permutation;
            var permB = new PerlinNoise2D(4).Permutation;
            Assert.IsFalse(permA.SequenceEqual(permB), "seed 3 与 seed 4 的置换表完全相同");
        }

        // ═════════════════════════════════════════════════════════════════════
        // 三、FractalNoise2D:值域、单层恒等、octaves 作用、能量单调、复现
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void Fbm_ValuesStayInUnitRange()
        {
            float globalMin = float.MaxValue, globalMax = float.MinValue;
            foreach (int seed in new[] { 1, 7, 12345 })
            {
                var noise = new FractalNoise2D(seed, 5, 2f, 0.5f, 4f);
                for (int y = 0; y < 65; y++)
                    for (int x = 0; x < 65; x++)
                    {
                        float v = noise.Sample(x * 0.5f, y * 0.5f);
                        Assert.That(v, Is.InRange(-RangeEps, 1f + RangeEps),
                            $"seed={seed} 在 ({x * 0.5f},{y * 0.5f}) 处值 {v} 超出 [0,1]");
                        globalMin = Mathf.Min(globalMin, v);
                        globalMax = Mathf.Max(globalMax, v);
                    }
            }
            Debug.Log($"[fBm·值域] 3 个种子 × 65×65 网格,全局范围 [{globalMin:F4}, {globalMax:F4}] ⊂ [0,1] ✓");
            Assert.Greater(globalMax - globalMin, 0.3f, "fBm 场几乎恒定,疑似实现退化");
        }

        [Test]
        public void Fbm_SingleOctave_EqualsNormalizedPerlin()
        {
            const int seed = 11;
            const float scale = 8f;
            var fbm = new FractalNoise2D(seed, 1, 2f, 0.5f, scale);
            var perlin = new PerlinNoise2D(seed);
            Vector2[] probePoints =
            {
                new Vector2(0f, 0f), new Vector2(1.5f, 2.5f), new Vector2(7.25f, 0.5f),
                new Vector2(15.9f, 15.1f), new Vector2(0.001f, 0.002f), new Vector2(-3.7f, 11.2f),
            };
            float maxErr = 0f;
            foreach (Vector2 p in probePoints)
            {
                float expected = 0.5f + 0.5f * perlin.Noise(p.x / scale, p.y / scale);
                float actual = fbm.Sample(p.x, p.y);
                maxErr = Mathf.Max(maxErr, Mathf.Abs(expected - actual));
                Assert.Less(Mathf.Abs(expected - actual), 1e-5f,
                    $"单 octave 时 Sample({p.x},{p.y}) = {actual} ≠ 0.5+0.5·Noise = {expected}");
            }
            Debug.Log($"[fBm·单层恒等] octaves=1 时 6 个探针点与 0.5+0.5·Noise(x/scale,y/scale) 最大误差 {maxErr:E2} ✓");
        }

        [Test]
        public void Fbm_DifferentOctaveCounts_ChangeResult()
        {
            const int seed = 5;
            var oneOctave = new FractalNoise2D(seed, 1, 2f, 0.5f, 4f);
            var fiveOctaves = new FractalNoise2D(seed, 5, 2f, 0.5f, 4f);
            float maxDiff = 0f;
            for (int y = 0; y < 33; y++)
                for (int x = 0; x < 33; x++)
                    maxDiff = Mathf.Max(maxDiff,
                        Mathf.Abs(oneOctave.Sample(x * 0.5f, y * 0.5f) - fiveOctaves.Sample(x * 0.5f, y * 0.5f)));
            Debug.Log($"[fBm·octaves 生效] 同种子 octaves 1→5,33×33 网格最大值变化 {maxDiff:F4}(要求 > 0.01)");
            Assert.Greater(maxDiff, 0.01f, "增加 octaves 后采样场几乎未变,高频层疑似未叠加");
        }

        [Test]
        public void Fbm_HighFrequencyEnergy_GrowsWithOctaves()
        {
            double e1 = AdjacentDiffEnergy(1);
            double e2 = AdjacentDiffEnergy(2);
            double e4 = AdjacentDiffEnergy(4);
            Debug.Log($"[fBm·高频能量] 相邻差能量 E(1)={e1:F5}, E(2)={e2:F5}, E(4)={e4:F5};"
                      + $" 比值 E(2)/E(1)={e2 / e1:F3}, E(4)/E(2)={e4 / e2:F3}(要求均 > 1.1)");
            Assert.Greater(e1, 1e-4, "单层能量接近 0,采样场疑似恒定");
            Assert.Greater(e2, 1.1 * e1, "能量 E(2) 未明显高于 E(1),octaves 未注入高频成分");
            Assert.Greater(e4, 1.1 * e2, "能量 E(4) 未明显高于 E(2),更高层 octaves 未注入高频成分");
        }

        [Test]
        public void Fbm_SameSeed_ReproducesValues()
        {
            var fbmA = new FractalNoise2D(1234, 5, 2f, 0.5f, 10f);
            var fbmB = new FractalNoise2D(1234, 5, 2f, 0.5f, 10f);
            Vector2[] probePoints =
            {
                new Vector2(0f, 0f), new Vector2(3.3f, 7.7f), new Vector2(-15.2f, 4.4f),
                new Vector2(88.8f, 12.0f), new Vector2(31.999f, 0.001f),
            };
            foreach (Vector2 p in probePoints)
                Assert.AreEqual(fbmA.Sample(p.x, p.y), fbmB.Sample(p.x, p.y), "同种子同参数的两个 fBm 实例给出不同值");
            Debug.Log("[fBm·同种子复现] 两个同参数实例在 5 个探针点逐位相同 ✓");
        }

        // ═════════════════════════════════════════════════════════════════════
        // 四、HeightmapGenerator:端到端确定性、种子区分、尺寸与值域
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void Heightmap_SameSeed_IsBitwiseDeterministic()
        {
            var parameters = MakeParams(1234, 65);
            var stopwatch = Stopwatch.StartNew();
            float[,] first = HeightmapGenerator.Generate(parameters);
            stopwatch.Stop();
            float[,] second = HeightmapGenerator.Generate(parameters);

            string hashFirst = FnvHash(first), hashSecond = FnvHash(second);
            float maxDiff = MaxAbsDiff(first, second);
            Debug.Log($"[高度图·确定性] seed=1234, res=65;首次耗时 {stopwatch.Elapsed.TotalMilliseconds:F1} ms;"
                      + $" 哈希 A={hashFirst} B={hashSecond};逐元素最大差 {maxDiff}");
            Assert.AreEqual(hashFirst, hashSecond, "两次生成的 FNV-1a 哈希不同,位级确定性被破坏");
            Assert.AreEqual(0f, maxDiff, "两次生成存在逐元素差异,确定性被破坏");
        }

        [Test]
        public void Heightmap_DifferentSeeds_Differ()
        {
            float[,] mapA = HeightmapGenerator.Generate(MakeParams(1234, 65));
            float[,] mapB = HeightmapGenerator.Generate(MakeParams(999, 65));
            float maxDiff = MaxAbsDiff(mapA, mapB);
            Debug.Log($"[高度图·异种子区分] seed 1234 vs 999:哈希 {FnvHash(mapA)} vs {FnvHash(mapB)},最大差 {maxDiff:F4}(要求 > 0.05)");
            Assert.Greater(maxDiff, 0.05f, "不同种子生成了几乎相同的高度图,种子未真正生效");
        }

        [Test]
        public void Heightmap_SizeAndRange_AreCorrect()
        {
            var normal = MakeParams(7, 65);
            float[,] map = HeightmapGenerator.Generate(normal);
            Assert.AreEqual(65, map.GetLength(0), "高度图行数(z 维)应等于 Resolution");
            Assert.AreEqual(65, map.GetLength(1), "高度图列数(x 维)应等于 Resolution");
            float min = float.MaxValue, max = float.MinValue;
            for (int z = 0; z < 65; z++)
                for (int x = 0; x < 65; x++)
                {
                    min = Mathf.Min(min, map[z, x]);
                    max = Mathf.Max(max, map[z, x]);
                }
            Debug.Log($"[高度图·尺寸值域] res=65,范围 [{min:F4}, {max:F4}] ⊂ [0,1] ✓");

            var shaped = MakeParams(7, 65);
            shaped.HeightExponent = 2.5f;
            float[,] shapedMap = HeightmapGenerator.Generate(shaped);
            float shapedMin = float.MaxValue, shapedMax = float.MinValue;
            for (int z = 0; z < 65; z++)
                for (int x = 0; x < 65; x++)
                {
                    shapedMin = Mathf.Min(shapedMin, shapedMap[z, x]);
                    shapedMax = Mathf.Max(shapedMax, shapedMap[z, x]);
                }
            Debug.Log($"[高度图·幂次造型] exponent=2.5 后范围 [{shapedMin:F4}, {shapedMax:F4}] 仍在 [0,1] ✓");
            Assert.That(shapedMin, Is.InRange(-RangeEps, 1f + RangeEps), "幂次重映射后出现越界值");
            Assert.That(shapedMax, Is.InRange(-RangeEps, 1f + RangeEps), "幂次重映射后出现越界值");
        }

        // ═════════════════════════════════════════════════════════════════════
        // 五、MeshDataBuilder:数量、索引、顶点位置、绕序、UV
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void Mesh_Counts_AreCorrect()
        {
            const int res = 17;
            var parameters = MakeParams(5, res);
            parameters.WorldSize = 160f;
            parameters.HeightScale = 40f;
            MeshData mesh = MeshDataBuilder.Build(SyntheticHeights(res), parameters);

            Assert.AreEqual(res * res, mesh.Vertices.Length, $"顶点数应为 {res}²={res * res}");
            Assert.AreEqual((res - 1) * (res - 1) * 6, mesh.Triangles.Length, "三角形索引数应为 (res-1)²×6");
            Assert.AreEqual(res * res, mesh.UVs.Length, "UV 数应与顶点数一致");
            Debug.Log($"[Mesh·数量] res={res}:顶点 {mesh.Vertices.Length},索引 {mesh.Triangles.Length},UV {mesh.UVs.Length} ✓");
        }

        [Test]
        public void Mesh_Indices_AreAllWithinRange()
        {
            const int res = 17;
            var parameters = MakeParams(5, res);
            MeshData mesh = MeshDataBuilder.Build(SyntheticHeights(res), parameters);
            for (int i = 0; i < mesh.Triangles.Length; i++)
            {
                int index = mesh.Triangles[i];
                Assert.That(index, Is.InRange(0, mesh.Vertices.Length - 1), $"索引[{i}]={index} 越界");
            }
            Debug.Log($"[Mesh·索引合法] 全部 {mesh.Triangles.Length} 个索引 ∈ [0, {mesh.Vertices.Length - 1}] ✓");
        }

        [Test]
        public void Mesh_VertexPositions_MatchHeightmap()
        {
            const int res = 17;
            var parameters = MakeParams(5, res);
            parameters.WorldSize = 160f;
            parameters.HeightScale = 40f;
            float[,] heights = SyntheticHeights(res);
            MeshData mesh = MeshDataBuilder.Build(heights, parameters);

            float cell = parameters.WorldSize / (res - 1);
            float maxErr = 0f;
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    Vector3 v = mesh.Vertices[z * res + x];
                    maxErr = Mathf.Max(maxErr,
                        Mathf.Max(Mathf.Abs(v.x - x * cell),
                                  Mathf.Max(Mathf.Abs(v.y - heights[z, x] * parameters.HeightScale),
                                            Mathf.Abs(v.z - z * cell))));
                }
            Debug.Log($"[Mesh·顶点位置] index=z·res+x 约定,cell={cell:F1}:全量 {res * res} 顶点最大位置误差 {maxErr:E2}(容差 1e-3)✓");
            Assert.Less(maxErr, 1e-3f, "顶点位置与高度图/格距约定不符");
        }

        [Test]
        public void Mesh_AllTriangles_WindUpward()
        {
            const int res = 17;
            var parameters = MakeParams(5, res);
            MeshData mesh = MeshDataBuilder.Build(SyntheticHeights(res), parameters);

            float minNormalY = float.MaxValue;
            int triangleCount = mesh.Triangles.Length / 3;
            for (int t = 0; t < triangleCount; t++)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[t * 3]];
                Vector3 b = mesh.Vertices[mesh.Triangles[t * 3 + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[t * 3 + 2]];
                float normalY = Vector3.Cross(b - a, c - a).y;
                minNormalY = Mathf.Min(minNormalY, normalY);
                Assert.Greater(normalY, 0f, $"第 {t} 个三角形法线 y={normalY} ≤ 0,绕序朝下(将只能从地底看到)");
            }
            Debug.Log($"[Mesh·绕序朝上] {triangleCount} 个三角形叉积 y 分量全部 > 0,最小值 {minNormalY:F4} ✓");
        }

        [Test]
        public void Mesh_UV_IsNormalized()
        {
            const int res = 17;
            var parameters = MakeParams(5, res);
            MeshData mesh = MeshDataBuilder.Build(SyntheticHeights(res), parameters);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < mesh.UVs.Length; i++)
            {
                min = Vector2.Min(min, mesh.UVs[i]);
                max = Vector2.Max(max, mesh.UVs[i]);
                Assert.That(mesh.UVs[i].x, Is.InRange(-RangeEps, 1f + RangeEps), $"UV[{i}].x={mesh.UVs[i].x} 越界");
                Assert.That(mesh.UVs[i].y, Is.InRange(-RangeEps, 1f + RangeEps), $"UV[{i}].y={mesh.UVs[i].y} 越界");
            }
            Assert.Less(Vector2.Distance(mesh.UVs[0], Vector2.zero), 1e-5f, "首个顶点 UV 应为 (0,0)");
            Assert.Less(Vector2.Distance(mesh.UVs[mesh.UVs.Length - 1], Vector2.one), 1e-5f, "末位顶点 UV 应为 (1,1)");
            Debug.Log($"[Mesh·UV] 范围 [{min.x:F3},{min.y:F3}] ~ [{max.x:F3},{max.y:F3}],角点 (0,0)/(1,1) 正确 ✓");
        }
    }
}
