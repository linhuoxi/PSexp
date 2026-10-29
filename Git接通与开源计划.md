# PSexp 接通 Git + GitHub 开源 · 施工计划

日期：2026-10-29。本文件是只读核对后的施工计划，现已部分执行：仓库已建立并发布为公开仓库 <https://github.com/linhuoxi/PSexp>（MIT，默认分支 `main`，98 个文件，构建产物未入库，提交署名为 GitHub 隐私邮箱）；业务 C# 代码未作任何改动。

**尚未执行**：C 组第 1 条之外的 BAT 收编（把 `一键编译发布.bat` 纳入版本库），等待排期。文中"未创建仓库、未联网发布"等表述指核对时点，非当前状态。

## D 组执行结果（2026-10-29 实测）

按用户决定调整为**只去混淆、保留内置压缩**（目的是守住 EXE 体积），本文表格第 2 行的"压缩改 false"**没有执行**，`EnableCompressionInSingleFile` 仍为 `true`。

实际改动：`PSdodo.csproj` 的 `EnableReleaseObfuscation` 默认改 `false`；`一键编译发布.bat` 新增总开关 `ENABLE_OBSFUSCATION=0`，据此跳过混淆工具还原与"混淆后二次自测"，标题与状态行同步。`Obfuscation.targets` 与 `dotnet-tools.json` 保留未删，把开关改回 `1` 即恢复原流程。

验证：发布脚本 `[1/6]`→`[6/6]` 全部通过，57 项自测 PASS、0 FAIL；最终 EXE 实测 **50,312,995 字节（47.98 MB）**，状态行显示"混淆：关闭""内置压缩"。改动前的 EXE 只有 `du` 的四舍五入值 48M，脚本成功替换后会删除回滚副本，因此**体积变化量无法给出实测 Δ**——只能确认新值仍由压缩生成、未因去混淆而膨胀。Photoshop 五种导出模式的实机行为仍需人工验收。

## 一句话结论

git 已经装好、也能连上 GitHub，**现在接不通的唯一原因是：这个文件夹还不是一个 git 仓库**（`源码\.git` 和外层 `.git` 都不存在，所以 GitHub 桌面打开它只会说"不是仓库"）。
要做的只有三组事：① 在 `源码` 建仓库并首次提交 → ② 用 GitHub 桌面发布成公开仓库 + MIT 许可证 → ③ 把发布流程里的混淆和内置压缩关掉。

---

## A. 实测现状（每条都可复查）

