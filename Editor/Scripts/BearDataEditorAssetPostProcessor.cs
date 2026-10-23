using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor {
    public class BearDataEditorAssetPostProcessor : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        var validAssetFileEndings = new List<string> { ".asset", ".prefab" };

        var changedAssets = new List<string>();
            changedAssets.AddRange(importedAssets);
            changedAssets.AddRange(deletedAssets);

            var editorsNeedUpdate = changedAssets.Any(a => validAssetFileEndings
            .Contains(Path.GetExtension(a.ToLower())));

            if (editorsNeedUpdate) {
                foreach (var window in Resources.FindObjectsOfTypeAll<BearDataEditorWindow>()) {
                    window.RefreshObjects();
                    window.Repaint();
                }
            }
        }
    }
}