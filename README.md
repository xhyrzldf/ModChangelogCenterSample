# ModChangelogCenter 接入示例（前置模组模板）

本 README 是整个仓库的唯一说明文件。仓库只包含下列内容，方便作者直接 clone 后提交到自己的 GitHub / Steam 前置：

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


## 构建

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

## 注册方式（多种示例）

1. **直接引用 ModChangelogCenter 命名空间**  
   ```csharp
   using ModChangelogCenter;
   
   bool ok = ChangelogRegistry.RegisterModule(
       "YourModuleId",
       "模组中文名称",
       "Module English Name",
       jsonPayload,
       isInternal: false);
   ```
   适合你确定前置 DLL 会随游戏一同部署的情况。

2. **反射调用（本仓库默认方式）**  
   ```csharp
   MethodInfo? register = AccessTools.Method(
       "ModChangelogCenter.ChangelogRegistry:RegisterModule",
       new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool) });
   object? result = register?.Invoke(null, new object[] { moduleId, zhName, enName, jsonPayload, false });
   bool ok = result is bool flag && flag;
   ```
   无需在编译期添加引用，适合只想在运行时检查前置是否存在的情况。

3. **公共 API（对其他模组开放）**  
   ```csharp
   public static bool RegisterExternal(string moduleId, string zhName, string enName, string json)
   {
       object? result = RegisterModuleMethod?.Invoke(null, new object[] { moduleId, zhName, enName, json, false });
       return result is bool flag && flag;
   }
   ```
   其它模组引用你的 DLL 后即可调用，无需重复写注册逻辑。

4. **外部 JSON / 动态刷新**  
   ```csharp
   string json = File.ReadAllText(changelogPath, Encoding.UTF8);
   ChangelogRegistry.RegisterModule(moduleId, zhName, enName, json);
   ```
   JSON 可以来自嵌入资源、磁盘、网络等任意位置。想动态刷新时再次调用即可覆盖旧记录。

## JSON 格式与注册流程

### 1. 编写 changelog JSON

示例文件：`Changelog/SampleChangelog.json`

```json
{
  "entries": [
    {
      "version": "V1.0",
      "updatedAt": "2025-11-15",
      "zh": [
        "- 中文内容 1",
        "- 中文内容 2"
      ],
      "en": [
        "- English text 1",
        "- English text 2"
      ]
    }
  ]
}
```

- `entries`: 数组。按新旧顺序排列即可，面板会自动排序。
- `version`: 在面板中显示的版本号。
- `updatedAt` (可选): 字符串，显示在标题右上角。
- `zh` / `en`: 字符串数组。缺失 `zh` 时会 fallback 到 `en`，反之亦然。

你可以改为从外部文件加载，只要最终得到 JSON 字符串传给 Register 方法即可。

### 2. 在模组初始化时注册

`ModBehaviour.cs`:

```csharp
protected override void OnAfterSetup()
{
    if (ChangelogRegistrar.TryRegisterEmbeddedChangelog(out string? msg))
    {
        Debug.Log("[YourMod] changelog registered.");
    }
    else
    {
        Debug.LogWarning($"[YourMod] register failed: {msg}");
    }
}
```

`ChangelogRegistrar.cs` 逻辑：
- 通过 `Assembly.GetManifestResourceStream` 读取嵌入的 JSON。
- 使用 `HarmonyLib.AccessTools` 找到 `ModChangelogCenter.ChangelogRegistry.RegisterModule`。
- 以 `RegisterModule(moduleId, zhName, enName, json, isInternal: false)` 的方式调用。

如果你希望其他模组调用你的前置，可以把 `TryRegisterEmbeddedChangelog` 改为 `public static`，甚至提供 `TryRegisterFromJson(string moduleId, string json)` 之类的包装，以便他们传入自己的 JSON。

### 3. 部署/更新

- 更新 changelog 后重新编译即可；`RegisterModule` 会覆盖同 moduleId 的旧记录。
- 若要在运行时动态刷新（例如读取磁盘文件），可以在适合的时机再次调用 `RegisterModule`。
- 确保发布 DLL 时附带 `info.ini`，以便 Duckov 识别模组。

## 常见问题

- **需要 ModConfig 吗？** 不需要。该方案只依赖 ModChangelogCenter.dll。
- **如何只显示新版本？** 在注册前比较版本号或读取本地配置，决定是否调用 `RegisterModule` 即可。
- **可以让其他模组调用我的前置吗？** 可以。把 `ChangelogRegistrar` 对外公开方法（或提供简单的 API 类），其它模组引用你的 DLL 后直接调用即可。
- **如果没有创意工坊怎么办？** 复制 ModChangelogCenter DLL 到任意目录，在 csproj 中调整 `HintPath` 指向即可。
