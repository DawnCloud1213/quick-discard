# Quick Discard

SPT 4.1.x / EFT build 40743 inventory drag-to-discard plugin.

## 当前行为

- 局内打开背包时，在右下角显示 `DROP` 丢弃区。
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
| `UI.Width` | `168` | 丢弃区宽度 |
| `UI.Height` | `88` | 丢弃区高度 |
| `UI.Margin` | `24` | 距背包右下角的边距 |

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
