#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using OrbitGuard;

[InitializeOnLoad]
public static class OrbitGuardSetup
{
    static OrbitGuardSetup() { EditorApplication.delayCall += EnsureCampaign; }
    static void EnsureCampaign()
    {
        const string path = "Assets/Resources/Campaign.asset";
        if (AssetDatabase.LoadAssetAtPath<CampaignDefinition>(path) != null) return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CampaignDefinition>(), path);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Orbit Guard/Select Campaign")]
    static void SelectCampaign() { EnsureCampaign(); Selection.activeObject = AssetDatabase.LoadAssetAtPath<CampaignDefinition>("Assets/Resources/Campaign.asset"); }
}
#endif
