using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "drc")]
public class DracoScriptedImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        Material defaultMaterial = GetDefaultMaterial();
        GameObject mainGameObject = new GameObject();
        List<Mesh> meshes = new List<Mesh>();
        DracoMeshLoader dracoLoader = new DracoMeshLoader();
        
        dracoLoader.ConvertDracoMeshToUnity(
            File.ReadAllBytes(ctx.assetPath),
            ref meshes);

        if (meshes != null)
        {
            for (int i = 0; i < meshes.Count; ++i)
            {
                meshes[i].RecalculateBounds();
                meshes[i].name = string.Format("mesh-{0}", i);
                ctx.AddObjectToAsset(meshes[i].name, meshes[i]);

                GameObject subObject = new GameObject();
                subObject.name = "_scan-" + i.ToString();
                subObject.transform.SetParent(mainGameObject.transform);
                subObject.transform.localScale = new Vector3(1, -1, 1);
                MeshFilter meshFilter = subObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = subObject.AddComponent<MeshRenderer>();
                meshFilter.mesh = meshes[i];
                meshRenderer.sharedMaterial = defaultMaterial;                
                ctx.AddObjectToAsset(subObject.name, subObject);
            }
        }
        ctx.AddObjectToAsset("root", mainGameObject);
        ctx.SetMainObject(mainGameObject);
    }

    private Material GetDefaultMaterial()
    {
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        primitive.hideFlags = HideFlags.HideInHierarchy;
        Material defaultMaterial = primitive.GetComponent<MeshRenderer>().sharedMaterial;
        DestroyImmediate(primitive);
        return defaultMaterial;
    }
}