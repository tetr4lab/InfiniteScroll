using System;
using System.Collections;
using System.Collections.Generic;

namespace InfiniteScroll {

    /// <summary>
    /// 論理項目(抽象クラス)
    ///   継承したクラスを用意して、スクロールレクトの初期化に使用する
    ///   論理項目のリストの一部が物理項目に反映される
    /// </summary>
    public abstract class InfiniteScrollItemBase {

        /// <summary>
        /// 物理項目を生成
        ///   GameObjectを生成して、InfiniteScrollItemComponentを継承したコンポーネントをアタッチする
        /// </summary>
        /// <returns>生成したGameObjectにアタッチされているコンポーネントを返す</returns>
        public virtual InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index) {
            var component = InfiniteScrollItemComponentBase.Create (scrollRect, index);
            return component;
        }

        /// <summary>スクロール方向の位置 (実態へ反映)</summary>
        public virtual float Position { get; protected internal set; }

        /// <summary>スクロール方向のサイズ (物理項目から反映)</summary>
        public virtual float Size { get; protected internal set; }

        /// <summary>内容に変更があった</summary>
        public virtual bool Dirty { get; protected internal set; }

        /// <summary>コンストラクタ</summary>
        public InfiniteScrollItemBase () { }

        /// <summary>文字列化</summary>
        public override string ToString () => $"{base.ToString ()}({Position}, {Size}, {Dirty})";

    }

}
