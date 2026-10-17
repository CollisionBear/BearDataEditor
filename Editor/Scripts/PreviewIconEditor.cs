using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor {
    [CustomEditor(typeof(ScriptableObject), true)]
    public class PreviewIconEditor : Editor {
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) {
            return PreviewIconUtility.Render(target, width, height);
        }
    }
}