using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InfiniteScroll;

/// <summary>物理アイテム</summary>
public class ItemComponent : InfiniteScrollItemComponentBase {

    /// <summary>プレハブ</summary>
    private static GameObject prefab = null;

    /// <summary>生成</summary>
    public static new ItemComponent Create (InfiniteScrollRect scrollRect, int index) {
        if (prefab == null) {
            prefab = Resources.Load<GameObject> ("Prefabs/Item");
        }
        var obj = Instantiate (prefab, scrollRect.content);
        var component = obj.GetComponent<ItemComponent> () ?? obj.AddComponent<ItemComponent> ();
        component.ScrollRect = scrollRect;
        component.Index = index;
        component.Initialize ();
        return component;
    }

    /// <summary>初期化</summary>
    protected override void Initialize () {
        var texts = GetComponentsInChildren<Text> ();
        if (_titleText == default && texts.Length > 0) {
            _titleText = texts [0];
        }
        if (_descriptionText == default && texts.Length > 1) {
            _descriptionText = texts [1];
        }
        if (_checkBoxLabelText == default && texts.Length > 2) {
            _checkBoxLabelText = texts [2];
        }
        if (_iconImage == default) {
            _iconImage = GetComponentInChildren<Image> ();
        }
        if (_checkBoxToggle == default) {
            _checkBoxToggle = GetComponentInChildren<Toggle> ();
        }
        if (_checkBoxToggle) {
            _checkBoxToggle.onValueChanged.AddListener (isOn => Item.Check = isOn);
        }
    }

    /// <summary>論理項目の状態を反映</summary>
    protected override void Apply () {
        _titleText.text = Item.Title;
        _descriptionText.text = Item.Description;
        _iconImage.sprite = Item.Icon;
        _checkBoxLabelText.text = Item.Label;
        _checkBoxToggle.isOn = Item.Check;
        base.Apply ();
        // 乱数をサイズに反映
        if (int.TryParse (Item.Description.Split ('\n')[0], out var size)) {
            RectTransform.sizeDelta = ScrollRect.vertical ? new Vector2 (RectTransform.sizeDelta.x, size) : new Vector2 (size, RectTransform.sizeDelta.y);
        }
    }

    /// <summary>リンク中の論理項目</summary>
    public new Item Item => ScrollRect [_index] as Item;

    /// <summary>タイトルテキスト</summary>
    [SerializeField]
    private Text _titleText = default;

    /// <summary>説明テキスト</summary>
    [SerializeField]
    private Text _descriptionText = default;

    /// <summary>アイコンイメージ</summary>
    [SerializeField]
    private Image _iconImage = default;

    /// <summary>チェックボックスのトグル</summary>
    [SerializeField]
    private Toggle _checkBoxToggle = default;

    /// <summary>チェックボックスのラベルテキスト</summary>
    [SerializeField]
    private Text _checkBoxLabelText = default;

}
