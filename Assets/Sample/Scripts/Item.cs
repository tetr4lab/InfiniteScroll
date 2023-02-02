using InfiniteScroll;
using UnityEngine;
using UnityEngine.UI;

/// <summary>論理アイテム</summary>
public class Item : InfiniteScrollItemBase {

    /// <summary>タイトル</summary>
    public string Title { get; protected set; }

    /// <summary>説明</summary>
    public string Description { get; protected set; }

    /// <summary>アイコン</summary>
    public Sprite Icon { get; protected set; }

    /// <summary>チェックボックスのラベル</summary>
    public string Label { get; protected set; }

    /// <summary>チェックボックスの状態</summary>
    public bool Check { get; set; }
    
    /// <summary>サイズ</summary>
    public override Vector2 Size {
        get;
        protected internal set;
    }

    /// <summary>コンストラクタ</summary>
    public Item (string title = "", string desc = "", Sprite icon = null, string label = "", bool check = false) {
        Title = title;
        Description = desc;
        Icon = icon;
        Label = label;
        Check = check;
    }

    /// <summary>実体を生成</summary>
    /// <returns>生成したオブジェクトにアタッチされているコンポーネントを返す</returns>
    public override InfiniteScrollItemComponentBase Create (Transform parent, InfiniteScrollRect scrollRect) {
        var component = ItemComponent.Create (parent);
        component.ScrollRect = scrollRect;
        component.Item = this;
        component.Title = Title;
        component.Description = Description;
        component.Icon = Icon;
        component.CheckBoxLabel = Label;
        component.Check = Check;
        return component;
    }

}
