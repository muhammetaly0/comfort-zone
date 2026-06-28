using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Seçili objelerin (ve child'larının) tüm MeshRenderer/SkinnedMeshRenderer
// materyallerine Toon shader'ı toplu uygular.
public static class ToonShaderApplier
{
    private const string ToonShaderPath = "Toon Shaders Pro/URP/Toon";

    [MenuItem("Tools/Toon Shaders/Seçilenlere Toon Shader Uygula")]
    private static void ApplyToSelected()
    {
        Shader toonShader = Shader.Find(ToonShaderPath);
        if (toonShader == null)
        {
            Debug.LogError($"Shader bulunamadı: '{ToonShaderPath}'. Adı/yolu farklıysa scriptteki ToonShaderPath'i güncelle.");
            return;
        }

        var processedMaterials = new HashSet<Material>();
        int rendererCount = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat == null || !processedMaterials.Add(mat)) continue;

                    Undo.RecordObject(mat, "Apply Toon Shader");
                    mat.shader = toonShader;
                }
                rendererCount++;
            }
        }

        Debug.Log($"Toon shader {processedMaterials.Count} materyale, {rendererCount} renderer üzerinden uygulandı.");
    }
}
