// ─────────────────────────────────────────────────────────────────────────────
// 文件:Runtime/FlyCamera.cs
// 模块:程序化地形生成 · Unity 运行时
// 状态:骨架文件,实现留空待补
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace TerrainDemo
{
    /// <summary>
    /// 自由观察相机:WASD 平移、QE 升降,按住鼠标右键拖动旋转视角,用于浏览生成的山体。
    /// </summary>
    public class FlyCamera : MonoBehaviour
    {
        /// <summary>平移速度(米/秒)。</summary>
        [SerializeField] private float moveSpeed = 10f;

        /// <summary>视角旋转灵敏度(度/像素)。</summary>
        [SerializeField] private float lookSpeed = 3f;

        private void Update()
        {
            // TODO(待实现):读取 WASD/QE 得到本机轴向的移动向量,乘 moveSpeed·Time.deltaTime 平移;
            // 按住右键时用 Input.GetAxis("Mouse X"/"Mouse Y") 累加 yaw/pitch 并施加到 transform。
        }
    }
}

// ── 实现原理与思路(自然段) ──────────────────────────────────────────────────
//
// 目的。地形演示的观感评估离不开绕着山走一圈:结构保真度要看山脊线是否连贯,
// 细节保真度要凑近看表面过渡,这些都要求相机能自由飞行。本组件只做这一件事,
// 不与生成管线耦合,任何场景挂上就能用。
//
// 实现原理。移动部分每帧读取键盘输入组装一个本机坐标系下的方向向量(前后是
// transform.forward 的投影、左右是 transform.right、QE 对应世界 Y 轴),乘以速度与
// 帧间隔 deltaTime 后施加平移——乘 deltaTime 保证不同帧率下移动速度一致。旋转部分
// 采用最常见的 yaw/pitch 方案:按住鼠标右键时,把鼠标移动量按灵敏度折算成绕世界
// Y 轴的偏航与绕自身 X 轴的俯仰,俯仰通常夹在 ±89 度以内,避免万向节翻转带来的
// 翻滚感。用 legacy Input(UnityEngine.Input)即可,项目未启用旧的 Input System
// 强制后端,演示不必为此引入额外配置。
//
// 思路。实现顺序建议先平移后旋转,各自独立验证;pitch 用一个私有字段累加而不是
// 读回 transform 欧拉角,可以规避欧拉角解算的抖动。数值手感(moveSpeed/lookSpeed)
// 不必一次调准,等山生成出来后按 200 米边长的场景尺度微调即可。
// ─────────────────────────────────────────────────────────────────────────────
