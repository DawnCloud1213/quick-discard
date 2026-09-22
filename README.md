# Quick Discard

SPT 4.1.x / EFT build 40743 inventory drag-to-discard plugin.

## 当前行为

- 局内打开背包时，在左半边人物区域下方显示 `DROP` 丢弃区（默认铺满左下矩形，不留空白）。
- **丢弃区只在装备页签出现**：背包顶栏切到生命/技能/任务/地图/笔记/成就/声望/总体等其他页签时自动隐藏，切回装备页立即恢复。
- 把物品拖到丢弃区后，调用原版 `ItemUiContext.ThrowItem(Item)`，因此保留 SPT/Fika 的原版事务和同步链路。
- 可选热键：配置 `Hotkey.Discard` 后，鼠标悬停在物品上按热键即可丢弃。
- 默认保留原版“确定要销毁吗”确认框；将 `General.SkipConfirmation` 改为 `true` 可跳过匹配到的销毁确认框。

## 配置

首次启动后配置位于：

`BepInEx\config\com.dawncloud.quickdiscard.cfg`

主要项：

| 配置 | 默认值 | 说明 |
|---|---:|---|
| `General.EnableDropZone` | `true` | 是否创建拖放丢弃区 |
| `General.RaidOnly` | `true` | 是否仅在局内显示和生效 |
| `General.EquipmentTabOnly` | `true` | 是否仅在装备页签显示和生效（其他页签自动隐藏） |
| `General.SkipConfirmation` | `false` | 是否跳过销毁确认框 |
| `Hotkey.Discard` | 空 | 可选丢弃热键，例如 `F8` |
| `UI.AnchorMinX` | `0.006` | 丢弃区左边界，按界面宽度比例 |
| `UI.AnchorMinY` | `0.02` | 丢弃区下边界，按界面高度比例 |
| `UI.AnchorMaxX` | `0.271` | 丢弃区右边界，按界面宽度比例 |
| `UI.AnchorMaxY` | `0.13` | 丢弃区上边界，按界面高度比例 |
| `UI.Margin` | `0` | 锚定矩形内缩的像素，`0` 表示完全铺满 |
| `UI.Label` | `DROP` | 丢弃区中央文字 |
| `Debug.DumpInventoryLayout` | `false` | 打开后把背包界面各面板的归一化坐标写进 BepInEx 日志，用于校准丢弃区位置 |
| `Debug.LogTabGate` | `false` | 打开后每次切换页签写一条 `TAB-GATE tab=… equipmentTab=…` 日志，用于排查 `EquipmentTabOnly` |

界面比例以整个背包界面为基准：`(0,0)` 是左下角，`(1,1)` 是右上角。想改位置的直接改这四项即可，配置改动在游戏内即时生效，不需要重启。

### 🎛️ 局内实时调位置（F12 滑块）

本机已装 **BepInEx Configuration Manager**（`BepInEx\plugins\spt\ConfigurationManager`，默认热键 **F12**）。所以不用退出游戏改配置文件：

1. 局内打开背包（停留装备页签）
2. 按 **F12** → 展开 **Quick Discard** → **UI** 区
3. 拖滑块，丢弃区**实时跟着动**

| 滑块 | 范围 | 说明 |
|---|---|---|
| `AnchorMinX` / `AnchorMinY` | `0` ~ `1` | 丢弃区左下角（界面宽/高比例） |
| `AnchorMaxX` / `AnchorMaxY` | `0` ~ `1` | 丢弃区右上角（界面宽/高比例） |
| `Margin` | `0` ~ `200` | 锚定矩形内缩的 UI 像素 |

之所以能实时生效：`DropZoneController.LateUpdate()` 每帧调用 `ApplyPlacement()`，而 `ApplyPlacement()` 每次都直接读 `ConfigEntry.Value`（无缓存），只在数值变化时才重设 `anchorMin/anchorMax/offset`。改完的值由 BepInEx 自动写回 `com.dawncloud.quickdiscard.cfg`，下次启动依然是这个位置。

> 若 F12 没反应（例如被游戏截图键占用），在 `BepInEx\config\com.bepis.bepinex.configurationmanager.cfg` 里改 `Show config manager` 即可。

## 构建

本机没有 .NET SDK 时，可直接使用 Windows 自带的 .NET Framework 编译器：

```powershell
.\build.ps1
```

如果游戏目录不同：

```powershell
.\build.ps1 -GameDir "D:\path\to\EFT"
```

产物：

```text
dist\BepInEx\plugins\QuickDiscard\QuickDiscard.dll
```

将 `QuickDiscard.dll` 放到游戏目录的：

```text
BepInEx\plugins\QuickDiscard\QuickDiscard.dll
```

可用 PowerShell 7 做一次编译产物和目标方法签名校验：

```powershell
pwsh -NoProfile -File .\tools\ValidateBuild.ps1
```

## 实现边界

拖放区不依赖自定义 AssetBundle，只使用 Unity 原生 uGUI。丢弃动作始终调用 `ItemUiContext.ThrowItem(Item)`，不直接调用 `Remove`、`Discard` 或其他低层库存 API。
