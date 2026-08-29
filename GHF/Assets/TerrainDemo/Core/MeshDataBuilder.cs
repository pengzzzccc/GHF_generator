// ─────────────────────────────────────────────────────────────────────────────
// 文件:Core/MeshDataBuilder.cs
// 模块:程序化地形生成 · 核心(纯数据,仅用 Unity 数学类型)
// 状态:骨架文件,实现留空待补(测试文件定义了本类的行为契约)
// ─────────────────────────────────────────────────────────────────────────────

using System;

using UnityEngine;

namespace TerrainDemo.Core
{
    /// <summary>
    /// 网格几何数据的纯数据容器:顶点、三角形索引、UV。与 UnityEngine.Mesh 解耦,
    /// 便于脱离引擎构建与测试,TerrainGenerator 拿到它之后再灌入真正的 Mesh 资源。
    /// </summary>
    public struct MeshData
    {
        /// <summary>顶点数组,长度 = Resolution²;下标 index = z·Resolution + x。</summary>
        public Vector3[] Vertices;

        /// <summary>三角形索引数组,长度 = (Resolution−1)²·6;每三个一组。</summary>
        public int[] Triangles;

        /// <summary>UV 数组,与顶点一一对应,取值 [0,1]。</summary>
        public Vector2[] UVs;
    }

    /// <summary>
    /// 把高度图转成网格几何数据的纯函数。契约见 Build 注释与文件尾说明。
    /// </summary>
    public static class MeshDataBuilder
    {
        /// <summary>
        /// 构建网格数据。契约:顶点按 index = z·Resolution + x 排布,位置为
        /// (x·cell, heights[z,x]·HeightScale, z·cell);UV = (x/(R−1), z/(R−1));
        /// 每个格子两个三角形,绕序满足从上方看叉积 y 分量为正(法线朝上);
        /// 所有索引都在顶点范围内。
        /// </summary>
        public static MeshData Build(float[,] heights, TerrainParams parameters)
        {
            // TODO(待实现):先分配三组数组,再按行主序填顶点与 UV,最后逐格生成两个三角形。
            throw new NotImplementedException();
        }
    }
}

// ── 实现原理与思路(自然段) ──────────────────────────────────────────────────
//
// 目的。高度图是"数据",能被渲染的网格是"几何",本类就是两者之间唯一的桥。把它
// 从 TerrainGenerator 里拆出来,是因为学习目标之一是熟悉 Unity Mesh API,而 Mesh API
// 真正关心的只有三件事:顶点在哪、三角形怎么连、UV 怎么铺。把这三样装进纯数据
// 结构,网格构建就能像噪声一样被单元测试逐项校验,装配 Mesh 的那薄薄一层则留在
// 运行时模块里,职责各归其位。
//
// 实现原理。规则网格的三角化是固定套路:每个格点生成一个顶点,位置由平面坐标和
// "高度 × HeightScale"决定;每个由四个相邻顶点围成的格子拆成两个三角形,共
// (R−1)²·6 个索引。三角形在 Unity 里按顺时针为正面,顶点朝上的绕序要保证叉积的
// y 分量为正,写错的话地形会变成只从地底可见的"背面"。UV 取格点下标的归一化值,
// 与顶点一一对应,为将来贴材质、做高度着色留好坐标。整个构建没有任何随机成分,
// 确定性完全继承自高度图本身。
//
// 思路。实现时建议先把顶点与 UV 两个循环写完并自检(顶点数、角点坐标),再单独
// 写三角化循环,后者的两个三角形共享对角线,绕序一旦写反,测试的"法线朝上"断言
// 会精确指出是哪一半错。避免每格重复计算共享顶点,顶点循环与三角形循环分开写,
// 也是为将来分辨率提升时的性能留余地。这一步纯搬数据、没有技巧,风险全部集中在
// 绕序与索引偏移上,恰好是测试覆盖最严的两处。
// ─────────────────────────────────────────────────────────────────────────────
