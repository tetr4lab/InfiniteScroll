using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScroll {

    /// <summary>無限スクロールレクト</summary>
    [AddComponentMenu ("UI/Infinite Scroll Rect", 38)]
    [DisallowMultipleComponent]
    [RequireComponent (typeof (RectTransform))]
    public class InfiniteScrollRect : ScrollRect {

        /// <summary>レイアウトを確定するまでの最大ビルド回数</summary>
        protected const int MaxNumberOfLayoutRebuilds = 10;

        /// <summary>項目の外縁</summary>
        [SerializeField]
        public RectOffset m_padding = new RectOffset ();

        /// <summary>項目の隙間</summary>
        [SerializeField]
        public float m_spacing = 0;

        /// <summary>項目の配向</summary>
        [SerializeField]
        public TextAnchor m_childAlignment = TextAnchor.UpperLeft;

        /// <summary>項目の逆順</summary>
        [SerializeField]
        public bool m_reverseArrangement = false;

        /// <summary>項目の拡縮</summary>
        [SerializeField]
        public bool m_controlChildSize = false;

        /// <summary>論理項目</summary>
        public List<InfiniteScrollItemBase> Items { get; protected set; }

        /// <summary>実体項目</summary>
        protected List<InfiniteScrollItemComponentBase> _components { get; set; }

        /// <summary>先頭の実体項目の論理インデックス</summary>
        protected int _topIndex { get; set; }

        /// <summary>最後の実体項目の論理インデックス</summary>
        protected int _lastIndex { get; set; }

        /// <summary>スクロール方向のコンテントサイズ</summary>
        protected float _contentSize {
            get => vertical ? content.rect.height : content.rect.width;
            set {
                var size = content.sizeDelta;
                if (vertical) {
                    size.y = value;
                } else {
                    size.x = value;
                }
                content.sizeDelta = size;
            }
        }

        /// <summary>初期化</summary>
        public void Initialize (IEnumerable<InfiniteScrollItemBase> items) {
            Items = new List<InfiniteScrollItemBase> (items);
            // 消去
            foreach (RectTransform t in content) {
                Destroy (t.gameObject);
            }
            // 生成
            float pos = vertical ? m_padding.top : m_padding.left;
            _components = new List<InfiniteScrollItemComponentBase> ();
            LayoutRebuilder.ForceRebuildLayoutImmediate (transform as RectTransform);
            _lastIndex = Items.Count - 1;
            var viewportSize = vertical? viewport.rect.height : viewport.rect.width;
            for (var i = _topIndex; i <= _lastIndex; i++) {
                var component = Items [i].Create (content, this);
                Items [i].Size = component.SetSize (m_padding, m_childAlignment, m_controlChildSize, vertical, viewport.rect.size);
                pos += component.Size + m_spacing;
                _components.Add (component);
                if (pos > viewportSize && _lastIndex > i) {
                    // いっぱいの次まで
                    _lastIndex = i + 1;
                }
            }
            pos += vertical ? m_padding.bottom : m_padding.right;
            // 配置
            if (vertical) {
                var y = m_reverseArrangement ? 0f : 1f;
                content.anchorMin = new Vector2 (0f, y);
                content.anchorMax = new Vector2 (1f, y);
                content.pivot = new Vector2 (0.5f, y);
                content.offsetMin = new Vector2 (0f, 0f);
                content.offsetMax = new Vector2 (0f, pos);
                verticalScrollbar.value = m_reverseArrangement ? 0f : 1f;
            } else {
                var x = m_reverseArrangement ? 1f : 0f;
                content.anchorMin = new Vector2 (x, 0f);
                content.anchorMax = new Vector2 (x, 1f);
                content.pivot = new Vector2 (x, 0.5f);
                content.offsetMin = new Vector2 (0f, 0f);
                content.offsetMax = new Vector2 (pos, 0f);
                horizontalScrollbar.value = m_reverseArrangement ? 1f : 0f;
            }
            BuildItemLayout ();
        }

        /// <summary>項目の配置</summary>
        protected void BuildItemLayout () {
            var dir = vertical ? -1f : 1f;
            var pos = (vertical ? m_padding.top : m_padding.left) * dir;
            var maxIndex = _lastIndex - _topIndex;
            for (var i = 0; i <= maxIndex; i++) {
                var component = _components [m_reverseArrangement ? maxIndex - i : i];
                component.SetPosition (m_padding, m_spacing, m_childAlignment, vertical, pos);
                pos += (component.Size + m_spacing) * dir;
            }
        }

        /// <summary>破棄</summary>
        protected override void OnDestroy () {
            if (_components != null) {
                foreach (var item in _components) {
                    Destroy (item.gameObject);
                }
            }
        }

    }

    /// <summary>
    /// 論理項目(抽象クラス)
    ///   継承したクラスを用意して、スクロールレクトの初期化に使用する
    ///   論理項目のリストの一部が物理項目に反映される
    /// </summary>
    public abstract class InfiniteScrollItemBase {

        /// <summary>
        /// 実体を生成
        ///   GameObjectを生成して、InfiniteScrollItemComponentを継承したコンポーネントをアタッチする
        /// </summary>
        /// <returns>生成したGameObjectにアタッチされているコンポーネントを返す</returns>
        public abstract InfiniteScrollItemComponentBase Create (Transform parent, InfiniteScrollRect scroll);

        public abstract Vector2 Size { get; protected internal set; }
    
    }

    /// <summary>
    /// 実体項目(抽象クラス)
    ///   このクラスを継承したクラスを、Contentに配置するGameObjectにアタッチする
    ///   論理項目と物理項目(これ)を同期する役割を担う
    /// </summary>
    public abstract class InfiniteScrollItemComponentBase : MonoBehaviour {

        /// <summary>レイアウトを確定するまでの最大ビルド回数</summary>
        protected const int MaxNumberOfLayoutRebuilds = 10;

        /// <summary>親のスクロールレクト</summary>
        public virtual InfiniteScrollRect ScrollRect { get; protected internal set; }

        /// <summary>レクトトランスフォーム</summary>
        public virtual RectTransform RectTransform => _rectTransform ?? transform as RectTransform ?? gameObject.AddComponent<RectTransform> ();
        protected RectTransform _rectTransform;

        /// <summary>サイズ</summary>
        public virtual float Size => ScrollRect.vertical ? RectTransform.rect.height : RectTransform.rect.width;

        /// <summary>リンク中の論理項目</summary>
        public virtual InfiniteScrollItemBase Item { get; protected internal set; }

        /// <summary>項目のサイズ決め</summary>
        protected internal virtual Vector2 SetSize (RectOffset padding, TextAnchor alignment, bool controlSize, bool vertical, Vector2 viewportSize) {
            var controledSize = vertical ? viewportSize.x - padding.left - padding.right : viewportSize.y - padding.top - padding.bottom;
            RectTransform.sizeDelta = new Vector2 (
                (!vertical || !controlSize) ? RectTransform.sizeDelta.x : controledSize,
                (vertical && controlSize) ? controledSize : RectTransform.sizeDelta.y
            );
            for (var i = 0; i < MaxNumberOfLayoutRebuilds; i++) {
                var lastSize = Size;
                LayoutRebuilder.ForceRebuildLayoutImmediate (RectTransform);
                if (Mathf.Approximately (lastSize, Size)) {
                    Debug.Log ($"{i}: alignment={alignment}, controlSize={controlSize}, controledSize={controledSize}, viewportSize={viewportSize}, localPosition={RectTransform.localPosition}, sizeDelta{RectTransform.sizeDelta}, Size={Size}");
                    break;
                }
            }
            return RectTransform.sizeDelta;
        }

        /// <summary>項目の位置決め</summary>
        protected internal virtual Vector2 SetPosition (RectOffset padding, float spacing, TextAnchor alignment, bool vertical, float pos) {
            Vector2 anchor;
            Vector2 anchoredPosition;
            switch (alignment) {
                case TextAnchor.UpperLeft:
                    anchor.x = 0f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? padding.left : pos;
                    anchoredPosition.y = vertical ? pos : -padding.top;
                    break;
                case TextAnchor.UpperCenter:
                    anchor.x = 0.5f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? 0f : pos;
                    anchoredPosition.y = vertical ? pos : -padding.top;
                    break;
                case TextAnchor.UpperRight:
                    anchor.x = 1f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? -padding.right : pos;
                    anchoredPosition.y = vertical ? pos : -padding.top;
                    break;
                case TextAnchor.MiddleLeft:
                    anchor.x = 0f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? padding.left : pos;
                    anchoredPosition.y = vertical ? pos : 0f;
                    break;
                case TextAnchor.MiddleCenter:
                    anchor.x = 0.5f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? 0f : pos;
                    anchoredPosition.y = vertical ? pos : 0f;
                    break;
                case TextAnchor.MiddleRight:
                    anchor.x = 1f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? padding.right : pos;
                    anchoredPosition.y = vertical ? pos : 0f;
                    break;
                case TextAnchor.LowerLeft:
                    anchor.x = 0f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? padding.left : pos;
                    anchoredPosition.y = vertical ? pos : padding.bottom;
                    break;
                case TextAnchor.LowerCenter:
                    anchor.x = 0.5f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? 0f : pos;
                    anchoredPosition.y = vertical ? pos : padding.bottom;
                    break;
                case TextAnchor.LowerRight:
                    anchor.x = 1f;
                    anchor.y = 1f;
                    anchoredPosition.x = vertical ? padding.right : pos;
                    anchoredPosition.y = vertical ? pos : padding.bottom;
                    break;
                default:
                    anchor.x = 0f;
                    anchor.y = 1f;
                    anchoredPosition.x = 0f;
                    anchoredPosition.y = 0f;
                    break;
            }
            RectTransform.anchorMin = anchor;
            RectTransform.anchorMax = anchor;
            RectTransform.pivot = anchor;
            RectTransform.anchoredPosition = anchoredPosition;
            Debug.Log ($"localPosition={RectTransform.localPosition}, pos={pos}");
            return RectTransform.localPosition;
        }

    }


}
