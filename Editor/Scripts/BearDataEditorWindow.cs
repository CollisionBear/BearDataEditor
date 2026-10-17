using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CollisionBear.BearDataEditor {
    [InitializeOnLoad]
    public class BearDataEditorWindow : EditorWindow {
        private const string OnlineSourceUrl = "https://github.com/CollisionBear/beardataeditor";
        private const string EditorName = "Bear Data Editor";
        private const string Version = "1.0.10";
        private const string CollisionBearUrl = "https://assetstore.unity.com/publishers/82099";

        private const string Hotkey = "#b";
        private const string WindowBasePath = "Window/Bear Data Editor";

        private const int ListViewWidth = 150;
        private const int FooterHeight = 30;

        private const int ListViewItemHeight = 18;
        private const int IconSize = 33;

        private static readonly Vector2 MinWindowSize = new Vector2(400, 200);

        private static GUIContent ShowInProjectContent;
        private static Texture2D LogoTexture;
        private static GUIContent ObjectCategoryContent;

        private static BearDataEditorType[] AvailableEditorTypes;
        private static GUIContent[] DisplayNames;

        private static List<List<BearDataEditorPreviewButton>> IconGroups;
        private static Dictionary<KeyCode, BearDataEditorType> HotKeyMappings;

        private static GUIStyle IconButton;

        static BearDataEditorWindow() {
            var typeAttributes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => new TypeWithAttribute { Type = t, Attribute = ObjectEditorAttribute(t) })
                .Where(t => t.Attribute != null)
                .ToList();

            AvailableEditorTypes = GetAvailableEditorType(typeAttributes);
            DisplayNames = AvailableEditorTypes.Select(t => t.DisplayName).ToArray();
            IconGroups = GetIconGroups(typeAttributes);
            HotKeyMappings = GetHotkeyMappings(typeAttributes);
        }

        private class TypeWithAttribute {
            public System.Type Type;
            public BearDataEditorAttribute Attribute;
        }

        private static BearDataEditorType[] GetAvailableEditorType(List<TypeWithAttribute> typeAttributes) {
            return typeAttributes.Select(t => new BearDataEditorType {
                Type = t.Type,
                Index = typeAttributes.IndexOf(t),
                FullClassName = t.Type.FullName,
                DisplayName = new GUIContent(GetTypeName(t))
            }).ToArray();
        }

        private static List<List<BearDataEditorPreviewButton>> GetIconGroups(List<TypeWithAttribute> typeAttributes) {
            var result = new List<List<BearDataEditorPreviewButton>>();

            if (!typeAttributes.Any()) {
                return result;
            }

            var groupsRequired = typeAttributes
                .Select(t => t.Attribute.IconGroupIndex)
                .DefaultIfEmpty(-1)
                .Max() + 1;

            // Ensure positive values
            groupsRequired = Mathf.Max(groupsRequired, 0);

            for (int i = 0; i < groupsRequired; i++) {
                result.Add(new List<BearDataEditorPreviewButton>());
            }

            foreach (var type in typeAttributes.Where(t => t.Attribute.UseIcon)) {
                var editorType = AvailableEditorTypes.First(t => t.Type == type.Type);
                result[type.Attribute.IconGroupIndex].Add(
                    new BearDataEditorPreviewButton(
                    type.Type,
                    editorType,
                    type.Attribute)
                );
            }

            return result;
        }

        private static Dictionary<KeyCode, BearDataEditorType> GetHotkeyMappings(List<TypeWithAttribute> typeAttributes) {
            var result = new Dictionary<KeyCode, BearDataEditorType>();

            foreach (var type in typeAttributes.Where(t => t.Attribute.UseIcon)) {
                var editorType = AvailableEditorTypes.First(t => t.Type == type.Type);

                if (type.Attribute.HotKey != KeyCode.None) {
                    if (result.ContainsKey(type.Attribute.HotKey)) {
                        Debug.LogWarning($"[{EditorName}] - Multiple types uses the same Hot key {type.Attribute.HotKey}");
                        continue;
                    }

                    result.Add(type.Attribute.HotKey, editorType);
                }
            }

            return result;
        }

        private static BearDataEditorAttribute ObjectEditorAttribute(System.Type type) {
            return type.GetCustomAttributes(typeof(BearDataEditorAttribute), false).FirstOrDefault() as BearDataEditorAttribute;
        }

        private static string GetTypeName(TypeWithAttribute typeAndAttribute) {
            if (typeAndAttribute.Attribute.DisplayName == string.Empty) {
                return AddSpacesToSentence(typeAndAttribute.Type.Name);
            } else {
                return typeAndAttribute.Attribute.DisplayName;
            }
        }

        private static string AddSpacesToSentence(string text) {
            System.Text.StringBuilder newText = new System.Text.StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++) {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        private static Texture2D CreateTexture(int width, int height, Color32 color) {
            Texture2D result = new Texture2D(width, height);
            result.SetPixels32(Enumerable.Repeat(color, width * height).ToArray(), 0);
            result.Apply();

            return result;
        }


        [MenuItem(WindowBasePath + " " + Hotkey)]
        public static void ShowWindow() {
            var window = EditorWindow.CreateWindow<BearDataEditorWindow>();
            window.minSize = MinWindowSize;
            window.Show();
        }

        [SerializeField]
        private string SelectedTypeName;        // Used to lookup the selected type, even after a recompile.

        private BearDataEditorType SelectedType;

        [SerializeField]

        private int SelectedObjectIndex;

        [SerializeField]
        private string SelectedAssetPath = string.Empty;

        [SerializeField]
        private string FilterString = string.Empty;

        [SerializeField]
        private List<BearDataEditorAsset> FoundObjects = new List<BearDataEditorAsset>();

        private BearDataEditorAsset SelectedObject = new BearDataEditorAsset();

        private List<Editor> SelectedObjectEditors = new List<Editor>();

        private List<BearDataEditorAsset> FilteredObjects = new List<BearDataEditorAsset>();

        private Dictionary<BearDataEditorType, List<BearDataEditorAsset>> CachedObjects = new Dictionary<BearDataEditorType, List<BearDataEditorAsset>>();

        private Dictionary<BearDataEditorType, int> CachedSelectedIndex = new Dictionary<BearDataEditorType, int>();

        private SearchField ObjectSearchField;
        private GUIStyle SelectedStyle;
        private GUIStyle UnselectedStyle;

        private float ScrollViewHeight = 0;

        [SerializeField]
        private Vector2 ListScrollViewOffset;

        [SerializeField]
        private Vector2 InspectorScrollViewOffset;

        private void SetupStyles() {
            SelectedStyle = new GUIStyle(GUI.skin.label);
            SelectedStyle.normal.textColor = Color.white;
            SelectedStyle.normal.background = CreateTexture(200, ListViewItemHeight, new Color(0.24f, 0.48f, 0.9f));
            SelectedStyle.clipping = TextClipping.Clip;
            SelectedStyle.wordWrap = false;

            UnselectedStyle = new GUIStyle(GUI.skin.label);
            UnselectedStyle.clipping = TextClipping.Clip;
            UnselectedStyle.wordWrap = false;
            IconButton = new GUIStyle(GUI.skin.button) {
                padding = new RectOffset(1, 1, 1, 1)
            };

        }

        private void OnDisable() {
            ClearAllEditors();
        }

        public void OnEnable() {
            foreach (var group in IconGroups) {
                foreach (var item in group) {
                    if (item.Icon == null && item.IconPath != string.Empty) {
                        item.LoadIcon();
                    }
                }
            }

            LogoTexture = Resources.Load<Texture2D>("CollsionsBearLogo");
            ShowInProjectContent = new GUIContent(Resources.Load<Texture>("ShowInProjectIcon"), "Open in project view");
            ObjectCategoryContent = new GUIContent("Object category");

            ObjectSearchField = new SearchField();
            if (CanRestoreEditorState()) {
                RestoreEditorState();
            } else {
                ChangeSelectedType(AvailableEditorTypes.FirstOrDefault());
            }
        }

        private bool CanRestoreEditorState() {
            if (AvailableEditorTypes == null || AvailableEditorTypes.Length == 0 || string.IsNullOrEmpty(SelectedTypeName)) {
                return false;
            }

            return AvailableEditorTypes.Any(editorType => editorType.FullClassName == SelectedTypeName);
        }

        private void RestoreEditorState() {
            var restoredType = AvailableEditorTypes.First(editorType => editorType.FullClassName == SelectedTypeName);
            SelectedType = restoredType;
            titleContent = new GUIContent(restoredType.DisplayName.text.Substring(0, Mathf.Min(restoredType.DisplayName.text.Length, 10)), Resources.Load<Texture>("DataEditorIcon"));

            if (FoundObjects == null || FoundObjects.Count == 0) {
                UpdateFoundObjects(restoredType, force: true);
            } else {
                ReconnectAssetReferences(FoundObjects);
                CacheFoundObjects(restoredType);
            }

            if (string.IsNullOrEmpty(FilterString)) {
                FilterString = string.Empty;
            }

            UpdateFilter(FilterString);
            ClearAllEditors();
            RestoreSelectedObject();
        }

        private void CacheFoundObjects(BearDataEditorType editorType) {
            if (CachedObjects.ContainsKey(editorType)) {
                CachedObjects[editorType] = FoundObjects;
            } else {
                CachedObjects.Add(editorType, FoundObjects);
            }
        }

        private void ReconnectAssetReferences(List<BearDataEditorAsset> assets) {
            for (var index = assets.Count - 1; index >= 0; index--) {
                var asset = assets[index];
                if (asset == null) {
                    assets.RemoveAt(index);
                    continue;
                }

                if (string.IsNullOrEmpty(asset.Path) && asset.Object != null) {
                    asset.Path = AssetDatabase.GetAssetPath(asset.Object);
                }

                if (asset.Object == null) {
                    asset.GetObject();
                }

                if (asset.Object == null) {
                    assets.RemoveAt(index);
                }
            }
        }

        private void RestoreSelectedObject() {
            if (FilteredObjects == null || FilteredObjects.Count == 0) {
                SelectedObject = null;
                SelectedObjectIndex = -1;
                return;
            }

            var selectedAsset = FindRestoredAsset();
            if (selectedAsset == null) {
                UpdateSelectedObjectIndex(0);
                return;
            }

            ChangeSelectedObject(selectedAsset, keepScrollPosition: true);
        }

        private BearDataEditorAsset FindRestoredAsset() {
            if (!string.IsNullOrEmpty(SelectedAssetPath)) {
                var matchedByPath = FilteredObjects.FirstOrDefault(asset => asset.Path == SelectedAssetPath);
                if (matchedByPath != null) {
                    return matchedByPath;
                }
            }

            if (SelectedObjectIndex >= 0 && SelectedObjectIndex < FilteredObjects.Count) {
                return FilteredObjects[SelectedObjectIndex];
            }

            return null;
        }

        private void ClearAllEditors() {
            if (SelectedObjectEditors == null) {
                SelectedObjectEditors = new List<Editor>();
                return;
            }

            foreach (var editor in SelectedObjectEditors) {
                if (editor != null) {
                    DestroyImmediate(editor);
                }
            }

            SelectedObjectEditors.Clear();
        }


        public void OnGUI() {
            SetupStyles();
            EditorGUILayout.Space();

            if (AvailableEditorTypes.Length == 0) {
                EditorGUILayout.HelpBox($"No types to display.\n\nStart using [{EditorName}] attribute on classes inheriting from ScriptableObject or MonoBehaviour to expose them in the editor.\nSee the classes in the Examples folder for real uses.\n\nFor more info see {OnlineSourceUrl}", MessageType.Info);
                return;
            }

            HandleKeyboardInput();
            DisplayTypeSelection();
            DisplayObjectSelection();

            if (AssetPreview.IsLoadingAssetPreviews()) {
                Repaint();
            }
        }

        private void DisplayTypeSelection() {
            if (SelectedType == null) {
                return;
            }

            using (new EditorGUILayout.HorizontalScope()) {
                var selectedTypeIndex = EditorGUILayout.Popup(ObjectCategoryContent, SelectedType.Index, DisplayNames);
                var tmpSelectedType = AvailableEditorTypes[selectedTypeIndex];
                if (tmpSelectedType != SelectedType) {
                    ChangeSelectedType(AvailableEditorTypes[selectedTypeIndex]);
                }

                if (GUILayout.Button("Open new editor", GUILayout.Width(128))) {
                    ShowWindow();
                }
            }

            ShowIcons();
        }

        private void ShowIcons() {
            if (!IconGroups.Any(g => g.Count > 0)) {
                return;
            }

            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.Label(GUIContent.none, EditorStyles.label, GUILayout.Width(6), GUILayout.Height(IconSize));
                foreach (var group in IconGroups) {
                    foreach (var item in group) {
                        SetGuiColorState(item.EditorType == SelectedType);
                        if (GUILayout.Button(item.GetContent(), IconButton, GUILayout.Width(IconSize), GUILayout.Height(IconSize))) {
                            ChangeSelectedType(AvailableEditorTypes[item.EditorType.Index]);
                        }
                    }

                    GUILayout.Label(GUIContent.none, EditorStyles.label, GUILayout.Width(12), GUILayout.Height(IconSize));
                }

                SetGuiColorState(false);
            }
        }

        private void SetGuiColorState(bool state) {
            if (state) {
                GUI.color = Color.green;
            } else {
                GUI.color = Color.white;
            }
        }
        private void HandleKeyboardInput() {
            if (EditorGUIUtility.editingTextField) {
                return;
            }

            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.DownArrow) {
                    UpdateSelectedObjectIndex(SelectedObjectIndex + 1);
                    Event.current.Use();
                } else if (Event.current.keyCode == KeyCode.UpArrow) {
                    UpdateSelectedObjectIndex(SelectedObjectIndex + -1);
                    Event.current.Use();
                } else if (HotKeyMappings.ContainsKey(Event.current.keyCode)) {
                    ChangeSelectedType(HotKeyMappings[Event.current.keyCode]);
                }
            }
        }

        private void DisplayObjectSelection() {
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(ListViewWidth))) {
                    DisplayObjectList();
                }

                if (SelectedObject != null) {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                        DisplaySelectedObject();
                    }
                }
            }
        }

        private void UpdateSelectedObjectIndex(int newIndex) {
            if (FilteredObjects.Count == 0) {
                return;
            }

            newIndex = Mathf.Clamp(newIndex, 0, FilteredObjects.Count - 1);
            SelectedObjectIndex = newIndex;
            ChangeSelectedObject(FilteredObjects[newIndex]);
        }

        private void DisplayObjectList() {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Found " + FilteredObjects.Count());
                if (GUILayout.Button("Refresh", GUILayout.Width(64))) {
                    RefreshObjects();
                }
            }

            DisplaySearchField();
            DisplayScrollListView();
            DisplayFooter();
        }

        public void RefreshObjects() {
            if (SelectedType != null && FilterString != null) {
                UpdateFoundObjects(SelectedType, force: true);
                UpdateFilter(FilterString);
            }
        }

        private void DisplaySearchField() {
            var searchRect = GUILayoutUtility.GetRect(100, 32);
            var tmpFilterString = ObjectSearchField.OnGUI(searchRect, FilterString);

            if (tmpFilterString != FilterString) {
                UpdateFilter(tmpFilterString);
                FilterString = tmpFilterString;
            }
        }

        private void UpdateFilter(string filterString) {
            FilteredObjects = FilterObjects(FoundObjects, filterString);
        }

        private void DisplayScrollListView() {
            if (FoundObjects == null) {
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(ListScrollViewOffset, GUIStyle.none, GUI.skin.verticalScrollbar)) {
                    ListScrollViewOffset = scrollScope.scrollPosition;
                    foreach (var foundObject in new List<BearDataEditorAsset>(FilteredObjects)) {
                        if (foundObject == null) {
                            FilteredObjects.Remove(foundObject);
                        } else {
                            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(ListViewItemHeight))) {
                                GUI.DrawTexture(GUILayoutUtility.GetRect(16, ListViewItemHeight, GUILayout.Width(16), GUILayout.Height(ListViewItemHeight)), foundObject.GetPreview());

                                var nameStyle = GetGUIStyle(foundObject);
                                var nameRect = GUILayoutUtility.GetRect(0, ListViewItemHeight, GUILayout.ExpandWidth(true), GUILayout.Height(ListViewItemHeight));
                                if (GUI.Button(nameRect, new GUIContent(foundObject.Name, foundObject.Name), nameStyle)) {
                                    ChangeSelectedObject(foundObject);
                                }

                                if (GUILayout.Button(ShowInProjectContent, EditorStyles.label, GUILayout.Width(18), GUILayout.Height(ListViewItemHeight))) {
                                    var assetObject = foundObject.GetObject();
                                    ProjectWindowUtil.ShowCreatedAsset(assetObject);
                                    EditorGUIUtility.PingObject(assetObject);
                                }
                            }
                        }
                    }
                }
            }

            if (Event.current.type == EventType.Repaint) {
                var lastRect = GUILayoutUtility.GetLastRect();
                ScrollViewHeight = lastRect.height;
            }
        }

        private void DisplayFooter() {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(FooterHeight))) {
                if (GUILayout.Button(LogoTexture, EditorStyles.label)) {
                    Application.OpenURL(CollisionBearUrl);
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Height(FooterHeight))) {
                    EditorGUILayout.LabelField(EditorName);
                    EditorGUILayout.LabelField($"Version {Version}");
                }
            }
        }

        private GUIStyle GetGUIStyle(BearDataEditorAsset o) {
            if (SelectedObjectIndex == o.FilteredIndex) {
                return SelectedStyle;
            } else {
                return UnselectedStyle;
            }
        }

        private void DisplaySelectedObject() {
            if (SelectedObject == null || SelectedObject.Object == null) {
                return;
            }

            if (SelectedObjectEditors == null) {
                return;
            }


            using (var scrollScope = new EditorGUILayout.ScrollViewScope(InspectorScrollViewOffset)) {
                using (new EditorGUILayout.VerticalScope()) {
                    InspectorScrollViewOffset = scrollScope.scrollPosition;

                    DrawObjectHeader(SelectedObject.Object);
                    var inspectorColumnWidth = GUILayoutUtility.GetRect(0f, 0f, GUIStyle.none, GUILayout.ExpandWidth(true)).width;

                    var anyChanges = false;
                    var changedField = new List<string>();
                    foreach (var selectedEditor in SelectedObjectEditors) {
                        using (var changeDetection = new EditorGUI.ChangeCheckScope()) {
                            if (selectedEditor == null) {
                                continue;
                            }

                            using (new EditorGUILayout.HorizontalScope()) {
                                DrawComponentPreview(selectedEditor.target);
                                EditorGUILayout.LabelField(selectedEditor.target.GetType().Name, EditorStyles.boldLabel);
                            }

                            DrawComponentInspector(selectedEditor, inspectorColumnWidth);

                            DrawUILine(Color.gray);
                            EditorGUILayout.Space();

                            if (changeDetection.changed) {
                                anyChanges = true;
                                changedField.Add(selectedEditor.target.GetType().Name);
                                EditorUtility.SetDirty(selectedEditor.target);
                            }
                        }
                    }

                    if (anyChanges) {
                        EditorUtility.SetDirty(SelectedObject.Object);
                    }
                }
            }
        }

        private void DrawComponentInspector(Editor selectedEditor, float inspectorColumnWidth) {
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            var previousFieldWidth = EditorGUIUtility.fieldWidth;
            var previousWideMode = EditorGUIUtility.wideMode;
            var previousHierarchyMode = EditorGUIUtility.hierarchyMode;

            if (selectedEditor.target is Transform) {
                // Using DrawDefaultInspector for the transformcomponent draws it squeezed into a weird way.
                // This is a workaround to get it to draw correctly
                DrawBuiltInTransformInspector(selectedEditor, inspectorColumnWidth);
            } else if (selectedEditor.target is MonoBehaviour || selectedEditor.target is ScriptableObject) {
                EditorGUIUtility.labelWidth = 200;
                EditorGUIUtility.fieldWidth = 0;
                selectedEditor.OnInspectorGUI();
            } else {
                EditorGUIUtility.labelWidth = 200;
                EditorGUIUtility.fieldWidth = 0;
                selectedEditor.DrawDefaultInspector();
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUIUtility.fieldWidth = previousFieldWidth;
            EditorGUIUtility.wideMode = previousWideMode;
            EditorGUIUtility.hierarchyMode = previousHierarchyMode;
        }

        private static void DrawBuiltInTransformInspector(Editor transformEditor, float inspectorColumnWidth) {
            if (inspectorColumnWidth < 1f) {
                inspectorColumnWidth = 330f;
            }

            EditorGUIUtility.hierarchyMode = true;
            EditorGUIUtility.wideMode = true;
            EditorGUIUtility.fieldWidth = 0f;
            EditorGUIUtility.labelWidth = InspectorLabelWidth(inspectorColumnWidth);
            transformEditor.OnInspectorGUI();
        }

        private static float InspectorLabelWidth(float inspectorColumnWidth) {
            if (inspectorColumnWidth > 330f) {
                return Mathf.Max(inspectorColumnWidth * 0.45f - 40f, 120f);
            }

            return Mathf.Max(60f, inspectorColumnWidth - 212f);
        }

        public void DrawUILine(Color color, int thickness = 1, int padding = 0) {
            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            lineRect.height = thickness;
            lineRect.y += padding / 2;
            lineRect.x -= 20;
            lineRect.width += 20;
            EditorGUI.DrawRect(lineRect, color);
        }

        public void DrawComponentPreview(Object unityObject) {
            var drawRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            var previewTexture = AssetPreview.GetMiniThumbnail(unityObject);

            if (previewTexture != null) {
                GUI.DrawTexture(drawRect, previewTexture);
            }
        }

        public void ChangeSelectedType(BearDataEditorType editorType) {
            if ((SelectedType?.FullClassName ?? string.Empty) == (editorType?.FullClassName ?? string.Empty)) {
                return;
            }

            if (editorType == null) {
                titleContent = new GUIContent("Data Editor", Resources.Load<Texture>("DataEditorIcon"));
                return;
            }

            SelectedType = editorType;
            SelectedTypeName = editorType.FullClassName;

            UpdateFoundObjects(editorType);
            titleContent = new GUIContent(editorType.DisplayName.text.Substring(0, Mathf.Min(editorType.DisplayName.text.Length, 10)), Resources.Load<Texture>("DataEditorIcon"));
            FilteredObjects = FoundObjects;
            FilterString = string.Empty;
            ClearAllEditors();

            if (CachedSelectedIndex.ContainsKey(SelectedType)) {
                ChangeSelectedObject(FilteredObjects[CachedSelectedIndex[SelectedType]]);
            } else {
                SelectedObject = null;
                SelectedObjectIndex = -1;
            }

            Repaint();
        }

        public void UpdateFoundObjects(BearDataEditorType editorType, bool force = false) {
            if (CachedObjects.ContainsKey(editorType) && !force) {
                FoundObjects = CachedObjects[editorType];
            } else {
                FoundObjects = FindAssetsOfType(editorType.Type).ToList();

                if (CachedObjects.ContainsKey(editorType)) {
                    CachedObjects[editorType] = FoundObjects;
                } else {
                    CachedObjects.Add(editorType, FoundObjects);
                }
            }
        }

        public List<BearDataEditorAsset> FindAssetsOfType(System.Type type) {
            var result = new List<BearDataEditorAsset>();
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                result = FindScriptableObjectOfType(type).OrderBy(a => a.Name).ToList();
            } else if (typeof(Component).IsAssignableFrom(type)) {
                result = FindPrefabsWithComponentType(type).OrderBy(a => a.Name).ToList();
            }

            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
                result[i].FilteredIndex = i;
            }

            return result;
        }

        public List<BearDataEditorAsset> FindScriptableObjectOfType(System.Type type) {
            var allValidAsset = AssetDatabase.FindAssets(string.Format("t:{0}", type.FullName));
            return allValidAsset
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => new BearDataEditorAsset(p))
                .ToList();
        }

        private List<BearDataEditorAsset> FindPrefabsWithComponentType(System.Type type) {
            var allGameObjectAssetPaths = AssetDatabase.FindAssets("t:GameObject")
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => p.EndsWith(".prefab"))
                .ToList();

            return allGameObjectAssetPaths
                .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
                .Where(a => HasComponent(a, type))
                .Select(a => new BearDataEditorAsset(a))
                .ToList();
        }

        private bool HasComponent(GameObject gameObject, System.Type type) {
            return gameObject.GetComponents<Component>()
                .Where(t => type.IsInstanceOfType(t))
                .Any();
        }


        public List<BearDataEditorAsset> FilterObjects(List<BearDataEditorAsset> startCollection, string filter) {
            if (filter == string.Empty) {
                foreach (var item in startCollection) {
                    item.FilteredIndex = item.Index;
                }
                return startCollection;
            }

            foreach (var item in startCollection) {
                item.FilteredIndex = -1;
            }

            var result = startCollection.Where(o => o.Name.ToLower().Contains(filter.ToLower())).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].FilteredIndex = i;
            }

            return result;
        }

        public void ChangeSelectedObject(BearDataEditorAsset selectedObject, bool keepScrollPosition = false) {
            if (selectedObject == null) {
                return;
            }

            if (selectedObject == SelectedObject) {
                return;
            }

            SelectedObjectIndex = FilteredObjects.IndexOf(selectedObject);
            SelectedAssetPath = GetPersistentAssetPath(selectedObject);
            if (!keepScrollPosition) {
                EnsureSelectItemInView(SelectedObjectIndex);
            }

            if (CachedSelectedIndex.ContainsKey(SelectedType)) {
                CachedSelectedIndex[SelectedType] = SelectedObjectIndex;
            } else {
                CachedSelectedIndex.Add(SelectedType, SelectedObjectIndex);
            }

#if UNITY_2018_3_OR_NEWER
            if (selectedObject.Object is GameObject) {
                SelectedObject = selectedObject;
                CreateEditors(selectedObject.GetObject());
            } else {
                SelectedObject = selectedObject;
                CreateEditors(SelectedObject.GetObject());
            }
#else
            SelectedObject = selectedObject;
            PrefabInstance = null;
            CreateEditors(SelectedObject);
#endif
            GUI.FocusControl(null);
        }

        private static string GetPersistentAssetPath(BearDataEditorAsset asset) {
            if (!string.IsNullOrEmpty(asset.Path)) {
                return asset.Path;
            }

            if (asset.Object == null) {
                return string.Empty;
            }

            asset.Path = AssetDatabase.GetAssetPath(asset.Object);
            return asset.Path;
        }

        private void EnsureSelectItemInView(int index) {
            var expectedScrollPosition = GetExpectedScrollPosition(index);

            var scrollPositionTop = ListScrollViewOffset.y;
            var scrollPositionBottom = scrollPositionTop + ScrollViewHeight;

            if (expectedScrollPosition < scrollPositionTop) {
                ListScrollViewOffset.y = expectedScrollPosition;
            } else if (expectedScrollPosition > scrollPositionBottom - ListViewItemHeight) {
                var adjustment = expectedScrollPosition - (scrollPositionBottom - ListViewItemHeight);
                var misalignedRows = Mathf.CeilToInt(adjustment / ListViewItemHeight);
                ListScrollViewOffset.y += misalignedRows * ListViewItemHeight;
            }
        }

        private float GetExpectedScrollPosition(int index) => index * ListViewItemHeight;

        private static void DrawObjectHeader(Object target) {
            if (target == null) {
                return;
            }

            var content = EditorGUIUtility.ObjectContent(target, target.GetType());
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(32))) {
                var iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
                if (content.image != null) {
                    GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit);
                }

                using (new EditorGUILayout.VerticalScope()) {
                    EditorGUILayout.LabelField(content.text, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(target.GetType().Name, EditorStyles.miniLabel);
                }
            }
        }

        public void CreateEditors(Object selectedObject) {
            ClearAllEditors();

            if (selectedObject == null) {
                SelectedObject = null;
                return;
            }

            if (selectedObject is GameObject gameObject) {
                var components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) {
                    if (components[i] == null) {
                        continue;
                    }

                    SelectedObjectEditors.Add(Editor.CreateEditor(components[i]));
                }
            } else {
                SelectedObjectEditors.Add(Editor.CreateEditor(selectedObject));
            }
        }
    }
}
