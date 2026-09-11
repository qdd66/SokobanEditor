# 推箱子编辑器

本项目是一套在游戏运行时使用的推箱子关卡编辑器，其核心特色在于支持在Unity运行时以第一人称视角搭建关卡，可以边玩边修改关卡内容，并可在运行时保存和打开关卡，还可将关卡保存为本地文件。

Unity 推箱子玩法 + 运行时地图编辑器。打开示例关即可游玩；按 **F1** 切到第一人称编辑，在棋盘上摆玩家、墙、地面、箱子和目标点，再按 F1 立刻用新布局继续玩。

- 引擎：**Unity 6000.3.12f1**（Unity 6）
- 渲染：**URP**
- 仓库：https://github.com/qdd66/SokobanEditor
- 视频演示链接：https://my.feishu.cn/wiki/CGFqw4JC5i7tAKkAhGCcUBSAn5b?from=from_copylink

更细的模块边界、配置路径和约定见 [`项目手册.md`](项目手册.md)。

## 环境

1. 安装 [Unity Hub](https://unity.com/download) 与编辑器 **6000.3.12f1**。
2. 克隆本仓库（不要拷贝别人电脑上的 `Library/`）。

```powershell
git clone https://github.com/qdd66/SokobanEditor.git
```

3. 用 Hub 打开工程根目录，等待首次导入结束（`Library` 会在本地重新生成）。

依赖（已写在 `Packages/manifest.json`）：Input System、URP、TextMeshPro、`com.unity.pipeline`、Unity CLI Bridge。Inspector 中文标签使用 **Odin Inspector**（`Assets/Plugins/Sirenix`）。

## 快速开始（推荐）

1. 打开场景 `Assets/Sokoban/Scenes/SokobanSample.unity`。
2. 按 Play。默认是俯视推箱子。

示例关是 7×6 地面砖 + 围墙 + 1 名玩家 + 2 个箱子 + 2 个目标点。

若场景里没有飞行编辑角色或玩法/编辑切换组件，在 Unity 菜单执行 `Tools/推箱子/接入地图编辑器`。预制体或示例关丢失时：先 `Tools/推箱子/生成示例场景与预制体`，再 `Tools/推箱子/替换关卡素材`，最后再跑一次接入菜单。

## 操作

### 玩法

| 按键 | 作用 |
|------|------|
| WASD | 点按移动一格 |
| Z | 撤回 |
| R | 重置为当前已提交的开局 |
| 鼠标滚轮 | 拉近 / 拉远俯视相机（暂停或编辑时无效） |
| F1 | 进入编辑 |
| Esc | 暂停：继续、保存/读取、设置 |

规则：墙挡住人和箱子；一次只能推一箱；目标点与地面不挡格。所有目标点上都有箱子即过关。地面砖只负责外观，不算障碍。

### 编辑（F1）

第一人称飞行，准心放置。背包里是关卡棋子：玩家、墙 1/2/3、地面、箱子、目标点。

| 按键 | 作用 |
|------|------|
| WASD | 水平飞 |
| Space / Ctrl | 升降 |
| 鼠标 | 转向 |
| F | 选定 |
| B | 背包 |
| V | 高度 |
| X | 连续摆放 |
| Y | 网格（按住 Ctrl 不切换，避免和重做抢键） |
| C | 贴合 |
| CapsLock / Q / E | 旋转 |
| Shift+Q / E | 缩放 |
| F1 | 回到玩法（按当前棋盘重编规则） |
| Ctrl+S / Ctrl+O | 保存 / 打开 `.tabsmap` |
| Ctrl+Z / Ctrl+Y | 撤回 / 重做 |

编辑时箱子可以叠在目标点上；地面是单独一层，可与墙/箱/人同格。玩家最多一个。没有玩家或没有目标点时不能回玩法；箱子少于目标点仍可玩，但会提示。

快捷键和相机灵敏度可在 Esc → 设置 → **推箱子** 页修改，配置资产是 `Assets/Sokoban/Data/SokobanConfig.asset`。

## 工程结构

| 路径 | 职责 |
|------|------|
| `Assets/Sokoban/Scripts/` | 推箱子规则、输入、会话、俯视相机、HUD |
| `Assets/Sokoban/PlayEdit/` | F1 玩法 / 编辑切换 |
| `Assets/Sokoban/Data/` | 玩法配置、关卡棋子 Catalog |
| `Assets/Sokoban/Scenes/SokobanSample.unity` | 推荐入口 |
| `Assets/DZDRuntimeMapEditor/` | 运行时地图编辑器（飞行、放置、存档、暂停菜单） |
| `Assets/HaniJahanDesign/FreePack/` | 第三方美术（箱子、地块等） |

玩法程序集 `Sokoban.Runtime` 只引用编辑器程序集，以便设置项出现在 Esc 面板。编排在 `SokobanPlayEditController`，不要让 `SokobanSession` 直接去调飞行或放置类型。

关卡编辑用 `Assets/Sokoban/Data/Placement/SokobanPlaceableCatalog.asset`。地图编辑器背包里那个「推箱子」分类是装饰物，不是玩法棋子。

数值、快捷键、物品列表改 ScriptableObject / 预制体 / 场景引用，不要改 C#。

## 存档

- 扩展名 `.tabsmap`，格式 version 1。
- 编辑器下默认写到工程根目录 `Maps/`（该目录不进 Git）。
- 只存相机位姿和已放置物的编号、位置、旋转、缩放。

## 纯地图编辑器（可选）

打开 `Assets/DZDRuntimeMapEditor/Scenes/StartScene.unity` 可以走「新建地图 / 打开存档」流程。完整编辑场景是 `Assets/DZDRuntimeMapEditor/Scenes/DefaultScene.unity`。当前 Build Settings 里仍是 URP 空模板 `SampleScene`，要从开始界面进编辑，需要把带 `MapSceneAnchor` 的场景加入 Build Settings，并把 `StartMenuConfig` 的编辑场景名改成对应场景名。日常改关卡请用上面的 `SokobanSample`。

## 第三方与授权

本仓库为学习/备份用途，其中包含商业或第三方资源，**请勿默认可以再分发**：

- [Odin Inspector](https://odininspector.com/)（`Assets/Plugins/Sirenix`）
- VInspector / VFolders / VTabs / VHierarchy（`Assets/Packages/VEditor`）
- [HaniJahanDesign FreePack](Assets/Packages/HaniJahanDesign/FreePack/README.md) 美术包

克隆后请自行确认你拥有对应授权。没有 Odin 时，工程 Editor 侧有桩代码，但 Inspector 体验会降级。
