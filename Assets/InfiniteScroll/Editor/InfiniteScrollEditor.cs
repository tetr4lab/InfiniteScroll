using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InfiniteScroll {

    /// <summary>無限スクロールレクトのカスタムエディタ</summary>
    [CanEditMultipleObjects, CustomEditor (typeof (InfiniteScrollRect), true)]
    public class InfiniteScrollEditor : ScrollRectEditor {

        /// <summary>オブジェクトの追加コンテキストメニュー</summary>
        [MenuItem ("GameObject/UI/InfiniteScroll View", false, 2026)]
        public static void AddInfiniteScrollView (MenuCommand menuCommand) {
            var parent = menuCommand?.context as GameObject;
            if (parent == null) {
                // 選択されていなければ既存Canvasを親に
                parent = FindObjectOfType<Canvas> ()?.gameObject;
            }
            if (parent == null || (parent.GetComponent<Canvas> () == null && parent.GetComponentInParent<Canvas> () == null)) {
                // 親がないか親にCanvasがないならキャンバスを生成
                var canvasObject = new GameObject ("Canvas");
                canvasObject.layer = LayerMask.NameToLayer ("UI");
                canvasObject.AddComponent<Canvas> ().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler> ();
                canvasObject.AddComponent<GraphicRaycaster> ();
                canvasObject.transform.SetParent ((parent ?? canvasObject).transform);
                parent = canvasObject;
            }
            var eventSystem = FindObjectOfType<EventSystem> ()?.gameObject;
            if (eventSystem == null) {
                // イベントシステムを生成
                eventSystem = new GameObject ("EventSystem");
                eventSystem.AddComponent<EventSystem> ();
                eventSystem.AddComponent<StandaloneInputModule> ();
                eventSystem.transform.SetParent (eventSystem.transform);
            }
            if (_prefab == null) {
                // プレファブをロード
                var path = Path.GetDirectoryName (Path.GetDirectoryName (ThisScriptPath ())); // スクリプトのひとつ上のフォルダから辿る
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject> (Path.Combine (path, "Prefabs/InfiniteScroll View.prefab"));
            }
            if (_prefab != null) {
                // プレファブから生成
                try {
                    AssetDatabase.StartAssetEditing ();
                    var root = Instantiate (_prefab, parent.transform);
                    root.transform.SetParent ((parent ?? root).transform); // 親がないなら自身を親に
                    root.name = "InfiniteScroll View";
                    Undo.RegisterCreatedObjectUndo (root, "Create InfiniteScrollRect");
                    Selection.activeGameObject = root;
                }
                catch (Exception e) {
                    Debug.LogError (e);
                }
                finally {
                    AssetDatabase.StopAssetEditing ();
                    AssetDatabase.Refresh ();
                }
            }
        }

        /// <summary>プレファブ</summary>
        private static GameObject _prefab;

        /// <summary>前回の垂直側トグル状態</summary>
        private bool _lastVertical;

        /// <summary>Paddingのトグル状態</summary>
        protected static bool PaddingFoldoutToggle;

        /// <summary>インスペクタ用</summary>
        public override void OnInspectorGUI () {
            base.OnInspectorGUI ();
            var component = (InfiniteScrollRect) target;
            Undo.RecordObject (component, "Edit InfiniteScrollRect");
            if (component.horizontal == component.vertical) {
                // トグルが変更されたら前回と逆にする (垂直と水平のトグルをラジオボタン化)
                component.horizontal = _lastVertical;
                component.vertical = !_lastVertical;
            }
            // 最後の状態を記録
            _lastVertical = component.vertical;
            // 追加項目
            PaddingFoldoutToggle = EditorGUILayout.BeginFoldoutHeaderGroup (PaddingFoldoutToggle, "Padding");
            if (PaddingFoldoutToggle) {
                EditorGUI.indentLevel++;
                component.m_padding.left = EditorGUILayout.IntField ("Left", component.m_padding.left);
                component.m_padding.right = EditorGUILayout.IntField ("Right", component.m_padding.right);
                component.m_padding.top = EditorGUILayout.IntField ("Top", component.m_padding.top);
                component.m_padding.bottom = EditorGUILayout.IntField ("Bottom", component.m_padding.bottom);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup ();
            component.m_spacing = EditorGUILayout.FloatField ("Spacing", component.m_spacing);
            component.m_childAlignment = (TextAnchor) EditorGUILayout.EnumPopup ("Child Alignment", component.m_childAlignment);
            component.m_reverseArrangement = EditorGUILayout.Toggle ("Reverse Arrangement", component.m_reverseArrangement);
            component.m_controlChildSize = EditorGUILayout.Toggle ("Control Child Size", component.m_controlChildSize);
            component.m_standardItemSize = EditorGUILayout.FloatField ("Standard Item Size", component.m_standardItemSize);
        }

        /// <summary>このスクリプトのパス</summary>
        public static string ThisScriptPath ([CallerFilePath] string path = "") => path.Substring (path.IndexOf ($"{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}") + 1);

    }

}
