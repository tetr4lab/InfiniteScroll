using System.Collections.Generic;
using UnityEngine;
using InfiniteScroll;

/// <summary>無限スクロールサンプルメイン</summary>
public class InfiniteScrollTest : MonoBehaviour {

    /// <summary>無限スクロール実体</summary>
    [SerializeField]
    private InfiniteScrollRect _scroll = default;

    /// <summary>論理項目リスト</summary>
    private List<Item> _items;

    /// <summary>初期化</summary>
    private void Start () {
        _items = new List<Item> ();
        for (var i = 0; i < 10 ; i++) {
            _items.Add (new Item ($"No. {i}", $"{i}{new string ('\n', i)}end of {i}", label: $"check {i}"));
        }
        _scroll.Initialize (_items, 3);
    }

}
