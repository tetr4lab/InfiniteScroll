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
        public virtual bool Valid => Items != null && Components != null;

        /// <summary>論理項目リストへのアクセス</summary>
        public virtual ReadOnlyCollection<InfiniteScrollItemBase> AsReadOnly () => Items.AsReadOnly ();

        /// <summary>論理項目の絞り込み</summary>
        public virtual List<InfiniteScrollItemBase> FindAll (Predicate<InfiniteScrollItemBase> match) => Items.FindAll (match);

        /// <summary>論理項目リストの変換</summary>
        public virtual List<TOutput> ConvertAll<TOutput> (Converter<InfiniteScrollItemBase, TOutput> converter) => Items.ConvertAll (converter);

        /// <summary>論理項目へのアクセス</summary>
        public virtual InfiniteScrollItemBase this [int index] => Items [index];

        /// <summary>論理項目数</summary>
        public virtual int Count => Valid ? Items.Count : 0;

        /// <summary>論理項目リスト</summary>
        protected virtual List<InfiniteScrollItemBase> Items { get; set; }

        /// <summary>物理項目リスト</summary>
        protected virtual List<InfiniteScrollItemComponentBase> Components { get; set; }

        /// <summary>更新停止</summary>
        public virtual bool LockUpdate { get; protected set; }

        /// <summary>サイズの更新請求</summary>
        public virtual bool ResizeRequest { get; set; }

        /// <summary>表示中の最初の物理項目の論理インデックス</summary>
        public virtual int FirstIndex { get; protected set; } = -1;

        /// <summary>表示中の最後の物理項目の論理インデックス</summary>
        public virtual int LastIndex { get; protected set; } = -1;

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
                Items = null;
                Components = null;
            } else {
                Items?.Clear ();
                Components?.Clear ();
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
                first = Items.IndexOf (locked [0].item);
                Debug.Log ($"ScrollLocked: {FirstIndex} ({first}) - {LastIndex}, offsets={{{string.Join(",", locked.ConvertAll (l => l.offset))}}}");
            }
            Debug.Log ($"Modify [{Items.Count}]: FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            action (this, Items, FirstIndex, LastIndex);
            if (Items.Count > 0) {
                var lockedOne = locked.Find (l => l.item != null && Items.Contains (l.item));
                FirstIndex = lockedOne == default ? 0 : Items.IndexOf (lockedOne.item);
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
            Items = new List<InfiniteScrollItemBase> (items);
            if (index < 0 || index > Items.Count) { throw new ArgumentOutOfRangeException ("index"); }
            Components = new List<InfiniteScrollItemComponentBase> ();
            FirstIndex = index;
            ApplyItems (FirstIndex);
            SetAverageSize ();
            CalculatePositions ();
            SetScroll ((Items [FirstIndex], 0));
            Debug.Log ($"Initialized: viewport={viewport.rect.size}, content={content.rect.size}, first={FirstIndex}, last={LastIndex}, Scroll={Scroll}");
        }

        /// <summary>有効な物理項目の平均サイズを算出して未初期化の論理項目に設定</summary>
        protected virtual void SetAverageSize () {
            var averageSize = 0f;
            var count = 0;
            Components.ForEach (component => { if (component.Index >= 0) { averageSize += component.Size; count++; } });
            averageSize /= count;
            for (var i = 0; i < Items.Count; i++) {
                if (Items [i].Size <= 0) {
                    Items [i].Size = averageSize;
                    Debug.Log ($"Items [{i}].Size={averageSize}");
                }
            }
        }

        /// <summary>項目へスクロール</summary>
        /// <param name="point">基準項目とオフセットのタプル</param>
        public virtual void SetScroll ((InfiniteScrollItemBase item, float offset) point) {
            if (point.item == null || !Items.Contains (point.item)) {
                Scroll = (vertical == m_reverseArrangement) ? 0f : 1f;
                return;
            }
            var scroll = (point.item.Position + point.offset) / (ContentSize - ViewportSize);
            Debug.Log ($"SetScroll Items [{Items.IndexOf (point.item)}] offset={point.offset}, Scroll={Scroll} => {((vertical == m_reverseArrangement) ? scroll : 1f - scroll)} ({scroll})");
            Scroll = (vertical == m_reverseArrangement) ? scroll : 1f - scroll;
        }


        /// <summary>項目とそのスクロール変位を取得</summary>
        /// <param name="index">省略すると可視範囲の中央辺りの項目</param>
        /// <returns>項目と現在のスクロール位置に対する項目からのオフセットのタプル</returns>
        public virtual (InfiniteScrollItemBase item, float offset) GetScroll (int index = -1) {
            if (Items.Count <= 0) { return (null, 0f); }
            if (index < 0 || index >= Items.Count) {
                index = Mathf.Clamp ((FirstIndex + LastIndex) / 2, 0, Items.Count - 1);
            }
            var offset = ContentSize < ViewportSize ? 0f : (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            Debug.Log ($"ScrollLocked Items [{index}] offset={offset}, FirstIndex={FirstIndex}, LastIndex={LastIndex}");
            return (Items [index], offset - Items [index].Position);
        }

        /// <summary>項目の位置とコンテントサイズを算出</summary>
        /// <param name="index">Items.Countを指定するとContentのサイズを返す</param>
        protected virtual void CalculatePositions () {
            // サイズ校正
            foreach (var component in Components) {
                if (component.Index >= 0 && component.Size != component.Item.Size) {
                    component.SetSize ();
                }
            }
            // 位置を算出
            float pos = m_reverseArrangement ? (vertical ? m_padding.bottom : m_padding.right) : (vertical ? m_padding.top : m_padding.left);
            for (var i = 0; i < Items.Count; i++) {
                Items [i].Position = pos;
                pos += Items [i].Size + m_spacing;
            }
            pos += m_reverseArrangement ? (vertical ? m_padding.top : m_padding.left) : (vertical ? m_padding.bottom : m_padding.right);
            Debug.Log ($"ContentSize: {ContentSize} => {pos}\nCalculateItemPositions:\n{string.Join ("\n", Items.ConvertAll (i => $"Position={i.Position}, Size={i.Size}"))}");
            ContentSize = pos;
            // 位置を反映
            foreach (var component in Components) {
                if (component.Index >= 0) {
                    component.SetPosition (component.Item.Position);
                }
            }
        }

        /// <summary>物理項目の解放</summary>
        /// <param name="first">保全範囲開始</param>
        /// <param name="last">保全範囲終了</param>
        /// <param name="force">範囲を無視して全解放</param>
        protected virtual void Release (int first, int last, bool force = false) => Components.ForEach (c => { if (force || c.Index < first || c.Index > last) { c.Index = -1; } });

        /// <summary>論理アイテムを反映する</summary>
        /// <param name="first">firstもlastも~Indexと同じ場合は既存の実態をクリアして再構築</param>
        /// <param name="last">firstもlastも~Indexと同じ場合は既存の実態をクリアして再構築</param>
        protected virtual void ApplyItems (int first, int last = -1) {
            var rebuild = last < 0;
            if (rebuild) { last = Items.Count - 1; }
            Debug.Log ($"ApplyItems: ({FirstIndex}, {LastIndex}) {(rebuild ? "rebuild " : "")}=> ({first}, {last}), ContentSize={ContentSize}, Scroll={Scroll}");
            // 解放
            Release (first, last, rebuild);
            // 割当
            var pos = - GetScroll (first).offset;
            for (var i = first; i <= last; i++) {
                var component = Components.Find (c => c.Index == i);
                if (component == null) {
                    component = Components.Find (c => c.Index < 0);
                    if (component != null) {
                        // 再利用
                        component.Index = i;
                        Debug.Log ($"ApplyItems: Reuse Items [{i}].Size={Items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                    } else {
                        // 新規
                        component = Items [i].Create (this, i);
                        component.SetSize ();
                        Components.Add (component);
                        Debug.Log ($"ApplyItems: New Items [{i}].Size={Items [i].Size}");
                        CalculatePositions ();
                    }
                } else {
                    Debug.Log ($"ApplyItems: Exist Items [{i}].Size={Items [i].Size}, first={first}, last={last}, ViewportSize={ViewportSize}");
                }
                if (rebuild) {
                    pos += Items [i].Size + m_spacing;
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

        /// <summary>可視範囲の変化に追従</summary>
        protected virtual void FolowVisibleRange () {
            var top = (vertical == m_reverseArrangement ? Scroll : 1f - Scroll) * (ContentSize - ViewportSize);
            var bottom = top + ViewportSize;
            var first = -1;
            var last = -1;
            for (var i = 0; i < Items.Count; i++) {
                if ((Items [i].Position + Items [i].Size) > top && Items [i].Position < bottom) {
                    last = i;
                    if (first < 0) {
                        first = i;
                    }
                    //Debug.Log ($"Items [{i}] {first}-{last}: Position={_items [i].Position}, Size={_items [i].Size}, top={top}, bottom={bottom}, Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                }
            }
            if (first >= 0 && (first != FirstIndex || last != LastIndex)) {
                Debug.Log ($"({FirstIndex}, {LastIndex}) => ({first}, {last}), ({top}, {bottom}): Scroll={Scroll} * (ContentSize={ContentSize} - ViewportSize={ViewportSize})");
                var locked = GetScroll ();
                ApplyItems (first, last);
                SetScroll (locked);
                FirstIndex = first;
                LastIndex = last;
            }
        }

        /// <summary>ビューポートサイズの変化に追従</summary>
        protected virtual void FolowViewportSize () {
            if (_lastViewportSize != viewport.rect.size) {
                foreach (var component in Components) {
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
            if (!LockUpdate && Components?.Count > 0 && FirstIndex >= 0) {
                if (ResizeRequest) {
                    var locked = GetScroll ();
                    CalculatePositions ();
                    SetScroll (locked);
                    ResizeRequest = false;
                }
                FolowVisibleRange ();
                FolowViewportSize ();
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
