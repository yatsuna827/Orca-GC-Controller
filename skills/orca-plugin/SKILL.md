---
name: orca-plugin
description: ORCAプラグインの作成・編集ガイド。「プラグインを作りたい」「カスタムコマンドを追加したい」「MacroCommandを実装したい」「PluginのDLLを作りたい」のようにプラグイン開発に関する話になったら使う。
---

# ORCAプラグイン開発ガイド

ORCAプラグインは、マクロスクリプトの組み込みコマンド（Press, Wait, Start, Hit）では表現できない処理をC#で書いてカスタムコマンドとして追加する仕組み。ビルドしたDLLをアプリの`Plugin/`フォルダに置くと、起動時に`Assembly.LoadFrom`で読み込まれる。

## 事前条件

`ORCA.Core`と`ArduinoAPI`のNuGetパッケージがローカルのNuGetソースに登録されていること。登録されていない場合はユーザーに確認して、作業を進めない。

## プロジェクトの作り方

.NET Framework 4.7.2以上のクラスライブラリを作り、NuGetで`ORCA.Core`を参照する。

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ORCA.Core" Version="1.0.0" />
  </ItemGroup>
</Project>
```

`ORCA.Core`は`ArduinoAPI`に依存しているので、`ORCA.Core`だけ参照すれば`ArduinoAPI`の型も使える。

## 実装する型

プラグインには2つのクラスが必要になる。

### 1. コマンド本体（MacroCommandの派生クラス）

`Execute`メソッドにコマンドの処理を書く。

```csharp
public class MyCommand : MacroCommand
{
    public string SomeArg { get; set; }

    public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
    {
        // port: コントローラへの出力。PressButton()やSetButtonState()で操作する
        // token: マクロ停止時にキャンセルされる
        // context: タイマー操作、Wait、コマンド間の変数共有に使う
    }
}
```

### 2. パーサー（IMacroCommandParser<T>の実装クラス）

マクロスクリプトの引数文字列を受け取ってコマンドインスタンスを返す。`[MacroCommand]`属性でコマンド名を指定する。

```csharp
[MacroCommand(commandName: "MyCmd")]
public class MyCmdParser : IMacroCommandParser<MyCommand>
{
    public MyCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
    {
        errorMessage = "";
        return new MyCommand { SomeArg = args.Length > 0 ? args[0] : "" };
    }
}
```

マクロスクリプトからは `MyCmd arg1 arg2` のように呼び出せる。

### パーサーの制約

PluginLoaderが型を検出する条件は以下のとおり。

- `IMacroCommandParser<T>`を実装していること
- `[MacroCommand(commandName: "...")]`属性が付いていること
- 具象クラスであること（abstractやinterfaceは不可）
- ジェネリック型引数を持たないこと（`class Foo<T>`は不可）
- 引数なしのpublicコンストラクタがあること

## 使える主なAPI

### IWritable（port引数）の拡張メソッド

```csharp
// ボタンを押して離す。wait_msの間押し続ける（デフォルト200ms）
port.PressButton(ControllerInput.A);
port.PressButton(ControllerInput.A, wait_ms: 1000);

// 同時押し
port.PressButton(ControllerInput.Start | ControllerInput.Y, 4000);

// ボタン状態を直接セットする（押しっぱなしにしたい場合）
port.SetButtonState(ControllerInput.Left);
port.SetButtonState(ControllerInput.KeysAllUp); // 全部離す
```

### ControllerInput

`A`, `B`, `X`, `Y`, `Z`, `L`, `R`, `Start`, `Up`, `Down`, `Left`, `Right`, `KeysAllUp` がある。ビットフラグなので`|`で同時押しを表現できる。

### IMacroContext（context引数）

```csharp
// 待機
context.Wait(2000, token);

// タイマー
context.StartTimer();
context.StartTimer(label: 1);

// コマンド間で共有する変数
context.SetIntContext("key", 42);
int? val = context.GetIntContext("key");

context.SetStringContext("key", "value");
string s = context.GetStringContext("key");

context.SetObjectContext("key", obj);
object o = context.GetObjectContext("key");

// マクロ実行時に渡された引数
int? arg = context.GetArgument("name");
```

### IMacroParserContext（Parse時のcontext引数）

```csharp
// マクロの現在行
int line = context.CurrentLine;

// マクロ引数（プレースホルダ）の宣言
context.DeclareParameter("frame", defaultValue: null, allowsNegative: false);

// タイマー関連
context.TimerStarted(label: 0);
context.SetTimerStarted(label: 0);
context.AddHitPlan(label: 0, frame: 100);
```

## ビルドとデプロイ

ビルドしたDLLをアプリの`Plugin/`フォルダにコピーする。ORCA.CoreやArduinoAPIのDLLはホスト側が持っているので、プラグイン本体のDLLだけ置けばよい。

## 注意点

- `Execute`は同期メソッドなので、非同期処理を呼ぶ場合は`.GetAwaiter().GetResult()`で待つ。マクロ実行は専用スレッドで動いているのでデッドロックの心配はない。ただし`Wait`は`Stopwatch`ベースで同じスレッド上で計測しているため、HTTPリクエストなどでスレッドをブロックした時間はマクロ全体の進行が止まる。タイミングがシビアな操作（Hit前後など）とブロッキング呼び出しを混在させないように注意する。
- コマンド名にPress, Wait, Start, Hitは使えない（組み込みコマンドと衝突する）。
- strong nameは付いていないので厳密なバージョン照合は走らないが、CoreのAPIシグネチャが変わった場合、プラグインDLLは互換性がなくなる。
