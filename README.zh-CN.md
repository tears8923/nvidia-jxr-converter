# NVIDIA HDR 截图转格式工具

[English](README.md) | [简体中文](README.zh-CN.md)

把英伟达（NVIDIA GeForce Experience / NVIDIA App）在开启 HDR 时截出的 `.jxr`（JPEG XR）截图，转成手机、微信、QQ 等任何设备都能直接打开的 **JPG** 或 **PNG**，并且**颜色正确**。

开启 HDR 时，NVIDIA 会把截图保存成 `.jxr`（JPEG XR），这是微软的 HDR 图像格式，它有两个问题：

- 大多数手机和聊天软件（微信、WhatsApp 等）**打不开**；
- 里面存的是**线性光数据**，按普通 sRGB 图片直接查看会**偏暗、发灰、偏紫**。

本工具同时解决这两个问题：正确解码 HDR 数据，做一次标准的 **HDR → SDR 色调映射**，再导出标准 sRGB 图片。

---

## 目录

- [特性](#特性)
- [运行环境](#运行环境)
- [安装](#安装)
- [使用方法](#使用方法)
  - [双击运行（新手）](#双击运行新手)
  - [命令行](#命令行)
- [参数说明](#参数说明)
- [工作原理](#工作原理)
- [常见问题](#常见问题)
- [贡献](#贡献)
- [许可证](#许可证)

---

## 特性

- ✅ 把 `.jxr`（JPEG XR）HDR 截图转成 **JPG** 和/或 **PNG**
- ✅ 正确的 **HDR → SDR 色调映射**（Reinhard 曲线）——高光不过曝、暗部不死黑
- ✅ 快速——像素处理用编译后的 C# 执行（一张 2560×1600 图约 1 秒转换完成）
- ✅ 批量处理——递归扫描整个文件夹
- ✅ 零依赖——只使用 Windows 自带组件
- ✅ 不联网、不收集数据、无需安装
- ✅ 交互式菜单 + 完整命令行接口
- ✅ 永不修改原始文件

---

## 运行环境

- **Windows 10 / 11**（任意版本）
- 其他什么都不需要。工具使用 Windows 自带的 **Windows Imaging Component (WIC)** 编解码器。

> 不需要 Python、不需要 .NET SDK、不需要任何第三方软件。

---

## 安装

1. 下载或克隆本仓库：

   ```bash
   git clone https://github.com/tears8923/nvidia-jxr-converter.git
   ```

2. 完成。没有安装程序。

三个文件必须放在**同一个文件夹**里：

- `convert-jxr.bat` — 双击启动器，带格式选择菜单
- `jxr2sdr.ps1` — 转换引擎（PowerShell）
- `JxrConverter.cs` — C# 像素处理引擎（加速用，必须保留）

---

## 使用方法

### 双击运行（新手）

1. 打开存放这三个文件的文件夹。
2. **双击 `convert-jxr.bat`**。
3. 出现菜单：

   ```
   [1] JPG  - 体积小，适合发微信 / 手机
   [2] PNG  - 无损，画质最好
   [3] BOTH - 同时导出 JPG 和 PNG

   请选择 1、2 或 3：
   ```

4. 按 `1`、`2` 或 `3`，然后回车。
5. 等出现 `done: X converted`。
6. 按任意键关闭窗口。
7. 在 `done` 文件夹里找到转好的图片。

默认情况下，工具会递归扫描 `Videos\NVIDIA` 文件夹，并转换找到的**每一个** `.jxr` 文件。

### 命令行

在项目文件夹里打开 PowerShell，直接运行 `jxr2sdr.ps1`。

**把 `Videos\NVIDIA` 下所有 `.jxr` 转成 JPG：**

```powershell
.\jxr2sdr.ps1
```

**转成 PNG：**

```powershell
.\jxr2sdr.ps1 -Fmt png
```

**转换单个文件：**

```powershell
.\jxr2sdr.ps1 -InPath "C:\路径\到\截图.jxr"
```

**转换整个文件夹：**

```powershell
.\jxr2sdr.ps1 -InPath "C:\路径\到\文件夹"
```

**自定义输出位置：**

```powershell
.\jxr2sdr.ps1 -Fmt jpg -OutDir "D:\转换结果"
```

---

## 参数说明

| 参数       | 可选值              | 默认值                 | 说明                       |
|------------|---------------------|------------------------|----------------------------|
| `-Fmt`     | `jpg`、`png`        | `jpg`                  | 输出格式                   |
| `-InPath`  | 文件或文件夹        | *（扫描 Videos\NVIDIA）* | 要转换的文件或文件夹       |
| `-OutDir`  | 文件夹路径          | 源文件所在目录          | 输出目录                   |
| `-Mode`    | `tonemap`、`clip`   | `tonemap`              | HDR → SDR 转换方式         |

### `-Mode` 详解

| 模式       | 行为                                                        | 适用场景                         |
|------------|-------------------------------------------------------------|----------------------------------|
| `tonemap`  | Reinhard 曲线平滑压缩高光，保留细节                         | **默认**，效果最自然             |
| `clip`     | 超过 1.0 的亮度直接截断为纯白                              | 更简单，但可能丢失高光细节       |

---

## 工作原理

转换分四步：

1. **解码**——用 Windows 自带的 WIC（`BitmapDecoder`）把 `.jxr` 解码成**32 位浮点 RGB**，完整保留 HDR 动态范围。
2. **色调映射**——用 Reinhard 曲线（`v / (1 + 0.35·v)`）把 HDR 亮度平滑压缩到 0–1 区间。这一步在**编译后的 C#**（`JxrConverter.cs`）中执行，速度飞快。
3. **sRGB 编码**——套用标准 sRGB 传递函数（`1.055·v^(1/2.4) - 0.055`）。
4. **编码输出**——用 .NET 的 `JpegBitmapEncoder` / `PngBitmapEncoder` 写出标准 8 位图片。

和普通看图软件（直接把浮点数据截断，导致高光死白、暗部发灰）不同，本工具做的是真正的色调映射，结果和你屏幕上看到的一致。

### 为什么不直接把 `.jxr` 改名成 `.jpg`？

因为 `.jxr` 是 **JPEG XR**，不是普通 JPEG——它们是完全不同的编解码器。改名不会转换任何东西。本工具是真正地解码并重新编码。

---

## 常见问题

### 窗口一闪就没了

脚本很可能在你能看到错误前就失败了。改用 PowerShell 运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\jxr2sdr.ps1
```

这样窗口会保持打开，并显示错误信息。

### 提示「禁止运行脚本」

PowerShell 默认执行策略会拦截脚本。先放行一次：

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

出现提示时输入 `Y` 回车。只需设置一次。

### 提示「no .jxr files found」

检查 NVIDIA 截图是否真的存成了 `.jxr`。开启 HDR 时，NVIDIA 保存到：

```
C:\Users\<你的用户名>\Videos\NVIDIA\<游戏名>\
```

### 转出来还是暗的

确认用的是 `-Mode tonemap`（默认值）。如果某张图仍然偏暗，那很可能是截图本身曝光不足，不是转换问题。

### 出现重复的 `_1`、`_2` 文件

为避免覆盖已有文件，检测到重名时新文件会自动加数字后缀。

---

## 根本原因——以及永久解决办法

如果你不想每次截图都转格式，最简单的是截图前关闭 HDR：

- 按 **`Win + Alt + B`** 一键开关 HDR。

关闭 HDR 后，NVIDIA 会直接截出普通 8 位 PNG，手机直接能看——完全不用转换。

---

## 贡献

欢迎提 issue、提需求、贡献代码。

1. Fork 本仓库
2. 创建功能分支
3. 提交改动
4. 推送并发起 pull request

请保持改动精简，并在 README 里记录任何新增参数。

---

## 许可证

本项目采用 **MIT 许可证**。详见 [LICENSE](LICENSE)。

```
MIT License

Copyright (c) 2026 tears8923
```
