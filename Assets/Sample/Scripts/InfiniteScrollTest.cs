using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InfiniteScroll;

/// <summary>仮想スクロールサンプルメイン</summary>
public class InfiniteScrollTest : MonoBehaviour {

    /// <summary>仮想スクロール実体</summary>
    [SerializeField]
    private InfiniteScrollRect scroll = default;

    /// <summary>乱数トグル</summary>
    [SerializeField]
    private Toggle randomToggle = default;

    /// <summary>垂直トグル</summary>
    [SerializeField]
    private Toggle verticalToggle = default;

    /// <summary>逆順トグル</summary>
    [SerializeField]
    private Toggle reverseToggle = default;

    [SerializeField]
    private Dropdown alignDropdown = default;

    /// <summary>サイズ制御トグル</summary>
    [SerializeField]
    private Toggle ctrlSizeToggle = default;

    /// <summary>リセットボタン</summary>
    [SerializeField]
    private Button resetButton = default;

    /// <summary>インデックススライダ</summary>
    [SerializeField]
    private Slider indexSlider = default;

    /// <summary>追加ボタン</summary>
    [SerializeField]
    private Button addButton = default;

    /// <summary>追加ボタンのラベル</summary>
    private Text addButtonLabel;

    /// <summary>挿入ボタン</summary>
    [SerializeField]
    private Button insertButton = default;

    /// <summary>挿入ボタンのラベル</summary>
    private Text insertButtonLabel;

    /// <summary>除去ボタン</summary>
    [SerializeField]
    private Button removeButton = default;

    /// <summary>除去ボタンのラベル</summary>
    private Text removeButtonLabel;

    /// <summary>指定除去ボタン</summary>
    [SerializeField]
    private Button removeCheckedButton = default;

    /// <summary>全除去ボタン</summary>
    [SerializeField]
    private Button clearButton = default;

    /// <summary>デバッグ表示</summary>
    [SerializeField]
    private Text debugInfo = default;

    /// <summary>アイテムの数</summary>
    [SerializeField]
    private int numberOfItems = 10;

    /// <summary>最初に表示するインデックス</summary>
    [SerializeField]
    private int firstIndex = default;

    /// <summary>論理項目リスト</summary>
    private List<IInfiniteScrollItem> items;

    /// <summary>
    /// 初期化
    ///   コントロールの検出と設定
    /// </summary>
    private void Start () {
        items = new List<IInfiniteScrollItem> ();
        scroll ??= transform.parent.GetComponentInChildren<InfiniteScrollRect> ();
        var toggles = GetComponentsInChildren<Toggle> ();
        randomToggle ??= toggles.GetNth (0);
        randomToggle?.onValueChanged.AddListener ((isOn) => {
            // 項目内容の変更による項目サイズの即時変更
            scroll.ConvertAll (item => (item as Item).Description = $"{(isOn ? RandomSize (scroll.vertical) : (scroll.vertical ? 128 : 400))}\n{(item as Item).Description.Replace ('\n', '↵')}");
            Debug.Log ($"ItemSize {(isOn ? "randomized" : "fixed")}");
        });
        verticalToggle ??= toggles.GetNth (1);
        reverseToggle ??= toggles.GetNth (2);
        ctrlSizeToggle ??= toggles.GetNth (3);
        ctrlSizeToggle?.onValueChanged.AddListener (isOn => { if (alignDropdown) { alignDropdown.interactable = !isOn; } });
        alignDropdown ??= GetComponentInChildren<Dropdown> ();
        var buttons = GetComponentsInChildren<Button> ();
        resetButton ??= buttons.GetNth (0);
        resetButton?.onClick.AddListener (OnReset);
        indexSlider ??= GetComponentInChildren<Slider> ();
        addButton ??= buttons.GetNth (1);
        addButtonLabel = addButton?.GetComponentInChildren<Text> ();
        addButton?.onClick.AddListener (() => scroll.Modify ((scroll, items, first, last) => {
            // 追加処理
            var size = RandomSize (scroll.vertical);
            var index = items.Count;
            items.Add (new Item ($"No. {index}.Add", $"{(randomToggle.isOn ? $"{size}\n" : "")}Add at {index} / {first} - {last}", label: $"check {index}.Add"));
            Debug.Log ($"Added {items [items.Count - 1]}");
        }));
        insertButton ??= buttons.GetNth (2);
        insertButtonLabel = insertButton?.GetComponentInChildren<Text> ();
        insertButton?.onClick.AddListener (() => scroll.Modify ((scroll, items, first, last) => {
            // 挿入処理
            var size = RandomSize (scroll.vertical);
            var index = Mathf.RoundToInt (indexSlider.value);
            items.Insert (index, new Item ($"No. {index}.Insert", $"{(randomToggle.isOn ? $"{size}\n" : "")}Insert at {index} / {first} - {last}", label: $"check {index}.Insert"));
            Debug.Log ($"Inserted {items [index]}");
        }));
        removeButton ??= buttons.GetNth (3);
        removeButtonLabel = removeButton?.GetComponentInChildren<Text> ();
        removeButton?.onClick.AddListener (() => scroll.Modify ((scroll, items, first, last) => {
            // 除去処理
            var index = Mathf.RoundToInt (indexSlider.value);
            var item = items [index];
            items.RemoveAt (index);
            Debug.Log ($"Removed {item}");
        }));
        removeCheckedButton ??= buttons.GetNth (4);
        removeCheckedButton?.onClick.AddListener (() => scroll.Modify ((scroll, items, first, last) => {
            // 指定除去処理
            var targetItems = items.FindAll (item => ((item as Item)?.Check == true));
            foreach (var item in targetItems) {
                items.Remove (item);
            }
            Debug.Log ($"Removed Checked {{{string.Join (", ", targetItems.ConvertAll (i => i.ToString ()))}}}");
        }));
        clearButton ??= buttons.GetNth (5);
        clearButton?.onClick.AddListener (() => {
            // 全除去処理
            scroll.Clear ();
            Debug.Log ($"Removed All");
        });
        debugInfo ??= GameObject.Find ("DebugInfo")?.GetComponentInChildren<Text> ();
        // リセットボタンを押す
        OnReset ();
    }

