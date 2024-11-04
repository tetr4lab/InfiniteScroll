using Tetr4lab.InfiniteScroll;
using UnityEngine;
using UnityEngine.UI;

/// <summary>論理アイテム</summary>
public class Item : IInfiniteScrollItem {

    /// <summary>スクロール方向の位置 (実態へ反映)</summary>
    public float Position { get; set; }

    /// <summary>スクロール方向のサイズ (物理項目から反映)</summary>
    public float Size { get; set; }

    /// <summary>内容に変更があった</summary>
    public bool Dirty { get; set; }

    /// <summary>タイトル</summary>
    public string Title {
        get => _title;
        set {
            if (_title != value) {
                _title = value;
                Dirty = true;
            }
        }
    }
    private string _title;

    /// <summary>説明</summary>
    public string Description {
        get => _description;
        set {
            if (_description != value) {
                _description = value;
                Dirty = true;
            }
        }
    }
    private string _description;

    /// <summary>アイコン</summary>
    public Sprite Icon {
        get => _icon;
        set {
            if (_icon != value) {
                _icon = value;
                Dirty = true;
            }
        }
    }
    private Sprite _icon;

    /// <summary>チェックボックスのラベル</summary>
    public string Label {
        get => _label;
        set {
            if (_label != value) {
                _label = value;
                Dirty = true;
            }
        }
    }
    private string _label;

    /// <summary>チェックボックスの状態</summary>
    public bool Check {
        get => _check;
        set {
            if (_check != value) {
                _check = value;
                //Dirty = true;
            }
        }
    }
    private bool _check;

    /// <summary>コンストラクタ</summary>
    public Item (string title = "", string desc = "", Sprite icon = null, string label = "", bool check = false) : base () {
        _title = title;
        _description = desc;
        _icon = icon;
        _label = label;
        _check = check;
    }

    /// <summary>実体を生成</summary>
    /// <returns>生成したオブジェクトにアタッチされているコンポーネントを返す</returns>
    public InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index) => ItemComponent.Create (scrollRect, index);


    /// <summary>文字列化</summary>
    public override string ToString () => $"{base.ToString ()}({Position}, {Size}, {Dirty})";

}
