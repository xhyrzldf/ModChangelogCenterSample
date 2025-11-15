using Duckov.Modding;
using UnityEngine;

namespace ModChangelogCenterSample
{
    public sealed class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        protected override void OnAfterSetup()
        {
            // 示例：在模组初始化阶段注册 changelog
            if (ChangelogRegistrar.TryRegisterEmbeddedChangelog(out string? message))
            {
                Debug.Log($"[ModChangelogCenterSample] 已注册示例更新日志。{message}");
            }
            else if (!string.IsNullOrEmpty(message))
            {
                Debug.LogWarning($"[ModChangelogCenterSample] 注册更新日志失败：{message}");
            }
        }
    }
}
