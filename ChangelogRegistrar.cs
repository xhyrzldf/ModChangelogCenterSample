using System;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace ModChangelogCenterSample
{
    internal static class ChangelogRegistrar
    {
        private const string ModuleId = "ModChangelogCenter.Sample";
        private const string DisplayNameZh = "ModChangelogCenter 示例模组";
        private const string DisplayNameEn = "ModChangelogCenter Sample Mod";
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

                Assembly assembly = typeof(ChangelogRegistrar).Assembly;
                using Stream? stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
                if (stream == null)
                {
                    message = $"未找到资源：{EmbeddedResourceName}";
                    return false;
                }

                using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string json = reader.ReadToEnd();
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
    }
}
