// ─────────────────────────────────────────────────────────────────────────────
// 文件:Core/TerrainParams.cs
// 模块:程序化地形生成 · 核心(纯数据)
// 状态:骨架文件,预设工厂留空待补(字段已声明,供管线与测试使用)
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace TerrainDemo.Core
{
    /// <summary>
    /// 地形生成所需的全部参数。可序列化,便于在 Inspector 中调整与保存预设。
    /// 除 Seed 外的每个参数都只影响"长相",不影响确定性——同 Seed 不同参数是不同地形,
    /// 同参数不同 Seed 也是不同地形。
    /// </summary>
    [Serializable]
    public class TerrainParams
    {
        /// <summary>随机种子,全系统唯一随机源(非零亦可,零合法)。</summary>
        public int Seed = 1;

        /// <summary>高度图边长(格点数,不是格子数),例如 257 表示 256×256 格。</summary>
        public int Resolution = 257;

        /// <summary>地形边长(米),真实世界尺寸。</summary>
        public float WorldSize = 200f;

        /// <summary>最大高度(米),高度图 [0,1] 乘以它得到世界高度。</summary>
        public float HeightScale = 30f;

        /// <summary>fBm 基准波长(米):第 0 个 octave 的特征尺寸,越大山体越"缓"。</summary>
        public float Scale = 120f;

        /// <summary>octave 层数:低频到高频叠多少层。</summary>
        public int Octaves = 5;

        /// <summary>频率倍率:每层的采样频率乘子,经典值 2。</summary>
        public float Lacunarity = 2f;

        /// <summary>持续度:每层的振幅乘子,经典值 0.5,越大细节越"吵"。</summary>
        public float Persistence = 0.5f;

        /// <summary>高度幂次:对归一化高度做 h^e 重映射,e&gt;1 压低谷底、凸显峰顶。</summary>
        public float HeightExponent = 1f;

        /// <summary>预设一:平缓丘陵——层数少、幅度温和,展示低频结构本身。</summary>
        public static TerrainParams RollingHills()
        {
            // TODO(待补):仅数据不同——调 Scale/Octaves/HeightScale/HeightExponent 的组合。
            throw new NotImplementedException();
        }

        /// <summary>预设二:嶙峋高峰——层数多、幂次高,展示高频细节与峰谷对比。</summary>
        public static TerrainParams RuggedPeaks()
        {
            // TODO(待补):仅数据不同。
            throw new NotImplementedException();
        }

        /// <summary>预设三:台地方山——幂次或量化重映射造成平顶台地,展示造型手段。</summary>
        public static TerrainParams TerracedMesas()
        {
            // TODO(待补):仅数据不同。
            throw new NotImplementedException();
        }
    }
}

// ── 实现原理与思路(自然段) ──────────────────────────────────────────────────
//
// 目的。把"生成一座山"所需的一切决策集中到一个可序列化的对象里。演讲稿把可控性
// 定义为"通过种子精确复现",而实际可控制的远不止种子:波长、层数、持续度、幂次
// 共同决定了地形的性格。参数集中后,"同种子 + 同参数 ⇒ 同地形"这一确定性命题才有
// 明确的输入定义;三个预设则对应交付物里"三个展示不同结构特征的地形"。
//
// 实现原理。类本身只是 [Serializable] 的纯数据容器,没有任何行为逻辑,因此可以安全
// 地序列化、在 Inspector 里实时调整、被测试用对象初始化器构造。字段默认值取经典
// 组合(分辨率 257、边长 200 米、波长 120 米、五层、2/0.5),这套值在 200 米见方的
// 场地里大致能给出"一座山"的观感,也是性能预估(六万余顶点)的基准。
//
// 思路。三个预设工厂应当返回仅数据不同的新实例——它们是"参数空间采样"的三个代表
// 点:一个示低频结构(丘陵),一个示高频细节与对比(高峰),一个示造型手段(台地)。
// 设计预设时建议保持 Seed 固定,这样三者对比时地形差异全部归因于参数,演示时更有
// 说服力;具体数值不必一次到位,等管线跑通后对着画面微调即可。
// ─────────────────────────────────────────────────────────────────────────────
