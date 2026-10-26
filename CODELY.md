

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-10-26 13:24:42] HDQBZ 项目已从 Unity 2022.3.62f3 迁移到团结引擎 1.10.2（2022.3.62t14），默认 UPM 注册表变为 https://packages.tuanjie.cn；该镜像不含 com.unity.localization 1.5.13（最高 1.5.3，且 addressables 最高 1.23.2-t1），2026-10-26 已将 localization 固定为 1.5.3 以走团结原生依赖链，若要升回 1.5.13 需额外接入 packages.unity.com 作用域注册表并连带解析整条 Unity 侧依赖。
- [2026-10-26 13:24:45] HDQBZ 项目在团结引擎下重新导入资源时，会把 Assets 内 .meta 的 guid 从 32 位十六进制改写成加密 base64（如 d770de4b… → W30YtX74…=）并新增团结专有字段，一次迁移会产生数百个 .meta 的 diff，属正常行为；确认未破坏引用后再提交。另：Assets/Mutiny/Localization/Tables 表资源落后于 Mutiny.tsv（165 vs 167 键，缺 frontend.cheats_off / frontend.cheats_on），需执行菜单 Mutiny/Localization/Rebuild String Tables 重建。

### Reference

