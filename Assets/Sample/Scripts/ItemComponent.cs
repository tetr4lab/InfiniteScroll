using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tetr4lab.UI;

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
        if (titleText == default && texts.Length > 0) {
            titleText = texts [0];
        }
        if (descriptionText == default && texts.Length > 1) {
            descriptionText = texts [1];
        }
        if (checkBoxLabelText == default && texts.Length > 2) {
            checkBoxLabelText = texts [2];
        }
        if (iconImage == default) {
            iconImage = GetComponentInChildren<Image> ();
        }
        if (checkBoxToggle == default) {
            checkBoxToggle = GetComponentInChildren<Toggle> ();
        }
        if (checkBoxToggle) {
            checkBoxToggle.onValueChanged.AddListener (isOn => Item.Check = isOn);
        }
    }

    /// <summary>論理項目のコンテンツを反映</summary>
    protected override void Apply () {
        titleText.text = Item.Title;
        descriptionText.text = Item.Description;
        iconImage.sprite = Item.Icon;
        checkBoxLabelText.text = Item.Label;
        checkBoxToggle.isOn = Item.Check;
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
    private Text titleText = default;

    /// <summary>説明テキスト</summary>
    [SerializeField]
    private Text descriptionText = default;

    /// <summary>アイコンイメージ</summary>
    [SerializeField]
    private Image iconImage = default;

    /// <summary>チェックボックスのトグル</summary>
    [SerializeField]
    private Toggle checkBoxToggle = default;

    /// <summary>チェックボックスのラベルテキスト</summary>
    [SerializeField]
    private Text checkBoxLabelText = default;

}
