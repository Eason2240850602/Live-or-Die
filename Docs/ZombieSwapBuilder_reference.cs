// ============================================================================
// 换模构建器 · 归档参考（不参与编译：位于 Assets/ 之外）
//
// 来历：资产接入 v2 段 1（写实丧尸实验换模，2026-07-22）。
//       实验判决=写实路线否决、A 案(低模 3D)定案，换模已回退；
//       本文件按 PM 指示保留，**下一单接 Synty 低模资产时直接复用**。
//
// 用法：复制到 Assets/Editor/ 下，改 ModelPath 指向新模型的 URP prefab，
//       batchmode -executeMethod ZombieSwapBuilder.Build 跑，跑完即删。
//
// 核心红线（子物体替换法）：
//   模型挂为现有 GameObject 的子物体，只关原胶囊 MeshRenderer；
//   Collider / 脚本 / Tag / Layer 一律不动 —— 逻辑体与视觉体分离，玩法零改动。
//
// 两个踩过的坑（务必保留）：
//   1) 改"从场景加载的已有 prefab 实例"的组件属性，必须 EditorUtility.SetDirty +
//      PrefabUtility.RecordPrefabInstancePropertyModifications，否则 SaveScene
//      静默丢弃（日志照样报成功）。本脚本经 InstantiatePrefab 挂子物体那一路会
//      连带记录父实例修改，但回退脚本手改 mr.enabled 时必须显式调用。
//   2) 核验必须 grep 场景文件的 `propertyPath: m_Enabled` 格式，
//      不是 `m_Enabled: 0`（后者是非实例对象的写法，会误报为 0 处）。
//
// 缩放要点：父物体自带缩放（爬尸 y=0.5、博士 1.3）时，子物体缩放要除掉父缩放，
//           否则二次形变。脚底对齐 = localPosition.y = -1（胶囊中心在原点）。
//           横版侧视朝向 = localRotation Y+90°。
// ============================================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ZombieSwapBuilder
{
    const string ModelPath = "Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab";   // ← 换 Synty 时改这里

    public static void Build()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) { Debug.LogError("[ZombieSwap] 找不到 " + ModelPath + "，停机"); return; }

        var probe = (GameObject)PrefabUtility.InstantiatePrefab(model);
        float rawH = MeasureHeight(probe);
        Object.DestroyImmediate(probe);
        Debug.Log($"[ZombieSwap] 模型原始高度 {rawH:F2}m");

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Institute.unity", OpenSceneMode.Single);

        int swapped = 0, skipped = 0;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.IsPartOfPrefabAsset(z)) continue;
            if (z.transform.Find("ModelView") != null) { skipped++; continue; }   // 幂等

            var go = z.gameObject;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            var view = (GameObject)PrefabUtility.InstantiatePrefab(model);
            view.name = "ModelView";
            view.transform.SetParent(go.transform, false);

            float capsuleH = 2f * go.transform.localScale.y;
            float targetH = Mathf.Min(capsuleH, 1.8f);
            float s = rawH > 0.01f ? targetH / rawH : 1f;
            var ps = go.transform.localScale;
            view.transform.localScale = new Vector3(s / ps.x, s / ps.y, s / ps.z);   // 除掉父缩放

            view.transform.localPosition = new Vector3(0f, -1f, 0f);                 // 脚底对齐
            view.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);            // 横版侧视朝向

            swapped++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        bool ok = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Institute.unity");
        Debug.Log($"[ZombieSwap] SceneSaved={ok} : 换模 {swapped} 只, 跳过(已换) {skipped} 只");
    }

    static float MeasureHeight(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 0f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size.y;
    }
}

// ---------------------------------------------------------------------------
// 配套回退脚本（换模翻车时用）
// ---------------------------------------------------------------------------
public static class ZombieSwapRollback
{
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Institute.unity", OpenSceneMode.Single);

        int removed = 0, restored = 0;
        foreach (var z in Object.FindObjectsByType<ZombieController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.IsPartOfPrefabAsset(z)) continue;

            var view = z.transform.Find("ModelView");
            if (view != null) { Object.DestroyImmediate(view.gameObject); removed++; }

            var mr = z.GetComponent<MeshRenderer>();
            if (mr != null && !mr.enabled)
            {
                mr.enabled = true;
                EditorUtility.SetDirty(mr);
                PrefabUtility.RecordPrefabInstancePropertyModifications(mr);   // 坑1：必须显式记录
                restored++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        bool ok = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Institute.unity");
        Debug.Log($"[Rollback] SceneSaved={ok} : 移除模型 {removed}, 恢复胶囊 {restored}");
    }
}
