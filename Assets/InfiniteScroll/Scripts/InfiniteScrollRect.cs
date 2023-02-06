using System.Collections;
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

        /// <summary>可視の最初の実体項目の論理インデックス</summary>
        protected virtual int _firstIndex { get; set; } = -1;

        /// <summary>可視の最後の実体項目の論理インデックス</summary>
        protected virtual int _lastIndex { get; set; } = -1;

        /// <summary>スクロール方向のビューポートサイズ</summary>
        public virtual float ViewportSize => vertical ? viewport.rect.height : viewport.rect.width;

        /// <summary>スクロール方向のコンテントサイズ</summary>
        public virtual float ContentSize {
            get => vertical ? content.rect.height : content.rect.width;
            protected set => content.sizeDelta = vertical ? new Vector2 (content.sizeDelta.x, value) : new Vector2 (value, content.sizeDelta.y);
        }

        /// <summary>スクロール方向のスクロール量</summary>
        public virtual float Scroll {
            get => vertical ? verticalNormalizedPosition : horizontalNormalizedPosition;
            protected set {
                if (vertical) {
                    verticalNormalizedPosition = value;
                } else {
                    horizontalNormalizedPosition = value;
                }
            }
        }

        /// <summary>初期化</summary>
        public virtual void Initialize (IEnumerable<InfiniteScrollItemBase> items, int first = 0) {
            // 消去と整理
            foreach (RectTransform t in content) {
                Destroy (t.gameObject);
            }
            Items = null;
            _components = null;
            _firstIndex = _lastIndex = -1;
            content.sizeDelta = viewport.rect.size;
            LayoutRebuilder.ForceRebuildLayoutImmediate (transform as RectTransform);
            // 生成
            Items = new List<InfiniteScrollItemBase> (items);
            _components = new List<InfiniteScrollItemComponentBase> ();
            var sumSize = 0f + (m_reverseArrangement ? (vertical ? m_padding.bottom : m_padding.right) : (vertical ? m_padding.top : m_padding.left));
            _firstIndex = first; // 可視範囲の開始
            _lastIndex = Items.Count - 1;
            for (var i = first; i <= _lastIndex; i++) {
                sumSize += CreateItem (i).Size + m_spacing;
                if (sumSize > ViewportSize) {
                    // 可視端への到達
                    _lastIndex = i;
                }
            }
            // 仮サイズ
            var averageSize = 0f;
            _components.ForEach (c => averageSize += c.Size);
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
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            } else {
                var x = m_reverseArrangement ? 1f : 0f;
                content.anchorMin = new Vector2 (x, 0f);
                content.anchorMax = new Vector2 (x, 1f);
                content.pivot = new Vector2 (x, 0.5f);
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            }
            CalculatePositions ();
            // 可視の最初へスクロール
            SetScroll (_firstIndex);
            Debug.Log ($"Initialized: viewport={viewport.rect.size}, content={content.rect.size}, first={_firstIndex}, last={_lastIndex}, Scroll={Scroll}");
        }

        /// <summary>スクロール位置を記録</summary>
        public virtual void LockScroll (int index = -1) {
            // thread-unsafe
            if (index < 0 || index > Items.Count) {
                index = (_firstIndex + _lastIndex) / 2;
            }
            _lockedOffset = GetScrollOffset (_lockedIndex = index);
            Debug.Log ($"ScrollLocked Item [{_lockedIndex}] offset={_lockedOffset}");
        }
        protected int _lockedIndex;
        protected float _lockedOffset;

        /// <summary>項目へスクロール</summary>
        /// <param name="index">項目のインデックス</param>
        /// <param name="offset">項目内のオフセット</param>
        public virtual void SetScroll (int index = -1, float offset = 0) {
            if (index < 0 || index > Items.Count) {
                // thread-unsafe
                index = _lockedIndex;
                offset = _lockedOffset;
            }
            var scroll = (Items [index].Position + offset) / (ContentSize - ViewportSize);
            Debug.Log ($"SetScroll Item [{index}] offset={offset}, Scroll={Scroll} => {((vertical == m_reverseArrangement) ? scroll : 1f - scroll)} ({scroll})");
            Scroll = (vertical == m_reverseArrangement) ? scroll : 1f - scroll;
        }

        /// <summary>現在のスクロール位置に対する項目からのオフセットを得る</summary>
        /// <param name="index">項目のインデックス</param>
        /// <returns>オフセット</returns>
        protected virtual float GetScrollOffset (int index) {
            var offset = (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            Debug.Log ($"Scroll.Offset={offset} - Item [{index}].Position={Items [index].Position} ={offset - Items [index].Position}");
            return offset - Items [index].Position;
        }

        /// <summary>項目の位置とコンテントサイズを算出</summary>
        /// <param name="index">Items.Countを指定するとContentのサイズを返す</param>
        public virtual void CalculatePositions () {
            foreach (var component in _components) {
                if (component.Index >= 0 && component.Size != component.Item.Size) {
                    component.SetSize ();
                }
            }
            float pos = m_reverseArrangement ? (vertical ? m_padding.bottom : m_padding.right) : (vertical ? m_padding.top : m_padding.left);
            for (var i = 0; i < Items.Count; i++) {
                Items [i].Position = pos;
                pos += Items [i].Size + m_spacing;
            }
            pos += m_reverseArrangement ? (vertical ? m_padding.top : m_padding.left) : (vertical ? m_padding.bottom : m_padding.right);
            Debug.Log ($"ContentSize: {ContentSize} => {pos}\nCalculateItemPositions:\n{string.Join ("\n", Items.ConvertAll (i => $"Position={i.Position}, Size={i.Size}"))}");
            ContentSize = pos;
            foreach (var component in _components) {
                if (component.Index >= 0) {
                    component.SetPosition (component.Item.Position);
                }
            }
        }

        /// <summary>項目の生成</summary>
        protected virtual InfiniteScrollItemComponentBase CreateItem (int index) {
            var component = Items [index].Create (this, index);
            component.SetSize ();
            _components.Add (component);
            Debug.Log ($"ApplyItems: New Items [{index}].Size={Items [index].Size}");
            return component;
        }

        /// <summary>論理アイテムを反映する</summary>
        protected virtual void ApplyItems (int first, int last) {
            Debug.Log ($"ApplyItems: ({_firstIndex}, {_lastIndex}) => ({first}, {last}), ContentSize={ContentSize}, Scroll={Scroll}");
            LockScroll ();
            // 解放
            _components.ForEach (c => { if (c.Index < first || c.Index > last) { c.Index = -1; } });
            // 割当
            for (var i = first; i <= last; i++) {
                var component = _components.Find (c => c.Index == i);
                if (component == null) {
                    component = _components.Find (c => c.Index < 0);
                    if (component != null) {
                        // 再利用
                        component.Index = i;
                        Debug.Log ($"ApplyItems: Reuse Items [{i}].Size={Items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                    } else {
                        // 新規
                        component = CreateItem (i);
                        CalculatePositions ();
                    }
                } else {
                    Debug.Log ($"ApplyItems: Exist Items [{i}].Size={Items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                }
            }
            SetScroll ();
            _firstIndex = first;
            _lastIndex = last;
        }

        /// <summary>表示範囲の変化を監視</summary>
        protected virtual void CheckVisibleRange () {
            var top = (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            var bottom = top + ViewportSize;
            var first = -1;
            var last = -1;
            for (var i = 0; i < Items.Count; i++) {
                if ((Items [i].Position + Items[i].Size) >= top && Items [i].Position <= bottom) {
                    last = i;
                    if (first < 0) {
                        first = i;
                    }
                    //Debug.Log ($"Items [{i}] {first}-{last}: Position={Items [i].Position}, Size={Items [i].Size}, top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                }
            }
            //Debug.Log ($"{first}:{_firstIndex}, {last}:{_lastIndex} : top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
            if (first >= 0 && (first != _firstIndex || last != _lastIndex)) {
                ApplyItems (first, last);
            }
        }

        /// <summary>更新</summary>
        private void Update () {
            if (_components?.Count > 0 &&_firstIndex >= 0) {
                CheckVisibleRange ();
            }
        }

        /// <summary>破棄</summary>
        protected override void OnDestroy () {
            if (_components != null) {
                foreach (var item in _components) {
                    Destroy (item.gameObject);
                }
                _components = null;
                Items = null;
                _firstIndex = _lastIndex = -1;
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
        public virtual InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index) {
            var component = InfiniteScrollItemComponentBase.Create (scrollRect, index);
            return component;
        }

        /// <summary>スクロール方向の位置 (実態へ反映)</summary>
        public virtual float Position { get; protected internal set; }

        /// <summary>スクロール方向のサイズ (実体から反映)</summary>
        public virtual float Size { get; protected internal set; }

        /// <summary>内容に変更があった</summary>
        public virtual bool Dirty { get; protected internal set; }

        /// <summary>コンストラクタ</summary>
        public InfiniteScrollItemBase () { }

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
        public static InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index) {
            throw new System.NotImplementedException ("Must be implemented in a derived class");
#pragma warning disable CS0162 // 到達できないコードが検出されました
            // 以下参考
            var obj = new GameObject ("InfiniteScrollItem");
            var rect = obj.AddComponent<RectTransform> ();
            obj.transform.SetParent (scrollRect.content);
            var component = (obj.GetComponent<InfiniteScrollItemComponentBase> () ?? obj.AddComponent<InfiniteScrollItemComponentBase> ());
            component.ScrollRect = scrollRect;
            component.Index = index;
            component.Initialize ();
            return component;
#pragma warning restore CS0162 // 到達できないコードが検出されました
        }

        #endregion

        /// <summary>初期化</summary>
        protected virtual void Initialize () { }

        /// <summary>親のスクロールレクト</summary>
        public virtual InfiniteScrollRect ScrollRect { get; protected set; }

        /// <summary>項目のパディング</summary>
        protected virtual RectOffset m_padding => ScrollRect.m_padding;

        /// <summary>項目のサイズを制御</summary>
        protected virtual bool m_controlChildSize => ScrollRect.m_controlChildSize;

        /// <summary>項目の配向</summary>
        protected virtual TextAnchor m_childAlignment => ScrollRect.m_childAlignment;

        /// <summary>逆並び</summary>
        protected virtual bool m_reverseArrangement => ScrollRect.m_reverseArrangement;

        /// <summary>スクロールの向き 垂直/!水平</summary>
        protected virtual bool vertical => ScrollRect.vertical;

        /// <summary>ビューポート矩形</summary>
        protected virtual Rect viewportRect => ScrollRect.viewport.rect;

        /// <summary>レクトトランスフォーム</summary>
        public virtual RectTransform RectTransform => _rectTransform ?? transform as RectTransform ?? gameObject.AddComponent<RectTransform> ();
        protected RectTransform _rectTransform;

        /// <summary>サイズ</summary>
        public virtual float Size => ScrollRect.vertical ? RectTransform.rect.height : RectTransform.rect.width;

        /// <summary>リンク中の論理項目インデックス</summary>
        public virtual int Index {
            get => _index;
            set {
                _index = value;
                if (value >= 0) {
                    Apply ();
                    SetPosition (Item.Position);
                    gameObject.SetActive (true);
                } else {
                    gameObject.SetActive (false);
                }
            }
        }
        protected int _index;

        /// <summary>リンク中の論理項目</summary>
        public virtual InfiniteScrollItemBase Item => (_index < 0 || _index >= ScrollRect.Items.Count) ? null : ScrollRect.Items [_index];

        /// <summary>論理項目の状態を反映</summary>
        protected virtual void Apply () => Item.Dirty = false;

        /// <summary>更新</summary>
        protected virtual void Update () {
            if (ScrollRect && Index >= 0) {
                if (Item.Dirty) {
                    Apply ();
                }
                if (Item.Size != Size) {
                    ScrollRect.LockScroll ();
                    Item.Size = Size;
                    ScrollRect.CalculatePositions ();
                    ScrollRect.SetScroll ();
                }
            }
        }

        /// <summary>項目のサイズ決め</summary>
        protected internal virtual void SetSize () {
            if (m_controlChildSize) {
                RectTransform.sizeDelta = vertical
                    ? new Vector2 (viewportRect.size.x - m_padding.left - m_padding.right, RectTransform.sizeDelta.y)
                    : new Vector2 (RectTransform.sizeDelta.x, viewportRect.size.y - m_padding.top - m_padding.bottom);
            }
            for (var i = 0; i < MaxNumberOfLayoutRebuilds; i++) {
                var lastSize = Size;
                LayoutRebuilder.ForceRebuildLayoutImmediate (RectTransform);
                Debug.Log ($"Item [{Index}].SetSize({i}) {lastSize} => {Size}: controlSize={m_controlChildSize}, localPosition={RectTransform.localPosition}, sizeDelta{RectTransform.sizeDelta}");
                if (Mathf.Approximately (lastSize, Size)) {
                    break;
                }
            }
            Debug.Log ($"Item [{Index}].SetSize {Item.Size} => {Size}");
            Item.Size = Size;
        }

        /// <summary>項目の位置決め</summary>
        protected internal virtual void SetPosition (float pos) {
            pos *= (vertical == m_reverseArrangement) ? 1f : -1f;
            Vector2 anchor;
            Vector2 anchoredPosition;
            if (m_reverseArrangement) {
                switch (m_childAlignment) {
                    case TextAnchor.UpperRight:
                        anchor.x = vertical ? 1f : 1f;
                        anchor.y = vertical ? 0f : 1f;
                        anchoredPosition.x = vertical ? -m_padding.right : pos;
                        anchoredPosition.y = vertical ? pos : -m_padding.top;
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
                        anchoredPosition.x = vertical ? m_padding.left : pos;
                        anchoredPosition.y = vertical ? pos : m_padding.bottom;
                        break;
                    default:
                        anchor.x = 1f;
                        anchor.y = 0f;
                        anchoredPosition.x = 0f;
                        anchoredPosition.y = 0f;
                        break;
                }
            } else {
                switch (m_childAlignment) {
                    case TextAnchor.UpperRight:
                        anchor.x = vertical ? 1f : 0f;
                        anchor.y = vertical ? 1f : 1f;
                        anchoredPosition.x = vertical ? -m_padding.right : pos;
                        anchoredPosition.y = vertical ? pos : -m_padding.top;
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
                        anchoredPosition.x = vertical ? m_padding.left : pos;
                        anchoredPosition.y = vertical ? pos : m_padding.bottom;
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
        }

    }

    /// <summary>項目の寄せ方 (水平/垂直)</summary>
    public enum TextAnchor {
        LowerLeft = 0,
        MiddleCenter,
        UpperRight,
    }

}
