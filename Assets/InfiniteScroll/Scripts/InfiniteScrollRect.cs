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

        /// <summary>可視範囲判定の遊び</summary>
        protected const int VisibleRangePlay = 1;

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

        /// <summary>物理項目リスト</summary>
        protected virtual List<InfiniteScrollItemComponentBase> _components { get; set; }

        /// <summary>更新停止</summary>
        public virtual bool LockUpdate { get; protected set; }

        /// <summary>表示中の最初の物理項目の論理インデックス</summary>
        public virtual int FirstIndex { get; protected set; } = -1;

        /// <summary>表示中の最後の物理項目の論理インデックス</summary>
        public virtual int LastIndex { get; protected set; } = -1;

        /// <summary>可視範囲</summary>
        protected virtual (float top, float bottom, int first, int last) VisibleRange {
            get {
                var top = (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
                var bottom = top + ViewportSize;
                var first = -1;
                var last = -1;
                for (var i = 0; i < _items.Count; i++) {
                    if ((_items [i].Position + _items [i].Size) > top && _items [i].Position < bottom) {
                        last = i;
                        if (first < 0) {
                            first = i;
                        }
                        //Debug.Log ($"Items [{i}] {first}-{last}: Position={_items [i].Position}, Size={_items [i].Size}, top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                    }
                }
                //Debug.Log ($"{first}:{FirstIndex}, {last}:{LastIndex} : top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                return (top, bottom, first, last);
            }
        }

        /// <summary>スクロール方向のビューポートサイズ</summary>
        public virtual float ViewportSize => vertical ? viewport.rect.height : viewport.rect.width;

        /// <summary>前回のビューポートサイズ</summary>
        protected Vector2 _lastViewportSize;

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
        /// <param name="hard">リスト自体を削除する(!= 項目を削除してリストを残す)</param>
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

        /// <summary>アイテムの外部更新</summary>
        /// <param name="action">スクロールレクト、項目リスト、可視開始、終了のインデックスが渡われるメソッド</param>
        public virtual void Modify (Action<InfiniteScrollRect, List<InfiniteScrollItemBase>, int, int> action) {
            var lockBackup = LockUpdate;
            LockUpdate = true;
            var first = 0;
            var locked = new List<(InfiniteScrollItemBase item, float offset)> ();
            if (FirstIndex >= 0) {
                for (var i = FirstIndex; i <= LastIndex; i++) {
                    locked.Add (GetScroll (i));
                }
                first = _items.IndexOf (locked [0].item);
                Debug.Log ($"ScrollLocked: {FirstIndex} ({first}) - {LastIndex}, offsets={{{string.Join(",", locked.ConvertAll (l => l.offset))}}}");
            }
            Debug.Log ($"Modify [{_items.Count}]: FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            action (this, _items, FirstIndex, LastIndex);
            if (_items.Count > 0) {
                var lockedOne = locked.Find (l => l.item != null && _items.Contains (l.item));
                FirstIndex = lockedOne == default ? 0 : _items.IndexOf (lockedOne.item);
                SetAverageSize (); // 新規の項目にサイズを設定
                ApplyItems (FirstIndex);
                CalculatePositions ();
                SetScroll (lockedOne);
                Release (FirstIndex, LastIndex);
            } else {
                Clear ();
            }
            LockUpdate = lockBackup;
        }

        /// <summary>初期化</summary>
        /// <param name="items">項目のリスト</param>
        /// <param name="index">最初に表示する項目</param>
        public virtual void Initialize (IEnumerable<InfiniteScrollItemBase> items, int index = 0) {
            if (items == null) { throw new ArgumentNullException ("items"); }
            // 抹消
            Clear (true);
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
            LayoutRebuilder.ForceRebuildLayoutImmediate (transform as RectTransform);
            // 生成
            _items = new List<InfiniteScrollItemBase> (items);
            if (index < 0 || index > _items.Count) { throw new ArgumentOutOfRangeException ("index"); }
            _components = new List<InfiniteScrollItemComponentBase> ();
            FirstIndex = index;
            ApplyItems (FirstIndex);
            SetAverageSize ();
            CalculatePositions ();
            SetScroll (FirstIndex);
            Debug.Log ($"Initialized: viewport={viewport.rect.size}, content={content.rect.size}, first={FirstIndex}, last={LastIndex}, Scroll={Scroll}");
        }

        /// <summary>有効な物理項目の平均サイズを算出して未初期化の論理項目に設定</summary>
        protected virtual void SetAverageSize () {
            var averageSize = 0f;
            var count = 0;
            _components.ForEach (component => { if (component.Index >= 0) { averageSize += component.Size; count++; } });
            averageSize /= count;
            for (var i = 0; i < _items.Count; i++) {
                if (_items [i].Size <= 0) {
                    _items [i].Size = averageSize;
                    Debug.Log ($"Items [{i}].Size={averageSize}");
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

        /// <summary>項目とそのスクロール変位を取得</summary>
        /// <param name="index">省略すると可視範囲の中央辺りの項目</param>
        /// <returns>項目と現在のスクロール位置に対する項目からのオフセットのタプル</returns>
        public virtual (InfiniteScrollItemBase item, float offset) GetScroll (int index = -1) {
            if (_items.Count <= 0) { return (null, 0f); }
            if (index < 0 || index >= _items.Count) {
                index = Mathf.Clamp ((FirstIndex + LastIndex) / 2, 0, _items.Count - 1);
            }
            var offset = ContentSize < ViewportSize ? 0f : (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            Debug.Log ($"ScrollLocked Items [{index}] offset={offset}, FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            return (_items [index], offset - _items [index].Position);
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
            Debug.Log ($"ApplyItems: ({FirstIndex}, {LastIndex}) {(forceClear ? "clear " : "")}{(rebuild ? "rebuild " : "")}=> ({first}, {last}), ContentSize={ContentSize}, Scroll={Scroll}");
            // 解放
            Release (first, last, forceClear);
            // 割当
            var pos = - GetScroll (first).offset;
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

        /// <summary>可視範囲の変化を監視</summary>
        protected virtual void CheckVisibleRange () {
            var range = VisibleRange;
            if (range.first >= 0) {
                if (range.first == FirstIndex - 1 && range.last == LastIndex && (_items [range.first].Position + _items [range.first].Size) <= range.top + VisibleRangePlay) {
                    // 遊びとして除外
                    Debug.Log ($"({FirstIndex}, {LastIndex})=>({range.first}, {range.last}): ({_items [FirstIndex].Position}, {_items [FirstIndex].Size})=>({_items [range.first].Position}, {_items [range.first].Size}) view=({range.top}, {range.bottom})");
                } else if (range.first == FirstIndex + 1 && range.last == LastIndex && (_items [FirstIndex].Position + _items [FirstIndex].Size) <= range.top + VisibleRangePlay) {
                    // 遊びとして除外
                    Debug.Log ($"({FirstIndex}, {LastIndex})=>({range.first}, {range.last}): ({_items [FirstIndex].Position}, {_items [FirstIndex].Size})=>({_items [range.first].Position}, {_items [range.first].Size}) view=({range.top}, {range.bottom})");
                } else if (range.first == FirstIndex && range.last == LastIndex + 1 && _items [range.last].Position >= range.bottom - VisibleRangePlay) {
                    // 遊びとして除外
                    Debug.Log ($"({FirstIndex}, {LastIndex})=>({range.first}, {range.last}): ({_items [LastIndex].Position}, {_items [LastIndex].Size})=>({_items [range.last].Position}, {_items [range.last].Size}) view=({range.top}, {range.bottom})");
                } else if (range.first == FirstIndex && range.last == LastIndex - 1 && _items [LastIndex].Position >= range.bottom - VisibleRangePlay) {
                    // 遊びとして除外
                    Debug.Log ($"({FirstIndex}, {LastIndex})=>({range.first}, {range.last}): ({_items [LastIndex].Position}, {_items [LastIndex].Size})=>({_items [range.last].Position}, {_items [range.last].Size}) view=({range.top}, {range.bottom})");
                } else if (range.first != FirstIndex || range.last != LastIndex) {
                    var locked = GetScroll ();
                    ApplyItems (range.first, range.last);
                    SetScroll (locked);
                    FirstIndex = range.first;
                    LastIndex = range.last;
                }
            }
        }

        /// <summary>ビューポートサイズの変化を監視</summary>
        protected virtual void CheckViewportSize () {
            if (_lastViewportSize != viewport.rect.size) {
                foreach (var component in _components) {
                    if (component.Index >= 0) {
                        component.SetSize ();
                    }
                }
                Debug.Log ($"ViewportSize Chaneed: {_lastViewportSize} => {viewport.rect.size}");
                _lastViewportSize = viewport.rect.size;
            }
        }

        /// <summary>更新</summary>
        protected virtual void Update () {
            if (!LockUpdate && _components?.Count > 0 && FirstIndex >= 0) {
                CheckVisibleRange ();
                CheckViewportSize ();
            }
        }

        /// <summary>破棄</summary>
        protected override void OnDestroy () => Clear (true);

    }

    /// <summary>項目の寄せ方 (水平/垂直)</summary>
    public enum TextAnchor {
        /// <summary>下/左</summary>
        LowerLeft = 6,
        /// <summary>中央</summary>
        MiddleCenter = 4,
        /// <summary>上/右</summary>
        UpperRight = 2,
    }

}
