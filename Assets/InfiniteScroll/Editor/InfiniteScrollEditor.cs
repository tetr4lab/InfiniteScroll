using System;
using System.Collections;
using System.Collections.Generic;
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
            // プレファブから生成
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject> ("Assets/InfiniteScroll/Prefabs/InfiniteScroll View.prefab");
            if (prefab != null) {
                var root = Instantiate (prefab, parent.transform);
                root.transform.SetParent ((parent ?? root).transform); // 親がないなら自身を親に
                root.name = "InfiniteScroll View";
                Selection.activeGameObject = root;
            }
        }

        /// <summary>前回の垂直側トグル状態</summary>
        private bool _lastVertical;

        /// <summary>Paddingのトグル状態</summary>
        protected static bool PaddingFoldoutToggle;

        /// <summary>インスペクタ用</summary>
        public override void OnInspectorGUI () {
            DrawDefaultInspector ();
            var component = (InfiniteScrollRect) target;
            if (component.horizontal == component.vertical) {
                // トグルが変更されたら前回と逆にする (垂直と水平のトグルをラジオボタン化)
                component.horizontal = _lastVertical;
                component.vertical = !_lastVertical;
            }
            // 最後の状態を記録
            _lastVertical = component.vertical;
        }

    }

}
