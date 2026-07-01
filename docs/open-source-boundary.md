# 开源边界

这个仓库只发布第三方 Mod 开发所需内容。

## 可以开源

- `ModAgent.Abstractions`：接口、Attribute、DTO、枚举。
- `ModAgent.Mod.Sdk`：模板、Manifest 生成、打包校验。
- `ModAgent.Mod.TestKit`：Mock Context、Fake 服务、测试辅助。
- 示例 Mod 和文档。
- DevHost 的使用文档。

## 不开源

- 宿主核心实现。
- 桌面 UI 源码。
- 模型路由和计费策略。
- 权限执行内部逻辑。
- 数据库、同步、浏览器 Profile 管理实现。
- 用户配置、密钥和私有服务端代码。

## 兼容承诺

`ModAgent.Abstractions` 使用语义化版本：

- `1.x`：保持二进制和源码兼容，允许新增接口默认实现和 DTO 字段。
- `2.0`：允许破坏性变更，需要迁移指南。
- 宿主内部可以自由重构，但不能要求 Mod 引用内部类型。
