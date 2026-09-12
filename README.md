# Quick Discard

SPT 4.1.x / EFT build 40743 inventory drag-to-discard plugin.

## 当前行为

- 局内打开背包时，在左半边人物区域下方显示 `DROP` 丢弃区（默认铺满左下矩形，不留空白）。
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
| `General.SkipConfirmation` | `false` | 是否跳过销毁确认框 |
| `Hotkey.Discard` | 空 | 可选丢弃热键，例如 `F8` |
| `UI.AnchorMinX` | `0.006` | 丢弃区左边界，按界面宽度比例 |
| `UI.AnchorMinY` | `0.02` | 丢弃区下边界，按界面高度比例 |
| `UI.AnchorMaxX` | `0.271` | 丢弃区右边界，按界面宽度比例 |
| `UI.AnchorMaxY` | `0.13` | 丢弃区上边界，按界面高度比例 |
| `UI.Margin` | `0` | 锚定矩形内缩的像素，`0` 表示完全铺满 |
| `UI.Label` | `DROP` | 丢弃区中央文字 |
| `Debug.DumpInventoryLayout` | `false` | 打开后把背包界面各面板的归一化坐标写进 BepInEx 日志，用于校准丢弃区位置 |

界面比例以整个背包界面为基准：`(0,0)` 是左下角，`(1,1)` 是右上角。想改位置的直接改这四项即可，配置改动在游戏内即时生效，不需要重启。

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
