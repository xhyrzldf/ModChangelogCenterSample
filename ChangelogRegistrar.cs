using System;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace ModChangelogCenterSample
{
    /// <summary>
    /// 简易注册器：嵌入 JSON → 调用 ModChangelogCenter.RegisterModule。
    /// 按照注释步骤替换 ModuleId、名称和 JSON 即可。
    /// </summary>
    internal static class ChangelogRegistrar
    {
        // TODO: 这里替换成你自己的模组识别信息
        private const string ModuleId = "ModChangelogCenter.Sample";            // 唯一 ID，不要和别人重复
        private const string DisplayNameZh = "ModChangelogCenter 示例模组";      // 中文展示名称
        private const string DisplayNameEn = "ModChangelogCenter Sample Mod";   // 英文展示名称

        // 采用“嵌入资源”做示例：把 JSON 设为 EmbeddedResource 后，路径 = {命名空间}.{文件夹}.{文件名}
        private const string EmbeddedResourceName = "ModChangelogCenterSample.Changelog.SampleChangelog.json";

        private static readonly MethodInfo? RegisterModuleMethod =
            AccessTools.Method("ModChangelogCenter.ChangelogRegistry:RegisterModule",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool) });

        internal static bool TryRegisterEmbeddedChangelog(out string? message)
        {
            try
            {
                if (RegisterModuleMethod == null)
                {
                    message = "未能找到 ChangelogRegistry.RegisterModule 方法。";
                    return false;
                }

                // Step1: 读取嵌入的 JSON（若改走本地文件，直接替换此段即可）
                Assembly assembly = typeof(ChangelogRegistrar).Assembly;
                using Stream? stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
                if (stream == null)
                {
                    message = $"未找到资源：{EmbeddedResourceName}";
                    return false;
                }

                using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string json = reader.ReadToEnd();

                // Step2: 调用 RegisterModule（moduleId、中文名、英文名、json、是否内置）
                object? result = RegisterModuleMethod.Invoke(null, new object[] { ModuleId, DisplayNameZh, DisplayNameEn, json, false });
                bool ok = result is bool flag && flag;
                message = ok ? $"moduleId={ModuleId}" : "RegisterModule 返回 false";
                return ok;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        // ==== 可选方案（看需求取消注释） ====
        //
        // 方案 A：直接引用 ModChangelogCenter 命名空间（需要在 csproj 里引用 DLL）
        // using ModChangelogCenter;
        // internal static bool TryRegisterDirect(string json) =>
        //     ChangelogRegistry.RegisterModule(ModuleId, DisplayNameZh, DisplayNameEn, json, false);
        //
        // 方案 B：从磁盘读取 JSON
        // internal static bool TryRegisterFromFile(string filePath, string moduleId, string zhName, string enName)
        // {
        //     if (!File.Exists(filePath))
        //     {
        //         Debug.LogWarning($"未找到 changelog 文件：{filePath}");
        //         return false;
        //     }
        //     string json = File.ReadAllText(filePath, Encoding.UTF8);
        //     object? result = RegisterModuleMethod?.Invoke(null, new object[] { moduleId, zhName, enName, json, false });
        //     return result is bool flag && flag;
        // }
        //
        // 方案 C：对其他模组公开注册 API
        // public static bool RegisterExternal(string moduleId, string zhName, string enName, string json)
        // {
        //     object? result = RegisterModuleMethod?.Invoke(null, new object[] { moduleId, zhName, enName, json, false });
        //     return result is bool flag && flag;
        // }
    }
}
