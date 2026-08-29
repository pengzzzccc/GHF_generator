# Week 4 演讲稿：基于多层二维噪声的高保真程序化山体生成

**演示者：** [姓名 / 学号]  
**目标时长：** 约 5 分钟

> 本项目只实现一个固定范围的程序化山体生成器，不包含体素、洞穴、侵蚀模拟、无限世界或完整游戏玩法。

[0:00–0:55] I. Background and Game Cases

Hello everyone, my topic today is procedural terrain generation.

Procedural terrain generation has been applied to the environment creation of many games. For example, the development team of *Far Cry 5* used procedural tools to generate terrain textures, water features, and cliffs (Carrier, 2018). Furthermore, procedural terrain can also serve as the stage for gameplay itself; for example, *Minecraft* uses pseudo-random numbers and multi-layered noise to generate explorable worlds (Microsoft Learn, 2025).

[0:55–2:25] II. Literature Review and Evaluation Criteria

Two studies in this field have directly influenced my project direction.

In 1989, Musgrave et al. proposed a noise synthesis method based on fractal terrain, allowing the composition of each frequency in the terrain height field to be locally and independently controlled. They also introduced physical erosion simulation to enhance the realism of the terrain. This work laid the methodological foundation for subsequent procedural terrain generation.

Recent work comes from a 2024 paper by Grenier et al. They found that directly supplementing terrain details with standard noise leads to inconsistencies between texture and landform structure; for example, the direction and distribution of erosion lines do not match the actual slope. Their approach is to adapt the phaser noise to the water flow direction and slope orientation of the terrain, making the generated erosion texture visually consistent with the underlying terrain.

Based on the above literature, I evaluate the project from three dimensions. First is structural fidelity, i.e., whether ridges and valleys are clearly distinguishable. Second is detail fidelity, i.e., whether surface changes at different scales can transition naturally. Third is controllability, i.e., whether the result can be accurately reproduced using a seed.

[2:25–3:35] III. Algorithm Principles and Project Plan

To meet the requirements of structural fidelity and detail fidelity, I will use fBm, i.e., fractal Brownian motion.

Its core principle is to superimpose multiple noise layers of different frequencies, i.e., octaves. Each octave multiplies the sampling coordinates by a factor, so that the same screen position corresponds to a denser grid of points in the noise grid, thus sampling a completely new, higher-frequency noise map. Then, each layer is weighted and summed with decreasing amplitude. The low-frequency layer determines the large-scale outline of the mountain, while the high-frequency layer superimposes surface roughness and gravel texture, ultimately synthesizing a heightmap that combines structure and detail.

Finally, I will implement a fixed-range 2D heightmap pipeline in Unity: first, structure and detail are established through layered noise; then, the reproducibility of the result is controlled through a seed; finally, the heightmap is converted into a browsable 3D mesh.

[3:35–4:25] IV. Learning Objectives

This project has three learning objectives. First, I will be able to implement a deterministic terrain generation pipeline that generates the same terrain with the same seed and parameters. Second, I will be able to evaluate the impact of each additional octave on visual quality and generation time. Third, I will become familiar with the use of the Unity Mesh API.

[4:25–5:00] V. Deliverables Summary

Based on the above plan, this project will deliver a small technical demo built using Unity 6 and the Unity Mesh API. The demo includes a procedurally generated mountain with a fixed range, three preset terrains showcasing different structural features, a UI panel with adjustable seeds, a free camera, and generation times and real-time frame rates for each stage.

---

[0:00–0:55] 一、背景与游戏案例

大家好，我今天演讲的题目是程序化地形生成。

程序化地形生成已经应用在许多游戏的环境制作中。比如《Far Cry 5》的开发团队使用程序化工具生成地形纹理、水系和悬崖（Carrier, 2018）。此外，程序化地形还可以成为玩法本身的舞台，例如《Minecraft》使用伪随机数和多层噪声生成可探索的世界（Microsoft Learn, 2025）。

[0:55–2:25] 二、文献综述与评价依据

在这个领域，有两项研究对我的项目方向有直接影响。

Musgrave 等人在 1989 年在分形地形的基础上提出了一种噪声合成方法，使地形高度场中各频率的组成可以被局部独立控制。他们还引入了物理侵蚀模拟来增强地形的真实感。这项工作为后续的程序化地形生成奠定了方法基础。

 

基于以上文献，我从三个维度评价项目。第一是结构保真度，也就是山脊和山谷是否清晰可辨。第二是细节保真度，也就是不同尺度的表面变化能否自然过渡。第三是可控性，也就是结果能否通过种子精确复现。

[2:25–3:35] 三、算法原理与项目计划

为了满足结构保真度和细节保真度的需求，我将采用 fBm，也就是分形布朗运动。

