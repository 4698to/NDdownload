# NDDownload

天晴盒子（NDToolsBox）安装器 / 下载客户端源码。

用于下载并安装 NDToolsBox 插件与工具资源，提供公网与内网两个渠道入口。

## 环境

- Visual Studio 2019+（建议 2022）
- .NET Framework 4.7.2
- MSBuild（随 Visual Studio 安装）

## 打开工程

打开 `NDDownload\NDDownload.sln`。

还原 NuGet 包后编译。解决方案包含：

| 项目 | 说明 |
|------|------|
| `NDDownload.Core` | 安装器共享库 |
| `NDDownload` | 公网版入口（`NDDownload.exe`，腾讯云服务器） |
| `NDDownloadIn` | 内网版入口（`NDDownloadIn.exe`，公司内网服务器） |
| `SettingsWPF` | 设置相关 WPF 工程 |

## 打包发布

在仓库根目录执行：

```powershell
.\build-channels.ps1 -Configuration Release
```

或双击 `build.bat`。

产物输出到 `dist\NDToolsBox\`，部署时将整个目录复制到：

```
C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\
```

## 相关

- 3ds Max 插件客户端：`NDToolsBox-3dsMax`
- 安装/下载入口：https://sundaybox.cc/ndtooldata/

## 开源组织

本项目由以下组织开源：

- 网龙网络公司
- 福建天晴数码有限公司

## 开源协议

本项目采用 [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) 开源协议。

你可以自由使用、修改和分发本软件，包括用于商业用途，但须遵守该协议的要求，例如保留版权与许可声明，并在修改时注明变更。软件按「原样」提供，不附带任何明示或暗示的担保。
