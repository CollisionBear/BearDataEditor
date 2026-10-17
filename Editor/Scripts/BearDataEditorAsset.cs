using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor
{ 
    [System.Serializable]
    public class BearDataEditorAsset
    { 
        public Object Object;
        public string Name;
        public string Path;
        [System.NonSerialized]
        public Texture2D PreviewTexture;
        public int Index;
        public int FilteredIndex;

        public BearDataEditorAsset()
        {
            Object = null;
            Name = "None";
            Path = string.Empty;
        }

        public BearDataEditorAsset(Object o)
        {
            Object = o;
            Name = Object.name;
            PreviewTexture = GetPreviewTexture(o);
        }

        public BearDataEditorAsset(string path)
        {
            Path = path;
            Name = path.Split('/').Last().Split('.').First();
        }

        public Object GetObject()
        {
            if (Object == null) {
                Object = AssetDatabase.LoadAssetAtPath<Object>(Path);
            }

            return Object;
        }

        public Texture2D GetPreview()
        {
            if (PreviewTexture != null) {
                return PreviewTexture;
            }

            var asset = GetObject();
            if (asset == null) {
                return AssetDatabase.GetCachedIcon(Path) as Texture2D;
            }

            var generatedPreview = AssetPreview.GetAssetPreview(asset);
            if (generatedPreview != null) {
                PreviewTexture = generatedPreview;
                return PreviewTexture;
            }

            if (AssetPreview.IsLoadingAssetPreview(asset.GetInstanceID())) {
                return AssetPreview.GetMiniThumbnail(asset);
            }

            PreviewTexture = AssetPreview.GetMiniThumbnail(asset);
            return PreviewTexture;
        }

        private Texture2D GetPreviewTexture(Object asset)
        {
            Texture2D foundTexture = AssetPreview.GetAssetPreview(asset);

            if (foundTexture == null) {
                return null;
            } else {
                var result = new Texture2D(foundTexture.width, foundTexture.height);
                EditorUtility.CopySerialized(foundTexture, result);
                return result;
            }
        }

    }
}
