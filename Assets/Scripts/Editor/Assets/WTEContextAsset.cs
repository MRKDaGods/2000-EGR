using MRK.Server.WTE;
using System.IO;
using UnityEditor;
using UnityEngine;

public class WTEContextAsset : MonoBehaviour {
    [MenuItem("EGR/Assets/WTE Context")]
    static void Main() {
        Context ctx = ScriptableObject.CreateInstance<Context>();
        AssetDatabase.CreateAsset(ctx, "Assets/WTE Context.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = ctx;
    }

    [MenuItem("EGR/Assets/Export WTE Context")]
    static void ExportWTECtx() {
        string assetPath = "Assets/Server/WTE Context.asset";
        Context ctx = AssetDatabase.LoadAssetAtPath<Context>(assetPath);

        using (FileStream stream = new FileStream($"{Application.dataPath}\\Server\\UDB.2000", FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream)) {
            ctx.Write(writer);
            writer.Close();
        }

        Debug.Log("DONE");
    }
}
