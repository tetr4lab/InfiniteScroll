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

    /// <summary>論理項目リスト</summary>
    private List<Item> _items;

    /// <summary>初期化</summary>
    private void Start () {
        _items = new List<Item> ();
        OnReset ();
    }

    /// <summary>リセットボタン</summary>
    public void OnReset () {
        _items.Clear ();
        for (var i = 0; i < 10; i++) {
            _scroll.horizontal = !(_scroll.vertical = _verticalToggle.isOn);
            _scroll.m_reverseArrangement = _reverseToggle.isOn;
            _scroll.m_controlChildSize = _ctrlSizeToggle.isOn;
            _scroll.m_childAlignment = (InfiniteScroll.TextAnchor) _alignDropdown.value;
            var size = (_scroll.vertical ? 128 : 400) + Random.Range (0, 5) * 40; // 乱数を埋め込む
            _items.Add (new Item ($"No. {i}", $"{(_randomToggle.isOn ? $"{size}\n" : "")}start of {i} {new string ('\n', 1)}end of {i}", label: $"check {i}"));
        }
        _scroll.Initialize (_items, 3);
    }

    /// <summary>レポートボタン</summary>
    public void OnReport () {
        Debug.Log ($"Checks: {string.Join (", ", _scroll.Items.ConvertAll (i => $"{(i as Item).Title}: {(i as Item).Check}"))}");
    }

}
