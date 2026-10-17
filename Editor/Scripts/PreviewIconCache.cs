using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor {
    [InitializeOnLoad]
    public static class PreviewIconCache {
        private const int PreviewCacheRevision = 1;
        private static readonly string RevisionPreferenceKey = "CollisionBear.BearDataEditor.PreviewCacheRevision:" + Application.dataPath;

        static PreviewIconCache() {
            EditorApplication.delayCall += RefreshStalePreviewsWhenReady;
        }

        private static void RefreshStalePreviewsWhenReady() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += RefreshStalePreviewsWhenReady;
                return;
            }

            if (EditorPrefs.GetInt(RevisionPreferenceKey, 0) == PreviewCacheRevision) {
                return;
            }

            ReimportAssetsWithPreviewIcons();
            EditorPrefs.SetInt(RevisionPreferenceKey, PreviewCacheRevision);
            ClearInMemoryPreviewCache();
            EditorApplication.RepaintProjectWindow();
        }

        private static void ReimportAssetsWithPreviewIcons() {
            var assetPaths = FindPreviewIconAssetPaths();
            if (assetPaths.Count == 0) {
                return;
            }

            AssetDatabase.StartAssetEditing();
            try {
                foreach (var assetPath in assetPaths) {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static HashSet<string> FindPreviewIconAssetPaths() {
            var assetPaths = new HashSet<string>();
            foreach (var assetType in FindConcreteTypesWithPreviewIcons()) {
                AddAssetPathsForType(assetType, assetPaths);
            }

            return assetPaths;
        }

        private static List<Type> FindConcreteTypesWithPreviewIcons() {
            var assetTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                AddTypesWithPreviewIcons(assembly, assetTypes);
            }

            return assetTypes;
        }

        private static void AddTypesWithPreviewIcons(Assembly assembly, List<Type> assetTypes) {
            Type[] assemblyTypes;
            try {
                assemblyTypes = assembly.GetTypes();
            } catch (ReflectionTypeLoadException loadFailure) {
                assemblyTypes = loadFailure.Types;
            }

            if (assemblyTypes == null) {
                return;
            }

            foreach (var assetType in assemblyTypes) {
                if (!IsConcreteScriptableObject(assetType) || !PreviewIconUtility.HasPreviewIconField(assetType)) {
                    continue;
                }

                assetTypes.Add(assetType);
            }
        }

        private static bool IsConcreteScriptableObject(Type assetType) {
            return assetType != null
                && !assetType.IsAbstract
                && typeof(ScriptableObject).IsAssignableFrom(assetType);
        }

        private static void AddAssetPathsForType(Type assetType, HashSet<string> assetPaths) {
            foreach (var guid in AssetDatabase.FindAssets("t:" + assetType.Name)) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                if (asset != null) {
                    assetPaths.Add(assetPath);
                }
            }
        }

        private static void ClearInMemoryPreviewCache() {
            var clearTemporaryPreviews = typeof(AssetPreview).GetMethod(
                "ClearTemporaryAssetPreviews",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            clearTemporaryPreviews?.Invoke(null, null);
        }
    }
}
