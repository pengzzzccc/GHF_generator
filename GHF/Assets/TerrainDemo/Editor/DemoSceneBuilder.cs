// ─────────────────────────────────────────────────────────────────────────────
// 文件:Editor/DemoSceneBuilder.cs
// 模块:程序化地形生成 · Unity 编辑器
// 状态:骨架文件,实现留空待补
// ─────────────────────────────────────────────────────────────────────────────

using System;

using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Editor
{
    /// <summary>
    /// 一键搭建演示场景的编辑器工具,避免手工创建与摆放物体。
    /// </summary>
    public static class DemoSceneBuilder
    {
        /// <summary>菜单入口:Terrain Demo → Setup Scene,可重复执行(先清理同名旧物体再重建)。</summary>
        [MenuItem("Terrain Demo/Setup Scene")]
        public static void SetupScene()
        {
            // TODO(待实现):
            // 1) 清理旧物体(按名字查找 "TerrainDemo/*" 前缀);
            // 2) 创建地形根物体 TerrainRoot:MeshFilter + MeshRenderer(默认 URP Lit 材质)
            //    + MeshCollider + TerrainGenerator;
            // 3) 找到/创建主相机并挂 FlyCamera,摆到能俯瞰 200 米地形的初始位姿;
            // 4) 创建 Canvas 挂 TerrainDemoUI(引用 TerrainRoot 上的生成器),再加一盏平行光;
            // 5) Undo.RegisterCreatedObjectUndo + MarkSceneDirty,并选中 TerrainRoot。
            throw new NotImplementedException();
        }
    }
}

// ── 实现原理与思路(自然段) ──────────────────────────────────────────────────
//
// 目的。演示场景由五类物体组成:地形根、相机、UI 画布、灯光和它们之间的引用关系。
// 手工搭建既繁琐又容易漏引用,一键脚本把工程"打开即可跑"变成现实,也为反复重建
// (调预设、换参数后重建)提供了可重复的操作入口。
//
// 实现原理。MenuItem 把静态方法注册成编辑器菜单项,点击后在新场景或当前场景里
// 程序化创建物体:地形根挂 MeshFilter/MeshRenderer/MeshCollider 与 TerrainGenerator,
// 材质用内建管线兼容的默认 URP Lit(灰模即可,评估的是结构与细节,不需要贴图);
// 相机挂 FlyCamera 并给出合适的初始位姿(例如从 (150, 120, -150) 看向原点);Canvas
// 挂 TerrainDemoUI 并把生成器引用接好。两个编辑器礼仪不可省:创建的物体注册
// Undo,场景标记 dirty,这样一键搭建本身可以被撤销、被保存。
//
// 思路。脚本设计为可重复执行——先按命名约定清理上一轮生成的物体再重建,避免多次
// 点击后场景里堆满重复物体;所有物体统一加 "TerrainDemo/" 前缀方便识别与清理。
// 搭建完成后可顺手调用一次 generator 的 Awake 依赖检查,若引用缺失在编辑期就报
// 明确的错误,而不是留到运行时才发现面板失效。
// ─────────────────────────────────────────────────────────────────────────────