    /// <summary>更新</summary>
    private void Update () {
        if (scroll.Valid) {
            if (indexSlider) {
                // 項目数に応じたスライダの制限
                indexSlider.maxValue = scroll.Count > 0 ? scroll.Count - 1 : 0;
            }
            if (addButtonLabel) {
                // 追加ボタンのインデックス
                addButtonLabel.text = $"Add {scroll.Count}";
            }
            var index = indexSlider ? Mathf.RoundToInt (indexSlider.value) : (scroll.FirstIndex + scroll.LastIndex) / 2;
            if (insertButtonLabel) {
                // 挿入ボタンのインデックス
                insertButtonLabel.text = $"Insert {(index >= 0 ? index : 0)}";
            }
            if (removeButtonLabel) {
                // 除去ボタンのインデックスと活殺
                removeButtonLabel.text = scroll.Count > 0 ? $"Remove {index}" : "Remove";
                removeButton.interactable = scroll.Count > 0;
            }
            if (removeCheckedButton) {
                // 指定除去ボタンの活殺
                removeCheckedButton.interactable = scroll.Count > 0;
            }
            if (clearButton) {
                // 全除去ボタンの活殺
                clearButton.interactable = scroll.Count > 0;
            }
            if (debugInfo) {
                // デバッグ情報表示
                index = 0;
                debugInfo.text = $@"viewport: {scroll.viewport.rect.size}
content: {scroll.content.rect.size}
scroll: {scroll.Scroll}
visible: {(scroll.FirstIndex < 0 ? "no items" : $"{scroll.FirstIndex} - {scroll.LastIndex}")}
{string.Join ("\n", scroll.ConvertAll (i => 
                    $"{(index >= scroll.FirstIndex && index <= scroll.LastIndex ? "*" : " ")}[{index++}] {Mathf.RoundToInt (i.Position)} - {Mathf.RoundToInt (i.Size)} {((i as Item).Check ? "[x]" : "[ ]")} {(i as Item).Title}"
                ))}";
            }
        }
    }

    /// <summary>不定サイズ</summary>
    private float RandomSize (bool vertical) => (vertical ? 128 : 400) + Random.Range (0, 5) * 40;

    /// <summary>ドロップダウンからenumを得る変換辞書</summary>
    private static readonly Dictionary<int, InfiniteScroll.TextAnchor> _alignDict = new Dictionary<int, InfiniteScroll.TextAnchor> { { 0, InfiniteScroll.TextAnchor.LowerLeft }, { 1, InfiniteScroll.TextAnchor.MiddleCenter }, { 2, InfiniteScroll.TextAnchor.UpperRight }, };

    /// <summary>リセットボタン</summary>
    public void OnReset () {
        items.Clear ();
        // 生成
        if (numberOfItems <= 0) {
            numberOfItems = 10;
        }
        for (var i = 0; i < numberOfItems; i++) {
            scroll.horizontal = !(scroll.vertical = verticalToggle.isOn);
            scroll.reverseArrangement = reverseToggle.isOn;
            scroll.controlChildSize = ctrlSizeToggle.isOn;
            scroll.childAlignment = _alignDict [alignDropdown.value];
            items.Add (new Item ($"No. {i}", $"{(randomToggle.isOn ? $"{RandomSize (scroll.vertical)}\n" : "")}start of {i}\nend of {i}", label: $"check {i}"));
        }
        if (firstIndex < 0 || firstIndex >= numberOfItems) {
            firstIndex = 0;
        }
        scroll.Initialize (items, firstIndex);
        Debug.Log ($"Initialized {{{string.Join (", ", scroll.ConvertAll (i => i.ToString ()))}}}");
    }

}

/// <summary>配列ヘルパー</summary>
public static class ArrayHelper {
    /// <summary>ジェネリック型配列から指定インデックスの要素を安全に取得する</summary>
    /// <typeparam name="T">ジェネリック型(クラス)</typeparam>
    /// <param name="items">ジェネリック型配列</param>
    /// <param name="index">インデックス</param>
    /// <returns>指定されたインデックスが範囲外ならdefault値を返す</returns>
    public static T GetNth<T> (this T [] items, int index) where T : class => index < 0 || index >= items.Length ? default : items [index];
}
