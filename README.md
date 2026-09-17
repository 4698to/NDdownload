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

### 安装器自更新（服务器）

发布新版安装器时：

1. 提高源码中的 `ResourcesUrl.version` / `buildtime`（必须与将上传的 zip 内编译版本一致）
2. 执行 `build-channels.ps1`：会从 `ResourcesUrl.version` 自动生成 `dist\NDToolsBox\NDDownload_version.txt`（并复制到 `dist\NDDownload_version.txt`）
3. 将 `dist\NDToolsBox\` **目录内的文件**打成 `NDToolsBox.zip`（推荐 zip 根目录直接是 exe/dll，不要再包一层多余文件夹）并上传；同时上传同版本的 `NDDownload_version.txt`

若服务器上的 `NDDownload_version.txt` 比本地新，但 zip 里仍是旧版 exe，更新后会反复提示——务必保证二者版本一致。

启动 `NDDownload.exe` / `NDDownloadIn.exe` 且联网成功后，会比对服务器版本；若更高则提示下载 zip，退出后解压覆盖安装目录并重启。

**注意：** 不要把 `NDToolsBox.zip` 写入 `InstallBox_version_full.json` 工具清单。该包仅用于安装器自更新；若进入安装/卸载列表，卸载时可能删掉安装器自身。客户端也会硬性过滤该项。

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
