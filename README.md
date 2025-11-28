# ModChangelogCenter 接入指南

> **当前版本**: V1.2.0 (2025-11-28)
>
> **Steam Workshop**: [ModChangelogCenter](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839)

本文档说明如何将你的模组接入 ModChangelogCenter，实现：
- 📋 **更新日志展示**：在游戏内显示你的模组更新记录
- 🔔 **版本更新检测**：自动检查 Steam Workshop 上是否有新版本（V1.1.0+）
- 🔗 **一键跳转更新**：点击更新提示直接打开 Workshop 页面（V1.2.0+）

---

## 目录

- [快速开始](#快速开始)
- [更新日志注册](#更新日志注册)
- [版本检查注册（V1.1.0+）](#版本检查注册v110)
- [JSON 格式说明](#json-格式说明)
- [完整示例代码](#完整示例代码)
- [常见问题](#常见问题)

---

## 快速开始

### 项目结构

```
├─ README.md                    # 本文件
├─ ModChangelogCenterSample.csproj
├─ info.ini
├─ ModBehaviour.cs
├─ ChangelogRegistrar.cs
├─ Changelog/
│  └─ SampleChangelog.json
├─ .gitignore
├─ bin/                         # 构建产物（build 后出现）
└─ obj/                         # 编译中间文件
```

### 构建

```bash
dotnet build ModChangelogCenterSample/ModChangelogCenterSample/ModChangelogCenterSample.csproj -c Release
```

编译后会在 `bin/Release/netstandard2.1/ModChangelogCenterSample.dll` 生成示例 DLL。正常部署时将其复制到 `Duckov_Data/Mods/ModChangelogCenterSample/` 即可。

## 引入 ModChangelogCenter.dll 的方式

示例项目默认假设你在 `E:\steam\steamapps\common\Escape from Duckov` 安装了游戏，并在 `Duckov_Data\Mods\ModChangelogCenter\` 文件夹内具有 ModChangelogCenter（若通过创意工坊订阅，DLL 位于 `E:\steam\steamapps\workshop\content\3167020\<模组编号>\ModChangelogCenter\ModChangelogCenter.dll`，可复制或直接引用）。可参考以下方式：

1. **直接引用游戏目录中的 DLL（默认方式）**
   在 `ModChangelogCenterSample.csproj` 中包含：
   ```xml
   <Reference Include="ModChangelogCenter">
     <HintPath>$(DuckovPath)\Duckov_Data\Mods\ModChangelogCenter\ModChangelogCenter.dll</HintPath>
   </Reference>
   ```
   只需把 `DuckovPath` 属性替换成你的实际路径即可。

2. **引用创意工坊订阅目录**
   如果你把 ModChangelogCenter 安装在 Steam 创意工坊（例如 `steamapps/workshop/content/3167020/<订阅ID>/ModChangelogCenter.dll`），可以修改 `HintPath` 指向该位置，或通过相对路径复制 DLL。

3. **自带一份 DLL**
   对于不方便调整 csproj 的场景，可以把 ModChangelogCenter 的 DLL 放在示例项目的 `libs/` 目录，并设置 `HintPath` 指向该文件。示例：
   ```xml
   <Reference Include="ModChangelogCenter">
     <HintPath>libs\ModChangelogCenter.dll</HintPath>
   </Reference>
   ```

无论哪种方案，只要能在编译时找到 ModChangelogCenter.dll 即可。

---

## 更新日志注册

### API 签名

```csharp
// ModChangelogCenter.ChangelogRegistry
public static bool RegisterModule(
    string moduleId,        // 模组唯一标识（建议与文件夹名一致）
    string displayNameZh,   // 中文显示名称
    string displayNameEn,   // 英文显示名称
    string jsonPayload,     // 更新日志 JSON 字符串
    bool isInternal = false // 是否为内部模组（一般传 false）
);
```

### 注册方式

#### 方式 1：反射调用（推荐，兼容性最好）

```csharp
using HarmonyLib;
using System.Reflection;

// 检查 ModChangelogCenter 是否已加载
Type? registryType = AccessTools.TypeByName("ModChangelogCenter.ChangelogRegistry");
if (registryType == null)
{
    Debug.Log("[YourMod] ModChangelogCenter 未安装，跳过更新日志注册");
    return;
}

// 获取注册方法
MethodInfo? registerMethod = registryType.GetMethod(
    "RegisterModule",
    new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool) });

// 调用注册
object? result = registerMethod?.Invoke(null, new object[] {
    "YourModId",           // moduleId
    "你的模组名称",         // 中文名
    "Your Mod Name",       // 英文名
    jsonPayload,           // JSON 字符串
    false                  // isInternal
});

bool success = result is bool flag && flag;
```

#### 方式 2：直接引用（需添加 DLL 引用）

```csharp
using ModChangelogCenter;

bool ok = ChangelogRegistry.RegisterModule(
    "YourModId",
    "你的模组名称",
    "Your Mod Name",
    jsonPayload,
    isInternal: false);
```

---

## 版本检查注册（V1.1.0+）

> **新功能**：ModChangelogCenter V1.1.0 起支持自动检查 Steam Workshop 更新

### API 签名

```csharp
// ModChangelogCenter.ChangelogRegistry
public static bool RegisterVersionCheck(
    string moduleId,    // 模组 ID（需与 RegisterModule 时的 ID 一致）
    ulong workshopId    // Steam Workshop 物品 ID
);
```

### 获取 Workshop ID

从你的模组 Steam Workshop 页面 URL 中获取：
```
https://steamcommunity.com/sharedfiles/filedetails/?id=3595314238
                                                      ^^^^^^^^^^
                                                      这就是 Workshop ID
```

### 注册示例（反射方式）

```csharp
private const ulong WorkshopId = 3595314238UL;  // 替换为你的 Workshop ID

private static void TryRegisterVersionCheck(Type registryType)
{
    // 查找版本检查方法（V1.1.0+ 才有）
    MethodInfo? versionCheckMethod = registryType.GetMethod(
        "RegisterVersionCheck",
        new[] { typeof(string), typeof(ulong) });

    if (versionCheckMethod == null)
    {
        // ModChangelogCenter 版本过低，不支持版本检查
        Debug.Log("[YourMod] ModChangelogCenter 不支持版本检查（需要 V1.1.0+）");
        return;
    }

    // 调用注册
    object? result = versionCheckMethod.Invoke(null, new object[] {
        "YourModId",    // 与 RegisterModule 的 moduleId 一致
        WorkshopId
    });

    if (result is bool success && success)
    {
        Debug.Log($"[YourMod] 已注册版本检查 (Workshop ID: {WorkshopId})");
    }
}
```

### 版本号来源

- **本地版本**：自动从 `Mods/YourModId/info.ini` 的 `version=` 行读取
- **远程版本**：从 Steam Workshop 物品标题中解析（支持 V2.10、v2.10、Ver.2.10 等格式）

### UI 效果

| 状态 | 显示效果 |
|------|----------|
| 有更新 | 橙色文字：`有更新: 2.9 → 2.10（点击更新）` |
| 已最新 | 绿色文字：`已是最新` |
| 主菜单 | 红色角标显示需要更新的模组数量 |

---

## JSON 格式说明

### 字段说明

```json
{
  "entries": [
    {
      "version": "V2.0",
      "updatedAt": "2025-11-28",
      "zh": [
        "新增版本更新检测功能",
        "优化用户界面体验"
      ],
      "en": [
        "Added version update checking",
        "Improved UI experience"
      ]
    },
    {
      "version": "V1.0",
      "updatedAt": "2025-11-15",
      "zh": ["首次发布"],
      "en": ["Initial release"]
    }
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `entries` | array | ✅ | 版本条目数组，新版本放前面 |
| `version` | string | ✅ | 版本号，显示在面板中 |
| `updatedAt` | string | ❌ | 更新日期，显示在右上角 |
| `zh` | string[] | ❌ | 中文更新内容（缺失时 fallback 到 en） |
| `en` | string[] | ❌ | 英文更新内容（缺失时 fallback 到 zh） |

### 嵌入资源配置

在 `.csproj` 中添加：

```xml
<ItemGroup>
  <EmbeddedResource Include="Changelog\YourChangelog.json" />
</ItemGroup>
```

---

## 完整示例代码

### ChangelogCenterBridge.cs（推荐的完整实现）

```csharp
using System;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace YourMod
{
    internal static class ChangelogCenterBridge
    {
        // 嵌入资源路径（命名空间.文件夹.文件名）
        private const string ResourcePath = "YourMod.Changelog.ChangelogData.json";

        // Steam Workshop 发布 ID（从你的 Workshop 页面 URL 获取）
        private const ulong WorkshopId = 1234567890UL;

        private static bool _registered;
        private static bool _versionCheckRegistered;

        /// <summary>
        /// 在 ModBehaviour.OnAfterSetup() 中调用
        /// </summary>
        internal static void TryRegister()
        {
            if (_registered) return;

            // 检查 ModChangelogCenter 是否已加载
            Type? registryType = AccessTools.TypeByName("ModChangelogCenter.ChangelogRegistry");
            if (registryType == null)
            {
                Debug.Log("[YourMod] ModChangelogCenter 未安装，跳过更新日志注册");
                return;
            }

            // 读取嵌入的 JSON
            string? json = LoadEmbeddedJson();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[YourMod] 找不到更新日志资源");
                return;
            }

            // 注册更新日志
            if (TryRegisterChangelog(registryType, json))
            {
                _registered = true;
                Debug.Log("[YourMod] 已注册更新日志");

                // 注册版本检查（V1.1.0+）
                TryRegisterVersionCheck(registryType);
            }
        }

        private static string? LoadEmbeddedJson()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream? stream = assembly.GetManifestResourceStream(ResourcePath);
                if (stream == null) return null;

                using StreamReader reader = new(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[YourMod] 读取更新日志失败: {ex.Message}");
                return null;
            }
        }

        private static bool TryRegisterChangelog(Type registryType, string json)
        {
            MethodInfo? method = registryType.GetMethod(
                "RegisterModule",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool) });

            if (method == null)
            {
                Debug.LogWarning("[YourMod] ModChangelogCenter 版本不兼容");
                return false;
            }

            try
            {
                object? result = method.Invoke(null, new object[] {
                    ModBehaviour.ModIdentifier,  // 你的模组 ID
                    "你的模组名称",               // 中文名
                    "Your Mod Name",             // 英文名
                    json,
                    false
                });
                return result is bool success && success;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[YourMod] 注册失败: {ex.Message}");
                return false;
            }
        }

        private static void TryRegisterVersionCheck(Type registryType)
        {
            if (_versionCheckRegistered) return;

            MethodInfo? method = registryType.GetMethod(
                "RegisterVersionCheck",
                new[] { typeof(string), typeof(ulong) });

            if (method == null)
            {
                // V1.1.0 之前的版本不支持
                Debug.Log("[YourMod] ModChangelogCenter 不支持版本检查（需要 V1.1.0+）");
                return;
            }

            try
            {
                object? result = method.Invoke(null, new object[] {
                    ModBehaviour.ModIdentifier,
                    WorkshopId
                });

                if (result is bool success && success)
                {
                    _versionCheckRegistered = true;
                    Debug.Log($"[YourMod] 已注册版本检查 (Workshop ID: {WorkshopId})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[YourMod] 版本检查注册失败: {ex.Message}");
            }
        }
    }
}
```

### ModBehaviour.cs 中调用

```csharp
protected override void OnAfterSetup()
{
    // 注册更新日志和版本检查
    ChangelogCenterBridge.TryRegister();
}
```

---

## 常见问题

### Q: 需要硬性依赖 ModChangelogCenter 吗？

**不需要**。使用反射方式注册时，如果用户没有安装 ModChangelogCenter，你的模组依然正常运行，只是没有更新日志展示功能。

### Q: 版本检查的版本号从哪里读取？

- **本地版本**：自动从 `Mods/你的模组ID/info.ini` 的 `version=` 行读取
- **远程版本**：从 Steam Workshop 物品**标题**中解析

例如你的 Workshop 标题是 `史诗级背包扩容 V2.10`，则远程版本为 `2.10`。

### Q: 如何触发版本更新提示？

只需更新 Steam Workshop 标题中的版本号即可。例如：
- 旧标题：`我的模组 V1.0`
- 新标题：`我的模组 V1.1`

玩家下次启动游戏时，主菜单按钮会显示红色角标，面板中会显示橙色更新提示。

### Q: 老版本 ModChangelogCenter 兼容吗？

**兼容**。代码通过反射检查 `RegisterVersionCheck` 方法是否存在：
- V1.1.0+：完整功能（更新日志 + 版本检查）
- V1.0.x：仅更新日志功能，版本检查静默跳过

### Q: 如何更新更新日志？

1. 编辑 `Changelog/YourChangelog.json`，在 `entries` 数组开头添加新版本
2. 重新编译 DLL
3. 上传到 Steam Workshop
4. 更新 Workshop 标题中的版本号

### Q: 没有 Steam Workshop 怎么办？

版本检查功能需要 Workshop ID，如果你的模组不在 Workshop 上，可以：
- 只注册更新日志，不调用 `RegisterVersionCheck`
- 版本检查功能会静默跳过，不会报错
