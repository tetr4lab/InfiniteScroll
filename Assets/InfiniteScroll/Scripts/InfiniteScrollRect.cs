using System;
using System.Collections;
using System.Collections.ObjectModel;
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

        /// <summary>有効</summary>
        public virtual bool Valid => _items != null && _components != null;

        /// <summary>論理項目リストへのアクセス</summary>
        public virtual ReadOnlyCollection<InfiniteScrollItemBase> AsReadOnly () => _items.AsReadOnly ();

        /// <summary>論理項目の絞り込み</summary>
        public virtual List<InfiniteScrollItemBase> FindAll (Predicate<InfiniteScrollItemBase> match) => _items.FindAll (match);

        /// <summary>論理項目リストの変換</summary>
        public virtual List<TOutput> ConvertAll<TOutput> (Converter<InfiniteScrollItemBase, TOutput> converter) => _items.ConvertAll (converter);

        /// <summary>論理項目へのアクセス</summary>
        public virtual InfiniteScrollItemBase this [int index] => _items [index];

        /// <summary>論理項目数</summary>
        public virtual int Count => Valid ? _items.Count : 0;

        /// <summary>論理項目リスト</summary>
        protected virtual List<InfiniteScrollItemBase> _items { get; set; }

        /// <summary>実体項目リスト</summary>
        protected virtual List<InfiniteScrollItemComponentBase> _components { get; set; }

        /// <summary>更新停止</summary>
        public virtual bool LockUpdate { get; protected set; }

        /// <summary>可視の最初の実体項目の論理インデックス</summary>
        public virtual int FirstIndex { get; protected set; } = -1;

        /// <summary>可視の最後の実体項目の論理インデックス</summary>
        public virtual int LastIndex { get; protected set; } = -1;

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

        /// <summary>クリア</summary>
        public virtual void Clear (bool hard = false) {
            foreach (RectTransform t in content) {
                Destroy (t.gameObject);
            }
            if (hard) {
                _items = null;
                _components = null;
            } else {
                _items?.Clear ();
                _components?.Clear ();
            }
            FirstIndex = LastIndex = -1;
            Debug.Log ($"hard={hard} {content.sizeDelta}, {viewport.rect.size}");
            content.sizeDelta = Vector2.zero;
        }

        /// <summary>アイテムの更新</summary>
        public virtual void Modify (Action<InfiniteScrollRect, List<InfiniteScrollItemBase>, int, int> action) {
            var lockBackup = LockUpdate;
            LockUpdate = true;
            var first = 0;
            var locked = new List<(InfiniteScrollItemBase item, float offset)> ();
            if (FirstIndex >= 0) {
                for (var i = FirstIndex; i <= LastIndex; i++) {
                    locked.Add (PackScroll (i));
                }
                first = _items.IndexOf (locked [0].item);
                Debug.Log ($"ScrollLocked: {FirstIndex} ({first}) - {LastIndex}, offsets={{{string.Join(",", locked.ConvertAll (l => l.offset))}}}");
            }
            Debug.Log ($"Modify [{_items.Count}]: FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            action (this, _items, FirstIndex, LastIndex);
            if (_items.Count > 0) {
                FirstIndex = (first < _items.Count) ? first : 0;
                ApplyItems (FirstIndex);
                CalculatePositions ();
                SetScroll (locked);
                Release (FirstIndex, LastIndex);
            } else {
                Clear ();
            }
            LockUpdate = lockBackup;

            (InfiniteScrollItemBase item, float offset) PackScroll (int index) => (_items [index], GetScrollOffset (index));
        }

        /// <summary>初期化</summary>
        public virtual void Initialize (IEnumerable<InfiniteScrollItemBase> items, int index = 0) {
            if (items == null) { throw new ArgumentNullException ("items"); }
            // 抹消
            Clear (true);
            LayoutRebuilder.ForceRebuildLayoutImmediate (transform as RectTransform);
            // 生成
            _items = new List<InfiniteScrollItemBase> (items);
            if (index < 0 || index > _items.Count) { throw new ArgumentOutOfRangeException ("index"); }
            _components = new List<InfiniteScrollItemComponentBase> ();
            // 生成
            var sumSize = 0f + (m_reverseArrangement ? (vertical ? m_padding.bottom : m_padding.right) : (vertical ? m_padding.top : m_padding.left));
            FirstIndex = index; // 可視範囲の開始
            LastIndex = _items.Count - 1;
            for (var i = index; i <= LastIndex; i++) {
                sumSize += CreateItem (i).Size + m_spacing;
                if (sumSize > ViewportSize) {
                    // 可視端への到達
                    LastIndex = i;
                }
            }
            // 仮サイズ
            var averageSize = 0f;
            _components.ForEach (c => averageSize += c.Size);
            averageSize /= (LastIndex - FirstIndex + 1);
            for (var i = 0; i < FirstIndex; i++) {
                _items [i].Size = averageSize;
            }
            for (var i = LastIndex + 1; i < _items.Count; i++) {
                _items [i].Size = averageSize;
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
            SetScroll (FirstIndex);
            Debug.Log ($"Initialized: viewport={viewport.rect.size}, content={content.rect.size}, first={FirstIndex}, last={LastIndex}, Scroll={Scroll}");
        }

        /// <summary>スクロール位置を記録</summary>
        public virtual (InfiniteScrollItemBase item, float offset) LockScroll (int index = -1) {
            if (_items.Count <= 0) { return (null, 0f); }
            if (index < 0 || index >= _items.Count) {
                index = Mathf.Clamp ((FirstIndex + LastIndex) / 2, 0, _items.Count - 1);
            }
            var offset = GetScrollOffset (index);
            Debug.Log ($"ScrollLocked Items [{index}] offset={offset}, FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            return (_items [index], offset);
        }

        /// <summary>項目へスクロール</summary>
        public virtual void SetScroll (IEnumerable<(InfiniteScrollItemBase item, float offset)> lockeds) {
            foreach (var locked in lockeds) {
                if (locked.item != null && _items.IndexOf (locked.item) >= 0) {
                    SetScroll (locked.item, locked.offset);
                    return;
                }
            }
        }

        /// <summary>項目へスクロール</summary>
        /// <param name="item">基準項目</param>
        /// <param name="offset">基準項目からのオフセット</param>
        public virtual void SetScroll (InfiniteScrollItemBase item, float offset = 0) {
            if (item == null || _items.IndexOf (item) < 0) {
                Scroll = (vertical == m_reverseArrangement) ? 0f : 1f;
                return;
            }
            var scroll = (item.Position + offset) / (ContentSize - ViewportSize);
            Debug.Log ($"SetScroll Items [{_items.IndexOf (item)}] offset={offset}, Scroll={Scroll} => {((vertical == m_reverseArrangement) ? scroll : 1f - scroll)} ({scroll})");
            Scroll = (vertical == m_reverseArrangement) ? scroll : 1f - scroll;
        }

        /// <summary>項目へスクロール</summary>
        /// <param name="index">基準項目のインデックス</param>
        /// <param name="offset">基準項目からのオフセット</param>
        public virtual void SetScroll (int index, float offset = 0) => SetScroll (_items.Count <= 0 ? null : _items [index], offset);

        /// <summary>項目へスクロール</summary>
        /// <param name="point">基準項目とオフセット</param>
        public virtual void SetScroll ((InfiniteScrollItemBase item, float offset) point) => SetScroll (_items.Count <= 0 ? null : point.item, point.offset);

        /// <summary>現在のスクロール位置に対する項目からのオフセットを得る</summary>
        /// <param name="index">項目のインデックス</param>
        /// <returns>オフセット</returns>
        protected virtual float GetScrollOffset (int index) {
            if (index < 0 || index >=_items.Count) { return 0f; }
            var offset = ContentSize < ViewportSize ? 0f : (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            //Debug.Log ($"Scroll.Offset={offset} - Items [{index}].Position={_items [index].Position} ={offset - _items [index].Position}, Scroll={Scroll}, ScrollableSize={ContentSize - ViewportSize}");
            return offset - _items [index].Position;
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
            for (var i = 0; i < _items.Count; i++) {
                _items [i].Position = pos;
                pos += _items [i].Size + m_spacing;
            }
            pos += m_reverseArrangement ? (vertical ? m_padding.top : m_padding.left) : (vertical ? m_padding.bottom : m_padding.right);
            Debug.Log ($"ContentSize: {ContentSize} => {pos}\nCalculateItemPositions:\n{string.Join ("\n", _items.ConvertAll (i => $"Position={i.Position}, Size={i.Size}"))}");
            ContentSize = pos;
            foreach (var component in _components) {
                if (component.Index >= 0) {
                    component.SetPosition (component.Item.Position);
                }
            }
        }

        /// <summary>物理項目の生成</summary>
        protected virtual InfiniteScrollItemComponentBase CreateItem (int index) {
            var component = _items [index].Create (this, index);
            component.SetSize ();
            _components.Add (component);
            Debug.Log ($"ApplyItems: New Items [{index}].Size={_items [index].Size}");
            return component;
        }

        /// <summary>物理項目の解放</summary>
        /// <param name="first">保全範囲開始</param>
        /// <param name="last">保全範囲終了</param>
        /// <param name="force">範囲を無視して全解放</param>
        protected virtual void Release (int first, int last, bool force = false) => _components.ForEach (c => { if (force || c.Index < first || c.Index > last) { c.Index = -1; } });

        /// <summary>論理アイテムを反映する</summary>
        /// <param name="first">firstもlastも~Indexと同じ場合は既存の実態をクリアして再構築</param>
        /// <param name="last">firstもlastも~Indexと同じ場合は既存の実態をクリアして再構築</param>
        protected virtual void ApplyItems (int first, int last = -1) {
            var forceClear = FirstIndex == first && LastIndex == last || last < 0;
            var rebuild = last < 0;
            if (rebuild) { last = _items.Count - 1; }
            Debug.Log ($"ApplyItems: ({FirstIndex}, {LastIndex}) {(forceClear ? "clear " : "")}=> ({first}, {last}), ContentSize={ContentSize}, Scroll={Scroll}");
            // 解放
            Release (first, last, forceClear);
            // 割当
            var pos = - GetScrollOffset (first);
            for (var i = first; i <= last; i++) {
                var component = _components.Find (c => c.Index == i);
                if (component == null) {
                    component = _components.Find (c => c.Index < 0);
                    if (component != null) {
                        // 再利用
                        component.Index = i;
                        Debug.Log ($"ApplyItems: Reuse Items [{i}].Size={_items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                    } else {
                        // 新規
                        component = CreateItem (i);
                        CalculatePositions ();
                    }
                } else {
                    Debug.Log ($"ApplyItems: Exist Items [{i}].Size={_items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                }
                if (rebuild) {
                    pos += _items [i].Size + m_spacing;
                    Debug.Log ($"ApplyItems [{i}]: pos={pos} {(pos > ViewportSize ? ">" : "<=")} ViewportSize={ViewportSize}");
                    if (pos > ViewportSize) {
                        // 可視端への到達
                        last = i;
                    }
                }
            }
            if (rebuild && LastIndex != last) {
                Debug.Log ($"ApplyItems rebuild: first={first}, last={LastIndex} => {last}, ViewportSize={ViewportSize}");
                LastIndex = last;
            }
        }

        /// <summary>表示範囲の変化を監視</summary>
        protected virtual void CheckVisibleRange () {
            var top = (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            var bottom = top + ViewportSize;
            var first = -1;
            var last = -1;
            for (var i = 0; i < _items.Count; i++) {
                if ((_items [i].Position + _items[i].Size) > top && _items [i].Position < bottom) {
                    last = i;
                    if (first < 0) {
                        first = i;
                    }
                    //Debug.Log ($"Items [{i}] {first}-{last}: Position={_items [i].Position}, Size={_items [i].Size}, top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                }
            }
            //Debug.Log ($"{first}:{FirstIndex}, {last}:{LastIndex} : top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
            if (first >= 0 && (first != FirstIndex || last != LastIndex)) {
                var locked = LockScroll ();
                ApplyItems (first, last);
                SetScroll (locked);
                FirstIndex = first;
                LastIndex = last;
            }
        }

        /// <summary>更新</summary>
        private void Update () {
            if (!LockUpdate && _components?.Count > 0 && FirstIndex >= 0) {
                CheckVisibleRange ();
            }
        }

        /// <summary>破棄</summary>
        protected override void OnDestroy () => Clear (true);

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
            throw new NotImplementedException ("Must be implemented in a derived class");
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
        protected virtual RectTransform RectTransform => _rectTransform ?? transform as RectTransform ?? gameObject.AddComponent<RectTransform> ();
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
        public virtual InfiniteScrollItemBase Item => (_index < 0 || _index >= ScrollRect.Count) ? null : ScrollRect [_index];

        /// <summary>論理項目の状態を反映</summary>
        protected virtual void Apply () => Item.Dirty = false;

        /// <summary>更新</summary>
        protected virtual void Update () {
            if (ScrollRect && !ScrollRect.LockUpdate && Index >= 0) {
                if (Item.Dirty) {
                    Apply ();
                }
                if (Item.Size != Size) {
                    var locked = ScrollRect.LockScroll ();
                    Item.Size = Size;
                    ScrollRect.CalculatePositions ();
                    ScrollRect.SetScroll (locked);
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
                Debug.Log ($"Items [{Index}].SetSize({i}) {lastSize} => {Size}: controlSize={m_controlChildSize}, localPosition={RectTransform.localPosition}, sizeDelta{RectTransform.sizeDelta}");
                if (Mathf.Approximately (lastSize, Size)) {
                    break;
                }
            }
            Debug.Log ($"Items [{Index}].SetSize {Item.Size} => {Size}");
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
        LowerLeft = 6,
        MiddleCenter = 4,
        UpperRight = 2,
    }

}
