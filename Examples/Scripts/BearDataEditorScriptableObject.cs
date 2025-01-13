using UnityEngine;

namespace CollisionBear.BearDataEditor.Examples {
    [CreateAssetMenu(fileName = "new ScriptableObject", menuName = "Bear Data Editor/Examples/ScriptableObject")]
    [BearDataEditor(DisplayName = "Test Scriptable Object")]
    public class BearDataEditorScriptableObject : ScriptableObject {
        public int SomeIntData;
        public float SomeFloatData;
        public string SomeStringData;
    }
}