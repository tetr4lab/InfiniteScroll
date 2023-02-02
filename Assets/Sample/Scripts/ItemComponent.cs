using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InfiniteScroll;

/// <summary>物理アイテム</summary>
public class ItemComponent : InfiniteScrollItemComponentBase {

    #region static
    /// <summary>プレハブ</summary>
    private static GameObject prefab = null;

    /// <summary>生成</summary>
    public static ItemComponent Create (Transform parent) {
        if (prefab == null) {
            prefab = Resources.Load<GameObject> ("Prefabs/Item");
        }
        var obj = Instantiate (prefab, parent);
        return obj.GetComponent<ItemComponent> () ?? obj.AddComponent<ItemComponent> ();
    }
    #endregion

    /// <summary>タイトル</summary>
    public string Title {
        get => _titleText ? _titleText.text : "";
        set {
            if (_titleText) {
                _titleText.text = value ?? "";
            }
        }
    }

    /// <summary>説明</summary>
    public string Description {
        get => _descriptionText ? _descriptionText.text : "";
        set {
            if (_descriptionText) {
                _descriptionText.text = value ?? "";
            }
        }
    }

    /// <summary>アイコン</summary>
    public Sprite Icon {
        get => _iconImage?.sprite;
        set {
            if (_iconImage) {
                _iconImage.sprite = value;
            }
        }
    }

    /// <summary>チェックボックスの状態</summary>
    public bool Check {
        get => _checkBoxToggle?.isOn ?? false;
        set {
            if (_checkBoxToggle) {
                _checkBoxToggle.isOn = value;
            }
        }
    }


    /// <summary>チェックボックスのラベル</summary>
    public string CheckBoxLabel {
        get => _checkBoxLabelText ? _checkBoxLabelText.text : "";
        set {
            if (_checkBoxLabelText) {
                _checkBoxLabelText.text = value ?? "";
            }
        }
    }

    /// <summary>タイトルの初期状態</summary>
    [SerializeField]
    private string _titleString = "";

    /// <summary>説明の初期状態</summary>
    [SerializeField]
    private string _descriptionString = "";

    /// <summary>チェックボックスのラベルの初期状態</summary>
    [SerializeField]
    private string _checkBoxLabelString = "";

    /// <summary>アイコンの初期状態</summary>
    [SerializeField]
    private Sprite _iconSprite = default;

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

    /// <summary>初期化</summary>
    private void Awake () {
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
            _checkBoxToggle.onValueChanged.AddListener (isOn => (Item as Item).Check = isOn);
        }
        Title = _titleString;
        Description = _descriptionString;
        Icon = _iconSprite;
    }


}