| 项 | 实测结果 | 证据 |
| --- | --- | --- |
| git | 2.55.0.windows.5 已安装 | `git --version` |
| 署名 | `user.name=linhuoxi`，邮箱已配 | `git config --global --list` |
| GitHub 桌面版 | 已安装 | `AppData\Local\GitHubDesktop\GitHubDesktop.exe` 存在 |
| 命令行 gh | 已登录，真实账号 **linhuoxi**（id 252442538），令牌含 `repo` 权限 | `gh api user` |
| 仓库重名 | `linhuoxi/PSexp` 未被占用，可安全新建 | `gh repo view` 返回"不存在" |
| 网络 | **不需要代理**：git 直连成功（返回 8 个分支）。早前一次 `Connection was reset` 是瞬时干扰 | `git ls-remote https://github.com/git/git.git` 退出码 0 |
| 代理后备 | 本机 `127.0.0.1:7897` 在监听，走它也是 200，留作偶发不通时的后备 | curl 对比测试 |
| 是否已有历史 | **没有**。外层和 `源码` 都无 `.git` | `find -name .git` 无结果 |
| 体量 | `源码` 共 1.3G，但 `bin/obj` 占 1291M；**排除后仅 1.1M** → 推送极轻 | `du -sh --exclude=bin --exclude=obj` |
| 忽略规则 | 已就位：`**/bin/`、`**/obj/`、`*.pdb` | [源码/.gitignore](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/.gitignore) 第 1–4 行 |
| 隐私扫描 | 源码内**无**个人路径、账号、密钥。唯一命中是 README 里一个 shadcn 预设编号，无害 | [源码/README.md:172](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/README.md#L172) |
| 第三方版权 | `references\photoshop-scripts` 下 9 个 .js 是你自己写的脚本（中文注释、`{导出路径}` 占位符），无 Adobe 版权头 → 可公开 | 逐文件取样 |

---

## B. 仓库建在 `源码` 而不是外层（关键决定）

**依据（硬性冲突）**：`一键编译发布.bat` 的根目录自检只允许三项存在——EXE、BAT、`源码`（见 [一键编译发布.bat:195-203](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E4%B8%80%E9%94%AE%E7%BC%96%E8%AF%91%E5%8F%91%E5%B8%83.bat#L195)）。
只要在外层执行 `git init`，`.git` 目录就会让脚本每次跑到第 6 步报 `[根目录异常] .git` 并失败（同文件 [171-173 行](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E4%B8%80%E9%94%AE%E7%BC%96%E8%AF%91%E5%8F%91%E5%B8%83.bat#L171) 是错误出口）。

`源码` 里已经有 `.gitignore` 和 `README.md`，你自己在 [源码/重启评估.md](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/%E9%87%8D%E5%90%AF%E8%AF%84%E4%BC%B0.md) 的"工程基线"里也写了同一条建议（在遵守根目录三项规则的前提下为源码建 Git 历史）。

**唯一取舍**：`一键编译发布.bat` 留在仓库外，公开仓库里别人看不到你的一键发布流程。要把它收进去，得改它的相对路径（第 7–9 行）和根目录白名单，共 4 处——你点头我再单独开一轮，不和这次的改动混在一起。

---

## C. 建库与发布（1–4 我做，5–6 你点两下）

1. **仓库内署名改用 GitHub 隐私邮箱** `252442538+linhuoxi@users.noreply.github.com`，只作用于这个仓库。
   原因：仓库一公开，每次提交的作者邮箱都会被爬虫抓走（垃圾邮件）。这是 GitHub 官方隐私地址，头像和贡献统计照常识别。
2. `git init` + 默认分支 `main` + 首次提交（当前源码全量）。
   可回滚：删掉 `源码\.git` 就回到现在，源码文件一个字节都不动。
3. 新增 `LICENSE`（**MIT**）+ 在 README 顶部加一行许可说明。
   MIT 是"最大限度开放"的标准选择：任何人可商用、修改、闭源再分发，只要求保留你的署名。
   取舍讲明白：MIT 不要求对方也开源；若你要"用了我的代码必须同样开源"那是 GPL，与"最大限度开放"方向相反。真想让别人连署名都不必保留，还有 CC0/Unlicense 一档，但主流做法是 MIT。
4. 提交前把 `源码\Git接通与开源计划.md`（本文件）一起纳入——它是工程记录的一部分，与你 README/重启评估 的现有习惯一致。
5. **GitHub 桌面**：`File → Add Local Repository` → 选 `E:\onedrive-lin\OneDrive\开发\PSexp\源码`。
   若提示未登录：在桌面版里用浏览器授权登录 `linhuoxi` 一次（这步只能你本人做）。
6. `Repository → Publish repository` → **Visibility = Public** → 选 `linhuoxi` → Publish。

兜底（若第 5/6 步桌面版卡住）：`gh` 已验证授权可用，我一行命令建仓并推送，桌面版随后只用来翻历史。

---

## D. 去掉「混淆」与「内置压缩」（你这次要的）

先纠正一个概念，避免白干活：**混淆/压缩只影响你发出去的 EXE，跟"开源"无关**——开源公开的是源码。但你既然公开源码，混淆的保护意义就没了，去掉能让发布产物和源码一致、可调试。以下是实际改动点：

| # | 位置 | 现在 | 改成 |
| --- | --- | --- | --- |
| 1 | [源码/PSdodo/PSdodo.csproj:25](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/PSdodo/PSdodo.csproj#L25) | `EnableReleaseObfuscation` 在 Release 下默认 `true` | 默认 `false`（混淆整体停用） |
| 2 | [PSdodo.csproj:20](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/PSdodo/PSdodo.csproj#L20) | `EnableCompressionInSingleFile=true` | `false`（去掉内置压缩） |
| 3 | [PSdodo.csproj:72](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/PSdodo/PSdodo.csproj#L72) | `Import Obfuscation.targets` | **保留**。该 target 自带开关条件（[Obfuscation.targets:12-13](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/PSdodo/Obfuscation.targets#L12)），第 1 条关掉后它自动跳过；删文件反而不可逆 |
| 4 | [一键编译发布.bat:61](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E4%B8%80%E9%94%AE%E7%BC%96%E8%AF%91%E5%8F%91%E5%B8%83.bat#L61) | `-p:EnableReleaseObfuscation=true -p:ObfuscationRoot=...` | 改 `false` 并去掉 `ObfuscationRoot` 参数 |
| 5 | [一键编译发布.bat:66-72](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E4%B8%80%E9%94%AE%E7%BC%96%E8%AF%91%E5%8F%91%E5%B8%83.bat#L66) | 复制混淆后 DLL 再跑一次自测 | **必须删除**。不改这条的话第 68 行找不到 `%OBFUSCATION_ROOT%\output\PSexpV3.dll`，脚本直接跳到 `:obfuscated_test_failed` 报错终止——这是漏掉就必然失败的一处 |
| 6 | 同文件 第 12、23、114 行 | `OBFUSCATION_ROOT` 变量、标题"编译、混淆、压缩发布"、状态行"自包含+单文件+混淆+内置压缩" | 文案与变量同步去掉 |
| 7 | 同文件 [34-36 行](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E4%B8%80%E9%94%AE%E7%BC%96%E8%AF%91%E5%8F%91%E5%B8%83.bat#L34) + [源码/dotnet-tools.json](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/dotnet-tools.json) | 每次先 `dotnet tool restore` 拉 Obfuscar 2.2.50 | 停用后这一步不再必要；建议保留文件、只把脚本里的第 [1/6] 步降级为可选（省一次联网） |

### D 组预先声明的副作用

- **EXE 变大**：现在 48M 是含压缩的结果，去掉内置压缩后自包含单文件会明显增大。我不能提前报数，必须重编一次后拿实际字节回给你（当前体积 48M 是磁盘实测）。
- **可读性**：任何拿到 EXE 的人能直接读出 IL 与字符串（混淆含 `HideStrings=true`，见 [Obfuscation.targets:52](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/PSdodo/Obfuscation.targets#L52)）。源码公开后这项保护本就无意义，但这是实打实的改变，不是零代价。
- **收益**：脚本第 66 行原本写着"发布前的自测不能发现混淆器破坏类型引用的问题"——去掉混淆后这类风险直接消失，少一次二次自测。
- **跑脚本前要关软件**：第 86–93 行会替换正在运行的 EXE，`bat:157` 的错误提示就是"请先关闭正在运行的软件"。
- 改动只碰 csproj、bat、tools 清单，**不动任何业务 C# 代码**。

---

## E. 验收标准（编号，逐条可实测）

1. 在 `源码` 里执行 `git log --oneline` 至少返回 1 行；GitHub 桌面 History 面板能看到这条提交。
2. 网页端仓库为 Public，含 `LICENSE`（MIT）、README、`PSdodo`、`tests`、`references`；**页面里不应出现 `bin`、`obj`、EXE**。
3. `git count-objects -vH` 的 `size-pack` 为 MB 级（预期 ≤5M），不是 GB 级。
4. 提交作者邮箱显示为 `...@users.noreply.github.com`，不是 Gmail。
5. 重跑 `一键编译发布.bat` 一路到 `[6/6]` 全绿，最终状态行不再出现"混淆""内置压缩"字样，并回报新 EXE 确切体积。
6. 新 EXE 实机启动、五种导出模式（快速/全部画板/选中项/一级图层/切片）行为与旧版一致。**这条只能你在 Photoshop 里测，我无法验证。**
7. GitHub 桌面 Push/Pull 按钮变可用，之后你改代码能一键提交、看历史。

---

## F. 两个必须提前知道的坑

- **OneDrive 会同步 `.git`**：项目在 OneDrive 里，git 写对象文件时可能被 OneDrive 抢占，症状是 `index.lock: File exists` 之类的报错。缓解：把 `源码` 文件夹右键设为"始终要保留在此设备上"，并避开 OneDrive 正在大量上传的时段做提交。仓库只有 1M 级，风险不大但不是零。
- **48M 的 EXE 不进 git**：GitHub 单文件 100M 是硬限，且二进制产物会让历史迅速膨胀、永远删不掉。对外发版走 GitHub **Releases 附件**，同样公开可下载，而且不污染源码历史。
- **一旦 Public 就收不回**：`README.md` 与 `重启评估.md` 里有已知缺陷的坦白清单（例如 P1 切片漏导、取消后清理过早会丢结果，见 [重启评估.md](file:///E:/onedrive-lin/OneDrive/%E5%BC%80%E5%8F%91/PSexp/%E6%BA%90%E7%A0%81/%E9%87%8D%E5%90%AF%E8%AF%84%E4%BC%B0.md) 的"优先修复"节）。公开后外人会直接看到这些。按"最大限度开源"我默认**保留**（这是诚实的工程记录，也正是最大透明的代价）；你要撤，我只需把这两份挪出仓库或加进 `.gitignore`——但要在第一次提交**之前**定，提交后再抹历史要费事得多。

---

## G. 需要你给的一句话

计划里 D 组（去混淆/去压缩）和 C 组第 6 步（发布成 Public）是两件事，可以分开给时间。**发布 Public 是不可逆的对外动作，我一定等你明确说"发"才做**；其余本地建库、改配置都是可回滚的。