它的核心原理是叠加多个不同频率的噪声层，也就是 octave。每个 octave 会把采样坐标乘以一个倍率，让同一个屏幕位置对应到噪声网格中更密的格点，从而采出一张全新的、更高频的噪声图。然后每一层按递减的振幅加权求和。低频层决定山体的大尺度轮廓，高频层叠加表面的粗糙度和碎石感，最终合成一张兼具结构和细节的高度图。

最终，我会在 Unity 中实现一条固定范围的二维高度图管线：先通过分层噪声建立结构与细节，再通过种子控制结果的可复现性，最后把高度图转换为可浏览的三维网格。

[3:35–4:25] 四、学习目标

本项目有三个学习目标。第一，我将能够实现一条确定性的地形生成管线，使相同种子和参数生成相同地形。第二，我将能够评价每增加一个 octave 对视觉质量和生成耗时的影响。第三，我将熟悉 Unity Mesh API 的使用。

[4:25–5:00] 五、交付物总结

基于以上计划，本项目将交付一个使用 Unity 6 与 Unity Mesh API 构建的小型技术演示。演示包含一座固定范围的程序化山体、三个展示不同结构特征的预设地形、可调整种子的 UI 面板、自由相机，以及各阶段的生成时间和实时帧率。

---

## 论据与来源对应（不朗读）

| 讲稿中的论据 | 支持来源 |
|---|---|
| 《Minecraft》使用多阶段生成：种子与梯度噪声建立基础地形，后续加入多种噪声参数 | Microsoft Learn (2025), *World Generation Overview* |
| 《Far Cry 5》使用可局部调整的程序化世界制作工具 | Carrier (2018), *Procedural World Generation of Far Cry 5* |
| 《Edge of Eternity》使用高度、坡度与噪声规则控制地形纹理，并提供生物群系绘制工具 | Zeler-Maury (2018), *Semi-Procedural World Generation and Rendering in Edge of Eternity — Part I* |
| 《No Man's Sky》持续、实时地生成可交互的星球地形 | McKendrick (2017), *Continuous World Generation in No Man's Sky* |
| 大尺度结构和局部细节共同影响真实感；地形生成需要用户控制与地貌一致性 | Grenier et al. (2024) |
| 多个加权频段叠加形成复杂噪声；频率缩放形成 octaves；各噪声存在速度与质量取舍 | Lagae et al. (2010) |
| 多频率噪声用于分形地形高度场，并可局部控制频率组成 | Musgrave et al. (1989) |
| 伪随机数生成器由种子或初始状态初始化，再通过确定性递推产生序列 | Matsumoto & Nishimura (1998) |
| 普通 fBm 缺少结构基础；高度、梯度、方向和振幅控制能让细节符合地形 | Grenier et al. (2024) |

## 完整参考资料（不朗读）

### 学术论文

- Musgrave, F. K., Kolb, C. E., & Mace, R. S. (1989). *The Synthesis and Rendering of Eroded Fractal Terrains*. ACM SIGGRAPH Computer Graphics, 23(3), 41–50. https://doi.org/10.1145/74333.74337
- Matsumoto, M., & Nishimura, T. (1998). *Mersenne Twister: A 623-Dimensionally Equidistributed Uniform Pseudo-Random Number Generator*. ACM Transactions on Modeling and Computer Simulation, 8(1), 3–30. https://doi.org/10.1145/272991.272995
- Lagae, A., Lefebvre, S., Cook, R., DeRose, T., Drettakis, G., Ebert, D. S., Lewis, J. P., Perlin, K., & Zwicker, M. (2010). *A Survey of Procedural Noise Functions*. Computer Graphics Forum, 29(8), 2579–2600. https://doi.org/10.1111/j.1467-8659.2010.01827.x
- Grenier, C., Guérin, É., Galin, É., & Sauvage, B. (2024). *Real-time Terrain Enhancement with Controlled Procedural Patterns*. Computer Graphics Forum, 43(1), e14992. https://doi.org/10.1111/cgf.14992

### 游戏与开发资料

- Microsoft Learn. (2025). *World Generation Overview*. https://learn.microsoft.com/en-us/minecraft/creator/documents/world-generation?view=minecraft-bedrock-stable
- Carrier, E. (2018). *Procedural World Generation of Far Cry 5* [GDC conference presentation]. Ubisoft Montreal. https://www.gdcvault.com/play/1025215/Procedural-World-Generation-of-Far
- Zeler-Maury, J. (2018). *Semi-Procedural World Generation and Rendering in Edge of Eternity — Part I*. Game Developer. https://www.gamedeveloper.com/programming/semi-procedural-world-generation-and-rendering-in-edge-of-eternity-part-i-
- McKendrick, I. (2017). *Continuous World Generation in No Man's Sky* [GDC conference presentation]. Hello Games. https://www.gdcvault.com/play/1024265/Continuous_World_Generation_in__No_Man_s_Sky_
