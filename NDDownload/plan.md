修正 GetBestServer(bool)：Ping 目标应与 GetServerName 所选主机一致
理顺 NDDownloadIn 的引用方式，消除 CS0436
在 packages.config 中补全 Downloader，或迁移到 PackageReference
移除 Ssl3，统一 TLS 1.2+
已清理未使用字段、修正 async、收紧异常处理
已删除 NDDownload2；目录 Imgae→Image；命名 SelectSource / GetInstallItem 等已修正