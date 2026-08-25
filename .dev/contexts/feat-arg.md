# feat/arg 対話コンテキスト

- PR: export時点でPR未作成
- Branch: `feat/arg`
- Source commit: 1bdadae
- Updated at: 2026-08-25 17:18:56
- Exported by: Claude Opus 4.6, Claude Opus 5

## 目的

マクロの文法を拡張して整数型の引数を受け取れるようにする。Waitの待機時間やHitコマンドの-cオプション（フレーム補正値）などを、マクロファイルを編集せずに実行時に調整できるようにすることが動機。

## 設計方針

- プレースホルダ構文として`{name}`と`{name:default}`を導入する。対応する型は整数値のみ。名前は`[A-Za-z_][A-Za-z0-9_]*`、デフォルト値は`-?[0-9]+`。
- プレースホルダはコマンドの位置引数（`Hit A {frame}`）にもオプション値（`-c={correction}`）にも書ける。
- コンパイル時にパーサがプレースホルダを認識し、`MacroArg`型（リテラル値またはプレースホルダ参照を保持する判別共用体）としてコマンド内に保持する。解決は実行時に行う。この設計により、再コンパイルなしに異なる引数で再実行できる。
- `MacroArg`型はORCA.Coreに配置し、プラグインコマンドからも利用できるようにする。`int`からの暗黙変換を持たせ、リテラルを扱う既存の呼び出し箇所をそのまま書けるようにした。
- `MacroArg.TryParse`はプレースホルダを認識すると同時に`IMacroParserContext.DeclareParameter`でパラメータを宣言する。パーサ側がパラメータ収集を書き忘れても登録が漏れない。
- `MacroScript`は`Parameters`プロパティ（`IReadOnlyList<MacroParameter>`）でパラメータ一覧（名前・デフォルト値）を出現順に公開する。呼び出し側はこれを使って入力UIやプロトコルの組み立てを行う。
- `RunOnceAsync`/`RunLoopAsync`に`IReadOnlyDictionary<string, int>`の省略可能な引数を追加し、コンテキスト経由で`MacroArg`を解決する。省略可能にしたのでGUI/Headlessの既存の呼び出しはそのままビルドが通る。
- `RunOnceAsync`/`RunLoopAsync`は`Task.Run`に入る前に引数を解決・検証する。デフォルト値のないパラメータに値が渡されていなければ`ArgumentException`を同期的に投げる。
- 同名プレースホルダは複数箇所で使え、すべて同じ値で解決される。デフォルト値の指定が箇所ごとに食い違う場合はコンパイルエラーにする。片方だけに書かれたデフォルト値を採用する規則も検討したが、宣言の一致を要求するほうが規則として説明しやすく実装も短い。
- 負の値の検査は`MacroArg.TryParse`の`allowNegative`引数（既定false）で位置ごとに指定する。リテラル値・デフォルト値の検査はTryParse内で行われ、呼び出し側の`KnownValue < 0`検査は不要になったため`KnownValue`プロパティは削除した。制約は`DeclareParameter`経由で`MacroParameter.AllowsNegative`として収集され、実行時に渡された引数も`CreateContext`で同じ制約で検証される。
- 同名パラメータが負を許す位置（Hitの-c）と許さない位置の両方で使われる場合、1箇所でも許さない位置にあれば非負制約に揃える。宣言の出現順に依存せず同じ結果になる。
- `MacroArg.Resolve`は値が解決できない場合に`InvalidOperationException`を投げる。プレースホルダ側はデフォルト値を保持せず、デフォルト値の充填は`CreateContext`のみが行う。
- タイマーラベル（`-s`、`-l`）はプレースホルダ非対応で`int`のまま。コンパイル時にタイマーの起動状況とHit計画を追跡する必要があるため。

## 却下した代替案

- テキストレベル置換（パース前に文字列置換）: 再実行時に引数だけ変えるには再コンパイルが必要になるため不採用。
- パース時解決（パーサ内で引数辞書から値を取得）: 同上の理由で不採用。
- `MacroScript.SetArguments`のような状態セッター方式: 外部から状態を注入する設計はミュータブルな状態を持ち込むため不採用。引数は`Run`メソッドのパラメータとして渡す。
- コマンドの前段に引数解決レイヤーを挟む方式: コマンドのimmutabilityを壊すか、毎回コマンドオブジェクトを再生成するコストがかかるため不採用。
- `MacroArg`に加算などの式を表現させ、Hitのframeと-cを1つの`MacroArg`に畳み込む方式: 式木を持ち込むことになるため不採用。frameとcorrectを別々に保持して実行時に加算する。

## 意図的に対応しないこと

