using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScroll {

    /// <summary>無限スクロールレクト</summary>
    [AddComponentMenu ("UI/Infinite Scroll Rect", 38)]
    [DisallowMultipleComponent]
    [RequireComponent (typeof (RectTransform))]
    public class InfiniteScrollRect : ScrollRect {

        /// <summary>項目の外縁</summary>
        [SerializeField]
        public RectOffset m_padding = new RectOffset ();

        /// <summary>項目の隙間</summary>
        [SerializeField]
        public float m_spacing = 0;

        /// <summary>項目の配向</summary>
        [SerializeField]
        public TextAnchor m_childAlignment = TextAnchor.LowerLeft;

        /// <summary>項目の逆順</summary>
        [SerializeField]
        public bool m_reverseArrangement = false;

        /// <summary>項目の拡縮</summary>
        [SerializeField]
        public bool m_controlChildSize = false;

        /// <summary>論理項目</summary>
        public virtual List<InfiniteScrollItemBase> Items { get; protected set; }

        /// <summary>実体項目</summary>
        protected virtual List<InfiniteScrollItemComponentBase> _components { get; set; }

        /// <summary>最初の実体項目の論理インデックス</summary>
        protected virtual int _firstIndex { get; set; }

        /// <summary>最後の実体項目の論理インデックス</summary>
        protected virtual int _lastIndex { get; set; }

        /// <summary>スクロール方向のコンテントサイズ</summary>
        public virtual float ContentSize {
            get => vertical ? content.offsetMax.y : content.offsetMax.x;
            protected set => content.offsetMax = vertical ? new Vector2 (0f, value) : new Vector2 (value, 0f);
        }

        /// <summary>スクロール方向のスクロールバー</summary>
        protected virtual Scrollbar _scrollbar => vertical ? verticalScrollbar : horizontalScrollbar;

        /// <summary>初期化</summary>
        public virtual void Initialize (IEnumerable<InfiniteScrollItemBase> items, int first = 0) {
            // 消去と整理
            foreach (RectTransform t in content) {
                Destroy (t.gameObject);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate (transform as RectTransform);
            // 生成 (firstの手前からいっぱいの次まで)
            Items = new List<InfiniteScrollItemBase> (items);
            _components = new List<InfiniteScrollItemComponentBase> ();
            var viewportSize = vertical? viewport.rect.height : viewport.rect.width;
            var averageSize = 0f;
            _firstIndex = (first > 0 && first < Items.Count) ? first : 0;
            _lastIndex = Items.Count - 1;
            for (var i = _firstIndex; i <= _lastIndex; i++) {
                var component = Items [i].Create (this);
                Items [i].Size = component.SetSize (m_padding, m_childAlignment, m_controlChildSize, vertical, viewport.rect.size);
                Items [i].Verified = true;
                averageSize += component.Size;
                _components.Add (component);
                if (averageSize > viewportSize && _lastIndex > i) {
                    // いっぱいの次まで
                    _lastIndex = i + 1;
                }
            }
            averageSize /= (_lastIndex - _firstIndex + 1);
            for (var i = 0; i < _firstIndex; i++) {
                Items [i].Size = averageSize;
            }
            for (var i = _lastIndex + 1; i < Items.Count; i++) {
                Items [i].Size = averageSize;
            }
            // 配置
            if (vertical) {
                var y = m_reverseArrangement ? 0f : 1f;
                content.anchorMin = new Vector2 (0f, y);
                content.anchorMax = new Vector2 (1f, y);
                content.pivot = new Vector2 (0.5f, y);
                content.offsetMin = new Vector2 (0f, 0f);
            } else {
                var x = m_reverseArrangement ? 1f : 0f;
                content.anchorMin = new Vector2 (x, 0f);
                content.anchorMax = new Vector2 (x, 1f);
                content.pivot = new Vector2 (x, 0.5f);
                content.offsetMin = new Vector2 (0f, 0f);
            }
            ContentSize = ItemPosition (Items.Count);
            var pos = ItemPosition (_firstIndex);
            var scroll = pos / (ContentSize - viewportSize);
            //Debug.Log ($"scroll({scroll}) = pos({pos}) / (ContentSize({ContentSize}) - viewportSize({viewportSize}))");
            _scrollbar.value = (vertical == m_reverseArrangement) ? scroll : 1f - scroll;
            var dir = (vertical == m_reverseArrangement) ? 1f : -1f;
            pos *= dir;
            var maxIndex = _lastIndex - _firstIndex;
            for (var i = 0; i <= maxIndex; i++) {
                var component = _components [i];
                component.SetPosition (m_padding, m_spacing, m_childAlignment, m_reverseArrangement, vertical, pos);
                pos += (component.Size + m_spacing) * dir;
            }
        }

        /// <summary>項目の位置を算出</summary>
        /// <param name="index">Items.Countを指定するとContentのサイズを返す</param>
        public virtual float ItemPosition (int index) {
            if (index <= 0 || index > Items.Count) { throw new System.ArgumentOutOfRangeException ("index"); }
            var pos = (m_reverseArrangement ? (vertical ? m_padding.bottom : m_padding.right) : (vertical ? m_padding.top : m_padding.left)) + m_spacing * index;
            for (var i = 0; i < index; i++) {
                pos += Items [i].Size;
            }
            if (index == Items.Count) {
                pos += m_reverseArrangement ? (vertical ? m_padding.top : m_padding.left) : (vertical ? m_padding.bottom : m_padding.right);
            }
            return pos;
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
        public virtual InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect) {
            var component = InfiniteScrollItemComponentBase.Create (scrollRect);
            component.Item = this;
            return component;
        }

        /// <summary>スクロール方向のサイズ</summary>
        public virtual float Size { get; protected internal set; }
    
        /// <summary>検証済み (偽ならサイズが実体と一致しない可能性)</summary>
        public virtual bool Verified { get; protected internal set; }

        /// <summary>書き換えがあった</summary>
        public virtual bool UpdateRequired {
            get => _updateRequired;
            protected internal set {
                _updateRequired = value;
                Verified = false;
            }
        }
        protected bool _updateRequired;
    }

    /// <summary>
    /// 実体項目(抽象クラス)
    ///   このクラスを継承したクラスを、Contentに配置するGameObjectにアタッチする
    ///   論理項目と物理項目(これ)を同期する役割を担う
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent (typeof (RectTransform))]
    public abstract class InfiniteScrollItemComponentBase : MonoBehaviour {

        #region static

        /// <summary>レイアウトを確定するまでの最大ビルド回数</summary>
        protected const int MaxNumberOfLayoutRebuilds = 10;

        /// <summary>コンポーネント付きオブジェクトの生成</summary>
        public static InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect) {
            var obj = new GameObject ("InfiniteScrollItem");
            obj.AddComponent<RectTransform> ();
            obj.transform.SetParent (scrollRect.content);
            var component = (obj.GetComponent<InfiniteScrollItemComponentBase> () ?? obj.AddComponent<InfiniteScrollItemComponentBase> ());
            component.ScrollRect = scrollRect;
            component.Initialize ();
            return component;
        }

        #endregion

        /// <summary>初期化</summary>
        protected virtual void Initialize () { }

        /// <summary>親のスクロールレクト</summary>
        public virtual InfiniteScrollRect ScrollRect { get; protected set; }

        /// <summary>レクトトランスフォーム</summary>
        public virtual RectTransform RectTransform => _rectTransform ?? transform as RectTransform ?? gameObject.AddComponent<RectTransform> ();
        protected RectTransform _rectTransform;

        /// <summary>サイズ</summary>
        public virtual float Size => ScrollRect.vertical ? RectTransform.rect.height : RectTransform.rect.width;

        /// <summary>リンク中の論理項目</summary>
        public virtual InfiniteScrollItemBase Item {
            get => _item;
            set {
                _item = value;
                Apply ();
            }
        }
        protected InfiniteScrollItemBase _item;

        /// <summary>論理項目の状態を反映</summary>
        protected virtual void Apply () {  }

        /// <summary>更新</summary>
        protected virtual void Update () {
            if (ScrollRect && Item?.UpdateRequired == true) {
                Apply ();
            }
            if (!Item.Verified) {
                if (Item.Size != Size) {
                    Item.Size = Size;
                } else {
                    Item.Verified = true;
                }
            }
        }

        /// <summary>項目のサイズ決め</summary>
        protected internal virtual float SetSize (RectOffset padding, TextAnchor alignment, bool controlSize, bool vertical, Vector2 viewportSize) {
            var controledSize = vertical ? viewportSize.x - padding.left - padding.right : viewportSize.y - padding.top - padding.bottom;
            RectTransform.sizeDelta = new Vector2 (
                (!vertical || !controlSize) ? RectTransform.sizeDelta.x : controledSize,
                (vertical && controlSize) ? controledSize : RectTransform.sizeDelta.y
            );
            for (var i = 0; i < MaxNumberOfLayoutRebuilds; i++) {
                var lastSize = Size;
                LayoutRebuilder.ForceRebuildLayoutImmediate (RectTransform);
                if (Mathf.Approximately (lastSize, Size)) {
                    //Debug.Log ($"{i}: alignment={alignment}, controlSize={controlSize}, controledSize={controledSize}, viewportSize={viewportSize}, localPosition={RectTransform.localPosition}, sizeDelta{RectTransform.sizeDelta}, Size={Size}");
                    break;
                }
            }
            return Size;
        }

        /// <summary>項目の位置決め</summary>
        protected internal virtual Vector2 SetPosition (RectOffset padding, float spacing, TextAnchor alignment, bool reverse, bool vertical, float pos) {
            Vector2 anchor;
            Vector2 anchoredPosition;
            if (reverse) {
                switch (alignment) {
                    case TextAnchor.UpperRight:
                        anchor.x = vertical ? 1f : 1f;
                        anchor.y = vertical ? 0f : 1f;
                        anchoredPosition.x = vertical ? -padding.right : pos;
                        anchoredPosition.y = vertical ? pos : -padding.top;
                        break;
                    case TextAnchor.MiddleCenter:
                        anchor.x = vertical ? 0.5f : 1f;
                        anchor.y = vertical ? 0f : 0.5f;
                        anchoredPosition.x = vertical ? 0f : pos;
                        anchoredPosition.y = vertical ? pos : 0f;
                        break;
                    case TextAnchor.LowerLeft:
                        anchor.x = vertical ? 0f : 1f;
                        anchor.y = vertical ? 0f : 0f;
                        anchoredPosition.x = vertical ? padding.left : pos;
                        anchoredPosition.y = vertical ? pos : padding.bottom;
                        break;
                    default:
                        anchor.x = 1f;
                        anchor.y = 0f;
                        anchoredPosition.x = 0f;
                        anchoredPosition.y = 0f;
                        break;
                }
            } else {
                switch (alignment) {
                    case TextAnchor.UpperRight:
                        anchor.x = vertical ? 1f : 0f;
                        anchor.y = vertical ? 1f : 1f;
                        anchoredPosition.x = vertical ? -padding.right : pos;
                        anchoredPosition.y = vertical ? pos : -padding.top;
                        break;
                    case TextAnchor.MiddleCenter:
                        anchor.x = vertical ? 0.5f : 0f;
                        anchor.y = vertical ? 1f : 0.5f;
                        anchoredPosition.x = vertical ? 0f : pos;
                        anchoredPosition.y = vertical ? pos : 0f;
                        break;
                    case TextAnchor.LowerLeft:
                        anchor.x = vertical ? 0f : 0f;
                        anchor.y = vertical ? 1f : 0f;
                        anchoredPosition.x = vertical ? padding.left : pos;
                        anchoredPosition.y = vertical ? pos : padding.bottom;
                        break;
                    default:
                        anchor.x = 0f;
                        anchor.y = 1f;
                        anchoredPosition.x = 0f;
                        anchoredPosition.y = 0f;
                        break;
                }
            }
            RectTransform.anchorMin = anchor;
            RectTransform.anchorMax = anchor;
            RectTransform.pivot = anchor;
            RectTransform.anchoredPosition = anchoredPosition;
            //Debug.Log ($"localPosition={RectTransform.localPosition}, pos={pos}");
            return RectTransform.localPosition;
        }

    }

    /// <summary>項目の寄せ方 (水平/垂直)</summary>
    public enum TextAnchor {
        LowerLeft = 0,
        MiddleCenter,
        UpperRight,
    }

}
