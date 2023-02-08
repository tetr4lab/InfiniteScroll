---
title: Unity シンプルな無限スクロール (uGUI)
tags: Unity C# uGUI
---
# はじめに
- 「無限」と銘打っていますが、次のような使い方はできません。
  - リストの上端と下端をループさせて、有限長のリストで無限にスクロールする。
  - 動的に生成される不定長のリストを自動的にスクロールする。
- 次のような使い方を想定しています。
  - `UnityEngine.UI.ScrollRect`(uGUI)の代替として`Content`内に`GameObject`を動的に生成する。
  - スクロール方向に対して、リストの前方にこれから表示される項目を追加し、表示の済んだリストの後方の項目を削除するなど、リストを動的に増減する。
  - 固定長だが長大なリストを扱う際に、可視範囲外の`GameObject`を節約する。

## テスト環境
- Unity 2021.3.18f1
- Windows 11

## できること
- リスト(`IEnumerable<InfiniteScrollItemBase>`)の登録と初期化
  - 縦スクロール、または、横スクロール
  - 項目に対するパディングとスペーシング
  - スクロールに直行する方向の項目制御(下/左寄せ、中央寄せ、上/右寄せ、拡縮Fit)
  - 項目の並び順の反転
- スクロール方向に可変長の項目サイズ
- 項目リスト(`List<InfiniteScrollItemBase>`)への任意の操作
- `UnityEngine.UI.ScrollRect`の主要な機能

## 概念
- スクロールレクト
  - 矩形のスクロール領域です。
  - 縦、または、横に項目が並び、スクロールします。
  - 可変長の項目を許容します。
- 項目
  - 物理項目
    - スクロールレクトに縦または横に並んだ表示体です。
    - 物理項目は見える範囲にしか作られず、範囲外になった項目は不活性にされ、必要に応じて再利用されます。
    - 活性中の物理項目には、紐付けられている論理項目の内容が反映されます。
    - 一般に、プレファブとその制御クラスとして実装されます。
  - 論理項目
    - 物理項目に表示される値を保持するクラスです。
- リスト
  - 論理項目の集合で、並び順を持ちます。
  - 配列や`List`(コレクション)として実装されます。

# 導入
- アセットの本体は`Assets/InfiniteScroll/`にあります。
  - フォルダを移動しても支障ありません。
  - フォルダの中は不用意に触らないようにしてください。
- `UI/Scroll View`の代わりに`UI/InfiniteScroll View`を、`UI/ScrollRect`の代わりに`UI/InfiniteScrollRect`を使用します。
  - 必要に応じて、`ScrollRect`と共通の設定に加えて、`Padding`、`Spacing`、`Child Alignment`、`Reverse Arrangement`、`Control Child Size`を設定してください。
  - `Content`には何も置かないでください。
  - スクロール方向は縦/横のどちらかしか選べません。
- クラス`InfiniteScrollItemBase`を継承したクラスを用意してください。
  - `Create ()`を`override`するメソッドの他に、コンストラクタを実装してください。
- クラス`InfiniteScrollItemComponentBase`を継承したクラスを用意してください。
  - `static Create ()`を置き換える(`new`)メソッドを実装してください。
    - このメソッドで項目の実体を生成するために、必要に応じて、クラスをアタッチしたプレファブを用意してください。
  - `Initialize ()`、`Apply ()`を`override`するメソッドの他に、`Item`プロパティを実装してください。

# 使い方
- `InfiniteScrollRect`コンポーネントの`Initialize`に、`InfiniteScrollItemBase`を継承したクラスの配列またはリストを渡します。
  - より具体的な使い方は、サンプルアセットを参照してください。
- フォルダ構造
  - Sample/
    - Resouces/
      - Prefabs/
        - Item.prefab: 項目のプレファブ
    - Scenes/
      - InfiniteScrollTest.unity: サンプルシーン
    - Scripts/
      - InfiniteScrollTest.cs: サンプルメイン
      - Item.cs: `InfiniteScrollItemBase`を継承した論理項目クラス
      - ItemComponent.cs: `InfiniteScrollItemComponentBase`を継承した物理項目クラス

# 主なAPI
## `class InfiniteScrollRect`
- `UnityEngine.UI.ScrollRect`を継承したコンポーネント・クラスです。

### フィールド
#### `RectOffset m_padding`
- `Content`と項目の間の隙間のサイズです。
- インスペクタで設定可能な項目で、初期化の際に使われます。
  - 動的な変更後には再初期化が必要になります。

#### `float m_spacing`
- 項目間の隙間のサイズです。
- インスペクタで設定可能な項目で、初期化の際に使われます。
  - 動的な変更後には再初期化が必要になります。

#### `TextAnchor m_childAlignment`
- スクロールに直行する方向の項目の整列制御です。
  - 下/左寄せ、中央寄せ、上/右寄せ
  - `m_controlChildSize`が真の時は効果がありません。
