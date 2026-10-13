# 程序体检 - 2026-10-13

范围：发布流程、配置与进度写入、词库解析、音频映射、现有核心/WPF 回归和实际单文件启动。环境：Windows x64，.NET SDK 10.0.401，运行时 10.0.12。

## 结果

- 核心检查 94 项通过；包含 --ui-qa 的完整运行共 99 项通过，无失败。覆盖 Unicode casefold、词库格式与重复键、进度隔离、原子写入失败、复习队列回退、自动播放取消、配置损坏保护、窗口焦点和静音 WAV 解码结束。
- 发布保护检查 5 项通过：保留用户文件和配置、排除 PDB/XML、失败构建保留旧版、多文件产物拒绝、EXE 占用拒绝。
- 正式 EXE 独立复制到含空格目录并改名，从另一工作目录启动：主窗口正常，生成实际 EXE 名称对应的配置，工作目录保持为空。
- 隔离运行时检查确认缺少 .NET 时输出原生安装说明；图形错误弹窗未目视验证。
- NuGet 已知漏洞检查（含传递依赖）未报告易受攻击的包。
- 正式 EXE：1,330,363 字节，约 1.27 MiB；词库、音频、userdata 和用户设置均为外部用户数据。

## 检查结论与限制

设置文件基于实际 EXE 名称，进度基于词库路径独立保存；JSON 写入使用临时文件、刷盘和原子替换，写入失败保留原内容。此次统一发布未改变词库格式、音频映射或复习逻辑，已执行检查未发现回归。

自动 WAV 播放检查采用静音，不代替真实发音试听。首次沙箱运行曾产生 NuGet 漏洞源无法连接的 NU1900 警告；随后本机账户的正式发布和独立漏洞检查成功，未将离线扫描当作安全结论。

## 复核命令

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-Publish.ps1
dotnet run --project ReciteWords.Tests -c Release -- --ui-qa
dotnet list ReciteWords/ReciteWords.csproj package --vulnerable --include-transitive
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Publish.ps1
```

发布配置和配置保留规则见 [统一发布流程](Publishing.md)。