- 整数以外の型（文字列、ボタン名など）のプレースホルダ対応。用途が整数値の調整に限られるため、対応する予定がない。
- GUI/Headlessでの引数入力UIの設計。コアAPIのみを今回のスコープとする。
- 壊れたプレースホルダ記法（`{x`、`{a-b}`など）に対する専用のエラーメッセージ。プレースホルダとして認識されず整数としてもパースできないため、既存の「32bit符号あり整数に収まる…」というメッセージが出る。

## 発見された制約

- 既存のコマンドパーサ（Hit, Wait, Press, Start）はint引数を直接保持しており、`MacroArg`型への差し替えが必要になる。パーサの`Parse`メソッドとコマンドクラスのコンストラクタの両方に変更が入る。
- プラグインパーサのインターフェース`IMacroCommandParser<T>`はORCA.Coreで定義されているため、`MacroArg`もORCA.Coreに置く必要がある。
- ORCA.CoreとORCA.Runtimeは`netstandard2.0`もターゲットにしているため、`record`など`IsExternalInit`を要求する構文は使えない。`MacroParameter`は素のクラスにした。
- Hit計画（`GetRemainingFrame()`が参照する`(label, frame)`の配列）はコンパイル時に`frame + correct`を畳み込んで作られていたが、どちらもプレースホルダになりうるため畳み込めなくなった。計画は`MacroArg`のまま保持し、実行開始時に解決した配列を作る。
- Hit計画は該当のHitコマンドが実行される前に参照される（`Start`で`_hitIndex`が0になった直後に`GetRemainingFrame()`が呼ばれる）ため、実行時にHitコマンド自身が計画を通知する方式は成立しない。
- `IMacroParserContext.AddHitPlan`の引数がプラグイン公開APIであるため、`(int label, MacroArg frame, MacroArg correct = default)`という省略可能引数の形にして`AddHitPlan(label, frame)`というソースをそのまま通るようにした。

## 新たに確認できた事実

- 現在のマクロシステムはGUI（Form1.cs）とHeadless（Service.cs）の両方から使われており、コアAPIの設計はどちらにも偏らない形にする必要がある。
- `IMacroContext`は実行時コンテキストとしてタイマー操作やキー・バリューストアを提供しており、引数の解決もここを経由させるのが自然。`GetArgument(string): int?`を追加した。
- `frame + correct`の0による下限クランプはHitコマンドの実行とHit計画の解決の両方で必要になる。`HitCommand.ResolveFrame`という内部staticに集約し、`MacroScript`の計画解決からも呼んでいる。
- `MacroScript.Compile`はパーサ辞書を引数に取るため、テストからテスト専用のコマンドとパーサを差し込める。`arg.Resolve(context)`の結果をそのまま記録するコマンドを使えば、引数解決の検証を実時間に依存せず書ける（`ORCA.Tests/Helpers/RecordArgParser.cs`）。
- ORCA.GUIは`dotnet build`だと`.resx`の非文字列リソースでMSB3823/MSB3822が出て失敗する。この変更の前から同じなので、GUIのビルド確認にはVisual Studioのmsbuildが要る。

## 注意が必要な難所

- 補正値の下限クランプは押下タイミングでは検出できない。クランプが無い場合の`border`は負になり、`CancelableWait`は負の待機時間でも即座に戻るため、クランプの有無で押下時刻が変わらない。`GetRemainingFrame()`の値（クランプ有りなら0付近、無しなら-90F付近）で検証する必要がある。
- xUnitはテストクラスを並列実行するため、実時間を測定するテストクラスが複数あると互いにCPUを奪い合って測定値がぶれる。`ORCA.Tests`では`Timing` collection（`DisableParallelization = true`）を定義し、`TimingTests`と`ArgumentTests`をそこに入れて直列化した。これを入れる前は全体実行で6件失敗していた。
- テストクラスは検証の観点で分ける。実時間を測るかどうかという検証方法でクラスを分けてはいけない。引数に関する観点は、実時間を測るものも含めて`ArgumentTests`に置く。

## 修正要求（対応済み・再評価で確認）

- `MacroArg.Resolve`のフォールバック廃止: 対応済み。`GetArgument`がnullを返すと`InvalidOperationException`を投げる。デフォルト値の充填は`CreateContext`に一本化された。
- 実行時引数の検証: 対応済み。`TryParse`の`allowNegative`で位置ごとの制約を宣言時に収集し、`CreateContext`でリテラル・デフォルト値と同じ制約を適用する。テストも追加されている。

再評価の結果、追加の修正要求はない。テストは全63件成功。

## 残作業

- GUI/Headlessの呼び出し側の対応（`MacroScript.Parameters`を使った引数入力UI、Headlessのプロトコルへの引数の追加）