- インスペクタで設定可能な項目で、初期化の際に使われます。
  - 動的な変更後には再初期化が必要になります。

#### `bool m_reverseArrangement`
- 項目の並び順を逆にします。
  - 偽だと、上から下、左から右になります。
  - 真だと、下から上、右から左になります。
- インスペクタで設定可能な項目で、初期化の際に使われます。
  - 動的な変更後には再初期化が必要になります。

#### `bool m_controlChildSize`
- スクロールに直行する方向の項目の拡大制御です。
  - 真だと、項目が幅いっぱいに拡大されて、`m_childAlignment`の設定は無効になります。
- インスペクタで設定可能な項目で、初期化の際に使われます。
  - 動的な変更後には再初期化が必要になります。

### プロパティ
#### `bool Valid`
- スクロールレクトが有効に初期化されていれば真です。

#### `InfiniteScrollItemBase this [int index]`
- 論理項目リストにアクセスするインデクサです。

#### `int Count`
- 論理項目の数です。

#### `int FirstIndex`
- 表示中の最初の論理項目のインデックスです。

#### `int LastIndex`
- 表示中の最後の論理項目のインデックスです。

### メソッド
#### `void Initialize (IEnumerable<InfiniteScrollItemBase> items, int index = 0)`
- 論理項目のリスト`items`を渡してスクロールレクトを初期化します。
  - `index`は最初に表示する項目のインデックスです。
- インスペクタで設定可能なシリアライズ・フィールドは、あらかじめ設定しておく必要があります。
  - シリアライズフィールドの変更後には再初期化が必要になります。

#### `ReadOnlyCollection<InfiniteScrollItemBase> AsReadOnly ()`
- 内部の論理項目リストにアクセスします。

#### `List<InfiniteScrollItemBase> FindAll (Predicate<InfiniteScrollItemBase> match)`
- 条件に合う論理項目を抽出したリストを返します。

#### `List<TOutput> ConvertAll<TOutput> (Converter<InfiniteScrollItemBase, TOutput> converter)`
- 論理項目を変換したリストを返します。

#### `void Clear ()`
- 全項目を抹消します。

#### `void Modify (Action<InfiniteScrollRect, List<InfiniteScrollItemBase>, int, int> action)`
- 項目リストの書き換えを行います。
- `action`には、スクロールレクト、論理項目のリスト、表示中の最初の論理項目のインデックス、表示中の最後の論理項目のインデックスが渡されます。
- 論理項目のリストを自在に更新することができますが、表示中の項目の削除など、変更の内容によってはスクロールが生じます。

## `class InfiniteScrollItemBase`
- 論理アイテムのベースとなる抽象クラスです。
- このクラスを継承したクラスを作成して、論理項目リスト`List<InfiniteScrollItemBase>`として使用します。
  - `Create ()`の`override`は必須で、他にコンストラクタを実装する必要があります。

### プロパティ
#### `float Position`
- `Content`上に配置される際のスクロール方向のオフセットです。

#### `float Size`
- `Content`上に配置される際のスクロール方向のサイズです。

#### `bool Dirty`
- 上記のオフセットとサイズを除く論理項目の内容に変更があって、物理項目に反映する必要が生じていることを表します。
- 物理項目側で、このフラグを監視して内容の取得と反映を行います。

### メソッド
#### `InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index)`
- 親のスクロールレクトと論理項目リストのインデックスを受け取って、この論理項目をスクロールレクトに表示するための物理項目を生成するメソッドで、`override`が必要です。
  - 実際の生成は、物理項目のクラスに委ねて、このメソッドは受け渡しをするだけです。
- 物理項目は、渡されたインデックスを用いて論理項目にアクセスします。

## `class InfiniteScrollItemComponentBase`
- 物理アイテムのベースとなる抽象クラスです。
- このクラスを継承したクラスを作成して、物理項目の`GameObject`にアタッチして使用します。
  - `static Create ()`の置き換え(`new`)は必須です。置き換えが行われていない場合は、実行時に例外を投げます。
  - 独自の情報を扱うなら、`Initialize ()`、`Apply ()`の`override`は必須で、他に`Item`プロパティの実装も必要です。

### プロパティ
#### `Item`
- リンク中の論理項目にアクセスするためのプロパティで、置き換え(`new`)が必要です。

### メソッド
#### `static InfiniteScrollItemComponentBase Create (InfiniteScrollRect scrollRect, int index)`
- 論理項目から呼ばれて物理項目を生成するメソッドで、置き換え(`new`)が必要です。
- このメソッドは継承したクラスでの置き換えを前提としたもので、参考のために実装されています。

#### `void Initialize ()`
- 生成直後に初期化するためのメソッドで`override`が必要です。

#### `void Apply ()`
- 生成直後、および、論理項目の内容に変更があった(`Dirty`)ときに、内容を反映するためのメソッドで、`override`が必要です。
