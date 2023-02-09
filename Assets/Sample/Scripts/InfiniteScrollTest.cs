using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InfiniteScroll;

/// <summary>無限スクロールサンプルメイン</summary>
public class InfiniteScrollTest : MonoBehaviour {

    /// <summary>無限スクロール実体</summary>
    [SerializeField]
    private InfiniteScrollRect _scroll = default;

    /// <summary>乱数トグル</summary>
    [SerializeField]
    private Toggle _randomToggle = default;

    /// <summary>垂直トグル</summary>
    [SerializeField]
    private Toggle _verticalToggle = default;

    /// <summary>逆順トグル</summary>
    [SerializeField]
    private Toggle _reverseToggle = default;

    [SerializeField]
    private Dropdown _alignDropdown = default;

    /// <summary>サイズ制御トグル</summary>
    [SerializeField]
    private Toggle _ctrlSizeToggle = default;

    /// <summary>リセットボタン</summary>
    [SerializeField]
    private Button _resetButton = default;

    /// <summary>インデックススライダ</summary>
    [SerializeField]
    private Slider _indexSlider = default;

    /// <summary>追加ボタン</summary>
    [SerializeField]
    private Button _addButton = default;

    /// <summary>追加ボタンのラベル</summary>
    private Text _addButtonLabel;

    /// <summary>挿入ボタン</summary>
    [SerializeField]
    private Button _insertButton = default;

    /// <summary>挿入ボタンのラベル</summary>
    private Text _insertButtonLabel;

    /// <summary>抹消ボタン</summary>
    [SerializeField]
    private Button _removeButton = default;

    /// <summary>抹消ボタンのラベル</summary>
    private Text _removeButtonLabel;

    /// <summary>抹消ボタン</summary>
    [SerializeField]
    private Button _clearButton = default;

    /// <summary>デバッグ表示</summary>
    [SerializeField]
    private Text _debugInfo = default;

    /// <summary>論理項目リスト</summary>
    private List<Item> _items;

    /// <summary>
    /// 初期化
    ///   コントロールの検出と設定
    /// </summary>
    private void Start () {
        _items = new List<Item> ();
        _scroll ??= transform.parent.GetComponentInChildren<InfiniteScrollRect> ();
        var toggles = GetComponentsInChildren<Toggle> ();
        _randomToggle ??= toggles.GetNth (0);
        _verticalToggle ??= toggles.GetNth (1);
        _reverseToggle ??= toggles.GetNth (2);
        _ctrlSizeToggle ??= toggles.GetNth (3);
        _ctrlSizeToggle?.onValueChanged.AddListener (isOn => { if (_alignDropdown) { _alignDropdown.interactable = !isOn; } });
        _alignDropdown ??= GetComponentInChildren<Dropdown> ();
        var buttons = GetComponentsInChildren<Button> ();
        _resetButton ??= buttons.GetNth (0);
        _resetButton?.onClick.AddListener (OnReset);
        _indexSlider ??= GetComponentInChildren<Slider> ();
        _addButton ??= buttons.GetNth (1);
        _addButtonLabel = _addButton?.GetComponentInChildren<Text> ();
        _addButton?.onClick.AddListener (() => _scroll.Modify ((scroll, items, first, last) => {
            // 追加処理
            var size = RandomSize (scroll.vertical);
            var index = items.Count;
            items.Add (new Item ($"No. {index}.Add", $"{(_randomToggle.isOn ? $"{size}\n" : "")}Add at {index} / {first} - {last}", label: $"check {index}.Add"));
            Debug.Log ($"Added {items [items.Count - 1]}");
        }));
        _insertButton ??= buttons.GetNth (2);
        _insertButtonLabel = _insertButton?.GetComponentInChildren<Text> ();
        _insertButton?.onClick.AddListener (() => _scroll.Modify ((scroll, items, first, last) => {
            // 挿入処理
            var size = RandomSize (scroll.vertical);
            var index = Mathf.RoundToInt (_indexSlider.value);
            items.Insert (index, new Item ($"No. {index}.Insert", $"{(_randomToggle.isOn ? $"{size}\n" : "")}Insert at {index} / {first} - {last}", label: $"check {index}.Insert"));
            Debug.Log ($"Inserted {items [index]}");
        }));
        _removeButton ??= buttons.GetNth (3);
        _removeButtonLabel = _removeButton?.GetComponentInChildren<Text> ();
        _removeButton?.onClick.AddListener (() => _scroll.Modify ((scroll, items, first, last) => {
            // 除去処理
            var index = Mathf.RoundToInt (_indexSlider.value);
            var item = items [index];
            items.RemoveAt (index);
            Debug.Log ($"Removed {item}");
        }));
        _clearButton ??= buttons.GetNth (4);
        _clearButton?.onClick.AddListener (() => {
            // 全除去処理
            _scroll.Clear ();
            Debug.Log ($"Removed All");
        });
        _debugInfo ??= GameObject.Find ("DebugInfo")?.GetComponentInChildren<Text> ();
        // リセットボタンを押す
        OnReset ();
    }

    /// <summary>更新</summary>
    private void Update () {
        if (_scroll.Valid) {
            if (_indexSlider) {
                // 項目数に応じたスライダの制限
                _indexSlider.maxValue = _scroll.Count > 0 ? _scroll.Count - 1 : 0;
            }
            if (_addButtonLabel) {
                // 追加ボタンのインデックス
                _addButtonLabel.text = $"Add {_scroll.Count}";
            }
            var index = _indexSlider ? Mathf.RoundToInt (_indexSlider.value) : (_scroll.FirstIndex + _scroll.LastIndex) / 2;
            if (_insertButtonLabel) {
                // 挿入ボタンのインデックス
                _insertButtonLabel.text = $"Insert {(index >= 0 ? index : 0)}";
            }
            if (_removeButtonLabel) {
                // 除去ボタンのインデックスと活殺
                _removeButtonLabel.text = _scroll.Count > 0 ? $"Remove {index}" : "Remove";
                _removeButton.interactable = _scroll.Count > 0;
            }
            if (_clearButton) {
                // 全除去ボタンの活殺
                _clearButton.interactable = _scroll.Count > 0;
            }
            if (_debugInfo) {
                // デバッグ情報表示
                index = 0;
                _debugInfo.text = $@"{_scroll.FirstIndex} - {_scroll.LastIndex}
{_scroll.ViewportSize} / {_scroll.ContentSize}
{_scroll.Scroll}
{string.Join ("\n", _scroll.ConvertAll (i => 
                    $"{(index >= _scroll.FirstIndex && index <= _scroll.LastIndex ? "*" : " ")}{index++}: {Mathf.RoundToInt (i.Position)} - {Mathf.RoundToInt (i.Size)} {((i as Item).Check ? "[x]" : "[ ]")} {(i as Item).Title}"
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
        _items.Clear ();
        // 生成
        for (var i = 0; i < 10; i++) {
            _scroll.horizontal = !(_scroll.vertical = _verticalToggle.isOn);
            _scroll.m_reverseArrangement = _reverseToggle.isOn;
            _scroll.m_controlChildSize = _ctrlSizeToggle.isOn;
            _scroll.m_childAlignment = _alignDict [_alignDropdown.value];
            _items.Add (new Item ($"No. {i}", $"{(_randomToggle.isOn ? $"{RandomSize (_scroll.vertical)}\n" : "")}start of {i}\nend of {i}", label: $"check {i}"));
        }
        _scroll.Initialize (_items);
        Debug.Log ($"Initialized {{{string.Join (", ", _scroll.ConvertAll (i => i.ToString ()))}}}");
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
