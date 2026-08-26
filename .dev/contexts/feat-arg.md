# feat/arg 対話コンテキスト

- PR: export時点でPR未作成
- Branch: `feat/arg`
- Source commit: 0d543c2
- Updated at: 2026-08-26 12:37:13
- Exported by: Claude Opus 4.6, Claude Opus 5

## 目的

マクロの文法を拡張して整数型の引数を受け取れるようにする。Waitの待機時間やHitコマンドの-cオプション（フレーム補正値）などを、マクロファイルを編集せずに実行時に調整できるようにすることが動機。

## 設計方針

### ORCA.Core / ORCA.Runtime

- プレースホルダ構文として`{name}`と`{name:default}`を導入する。対応する型は整数値のみ。名前は`[A-Za-z_][A-Za-z0-9_]*`、デフォルト値は`-?[0-9]+`。
- プレースホルダはコマンドの位置引数（`Hit A {frame}`）にもオプション値（`-c={correction}`）にも書ける。
- コンパイル時にパーサがプレースホルダを認識し、`MacroArg`型（リテラル値またはプレースホルダ参照を保持する判別共用体）としてコマンド内に保持する。解決は実行時に行う。この設計により、再コンパイルなしに異なる引数で再実行できる。
- `MacroArg`型はORCA.Coreに配置し、プラグインコマンドからも利用できるようにする。`int`からの暗黙変換を持たせ、リテラルを扱う既存の呼び出し箇所をそのまま書けるようにした。
- `MacroArg.TryParse`はプレースホルダを認識すると同時に`IMacroParserContext.DeclareParameter`でパラメータを宣言する。パーサ側がパラメータ収集を書き忘れても登録が漏れない。
- `MacroScript`は`Parameters`プロパティ（`IReadOnlyList<MacroParameter>`）でパラメータ一覧（名前・デフォルト値・負の可否）を出現順に公開する。呼び出し側はこれを使って入力UIやプロトコルの組み立てを行う。
- `RunOnceAsync`/`RunLoopAsync`に`IReadOnlyDictionary<string, int>`の省略可能な引数を追加し、コンテキスト経由で`MacroArg`を解決する。省略可能にしたのでGUI/Headlessの既存の呼び出しはそのままビルドが通る。
- `RunOnceAsync`/`RunLoopAsync`は`Task.Run`に入る前に引数を解決・検証する。値の不足などがあれば`ArgumentException`を同期的に投げる。
- 同名プレースホルダは複数箇所で使え、すべて同じ値で解決される。デフォルト値の指定が箇所ごとに食い違う場合はコンパイルエラーにする。片方だけに書かれたデフォルト値を採用する規則も検討したが、宣言の一致を要求するほうが規則として説明しやすく実装も短い。
- 負の値の検査は`MacroArg.TryParse`の`allowNegative`引数（既定false）で位置ごとに指定する。リテラル値・デフォルト値の検査はTryParse内で行われ、呼び出し側の`KnownValue < 0`検査は不要になったため`KnownValue`プロパティは削除した。制約は`DeclareParameter`経由で`MacroParameter.AllowsNegative`として収集され、実行時に渡された引数も`CreateContext`で同じ制約で検証される。
- 同名パラメータが負を許す位置（Hitの-c）と許さない位置の両方で使われる場合、1箇所でも許さない位置にあれば非負制約に揃える。宣言の出現順に依存せず同じ結果になる。
- `MacroArg.Resolve`は値が解決できない場合に`InvalidOperationException`を投げる。プレースホルダ側はデフォルト値を保持せず、デフォルト値の充填は`CreateContext`のみが行う。
- タイマーラベル（`-s`、`-l`）はプレースホルダ非対応で`int`のまま。コンパイル時にタイマーの起動状況とHit計画を追跡する必要があるため。
- 引数の検証エラーは`CreateContext`が一箇所で判定し、呼び出し側は例外を流すだけでよい形にする。検証は3種類（デフォルト値のないパラメータへの値の不足、非負制約の違反、マクロが宣言していない名前の指定）で、それぞれ該当する名前を全件集めてから、`; `で連結した1つの`ArgumentException`として投げる。呼び出し側が1回の修正で全ての問題を直せる。
- `CreateContext`が投げる`ArgumentException`に`paramName`を渡さない。渡すと.NETがメッセージ末尾に`(Parameter 'arguments')`を連結してしまい、そのままユーザーに表示される。
- マクロが宣言していない名前が渡された場合はエラーにする。黙って無視すると`--arg framee=100`のような打ち間違いがデフォルト値のまま実行され、実行時に値を調整するための機能として成立しない。

### ORCA.Headless

- CLIからの引数の渡し方は`--arg name=value`の繰り返しとする。`docker build --build-arg`と同じ記法。
- `--arg`のパースは`Service.TryParseArguments`に置く。`ParseLoop`と同様に`args`配列を走査する。`=`が無い、値が整数としてパースできない、`--arg`の後にトークンが無い、同じ名前を複数回指定した、のいずれもコマンドを拒否する。
- `MacroHistory.Entry`はその実行で渡された引数を`Dictionary<string, int>`として保持する。値を渡さなかった実行でも空の辞書を入れ、null判定を持ち込まない。
- `rerun`は履歴のエントリの引数を土台にして、`--arg`で名前を指定されたものだけを差し替えた新しいエントリを作る。指定しなかったパラメータは前回の実行で渡された値のままになる。
- `rerun`が積むエントリの`DryRun`は、土台にしたエントリの値ではなくその実行で決まった値にする。実行ごとに別のエントリを積む以上、各エントリはその実行が実際にどうだったかを表す必要があるため。`--dry-run`つきのrerunは`(dry-run)`として、`--no-dry-run`つきのrerunは本実行として履歴に残る。
- `MacroHistory`は同じマクロを同じ条件で実行しても実行ごとに別のエントリとして積む。渡した引数が実行ごとに違いうるため、`Entry`のカスタム`Equals`/`GetHashCode`と`Remember`内の`_entries.Remove`を削除した。
- `Remember`は`StartMacro`の成功後に呼ぶ。引数解決に失敗した実行はマクロが1行も動いていない点でコンパイルエラーと同じなので、履歴に残さない。残すと番号省略の`rerun`が実行されなかったエントリを土台にしてしまう。

## 却下した代替案

- テキストレベル置換（パース前に文字列置換）: 再実行時に引数だけ変えるには再コンパイルが必要になるため不採用。
- パース時解決（パーサ内で引数辞書から値を取得）: 同上の理由で不採用。
- `MacroScript.SetArguments`のような状態セッター方式: 外部から状態を注入する設計はミュータブルな状態を持ち込むため不採用。引数は`Run`メソッドのパラメータとして渡す。
- コマンドの前段に引数解決レイヤーを挟む方式: コマンドのimmutabilityを壊すか、毎回コマンドオブジェクトを再生成するコストがかかるため不採用。
- `MacroArg`に加算などの式を表現させ、Hitのframeと-cを1つの`MacroArg`に畳み込む方式: 式木を持ち込むことになるため不採用。frameとcorrectを別々に保持して実行時に加算する。
- CLIで`--frame=1234`のようにパラメータ名をそのままフラグにする方式: マクロ作者がパラメータ名を自由に付けられるため、`--loop`、`--dry-run`、`--json`、`--quiet`と衝突しうる。不採用。
- 引数の不足を`Service`側で`MacroScript.Parameters`と突き合わせて事前検査する方式: 不足の検査規則をCoreと2箇所に持つことになり、非負制約の違反は結局`CreateContext`の例外経路で報告されるためエラー表示の形式も揃わない。`CreateContext`側を直せばGUIを含む全呼び出し側で一度に解消する。

## 意図的に対応しないこと

- 整数以外の型（文字列、ボタン名など）のプレースホルダ対応。用途が整数値の調整に限られるため、対応する予定がない。
- ORCA.GUIの対応。このブランチではGUIには手を入れない。
- 壊れたプレースホルダ記法（`{x`、`{a-b}`など）に対する専用のエラーメッセージ。プレースホルダとして認識されず整数としてもパースできないため、既存の「32bit符号あり整数に収まる…」というメッセージが出る。
- `history`の表示にその実行で渡した引数を出すこと。指示された仕様では`rerun`は番号を省略すれば直近のエントリを土台にし、引数の調整はそこに`--arg`を重ねて進めるため、同じラベルのエントリを番号で選び分ける操作が出てこない。必要になってから足す。

## 発見された制約

- 既存のコマンドパーサ（Hit, Wait, Press, Start）はint引数を直接保持しており、`MacroArg`型への差し替えが必要になる。パーサの`Parse`メソッドとコマンドクラスのコンストラクタの両方に変更が入る。
- プラグインパーサのインターフェース`IMacroCommandParser<T>`はORCA.Coreで定義されているため、`MacroArg`もORCA.Coreに置く必要がある。
- ORCA.CoreとORCA.Runtimeは`netstandard2.0`もターゲットにしているため、`record`など`IsExternalInit`を要求する構文は使えない。`MacroParameter`は素のクラスにした。
- Hit計画（`GetRemainingFrame()`が参照する`(label, frame)`の配列）はコンパイル時に`frame + correct`を畳み込んで作られていたが、どちらもプレースホルダになりうるため畳み込めなくなった。計画は`MacroArg`のまま保持し、実行開始時に解決した配列を作る。
- Hit計画は該当のHitコマンドが実行される前に参照される（`Start`で`_hitIndex`が0になった直後に`GetRemainingFrame()`が呼ばれる）ため、実行時にHitコマンド自身が計画を通知する方式は成立しない。
- `IMacroParserContext.AddHitPlan`の引数がプラグイン公開APIであるため、`(int label, MacroArg frame, MacroArg correct = default)`という省略可能引数の形にして`AddHitPlan(label, frame)`というソースをそのまま通るようにした。
- `MacroHistory`の重複排除をやめたことで、既存テストの期待値が変わった。`ServiceTests`の`rerunコマンド_指定された履歴のマクロを実行すること`は`["1: a.txt", "2: b.txt", "3: a.txt"]`に更新した。`MacroHistoryTests`の`Entryの同一性_*`3件はカスタム`Equals`の削除で意味を失うため削除し、`履歴にあるエントリと同一のエントリをRememberすると先頭に積み直されること`は`同じラベルと条件のエントリをRememberしても実行ごとに別のエントリとして積まれること`に置き換えた。
- `ServiceTests`から`RecordingPort`で観測できるのは書き込まれたバイト列だけで、経過時間は記録されない。引数の値が実際にコマンドへ届いていることは、値の内容によって成否が変わる経路（非負制約の違反）で確かめている。値の置換そのものの検証は`ORCA.Tests/ArgumentTests`の`RecordArgParser`側にある。

## 新たに確認できた事実

- 現在のマクロシステムはGUI（Form1.cs）とHeadless（Service.cs）の両方から使われており、コアAPIの設計はどちらにも偏らない形にする必要がある。
- `IMacroContext`は実行時コンテキストとしてタイマー操作やキー・バリューストアを提供しており、引数の解決もここを経由させるのが自然。`GetArgument(string): int?`を追加した。
- `frame + correct`の0による下限クランプはHitコマンドの実行とHit計画の解決の両方で必要になる。`HitCommand.ResolveFrame`という内部staticに集約し、`MacroScript`の計画解決からも呼んでいる。
- `MacroScript.Compile`はパーサ辞書を引数に取るため、テストからテスト専用のコマンドとパーサを差し込める。`arg.Resolve(context)`の結果をそのまま記録するコマンドを使えば、引数解決の検証を実時間に依存せず書ける（`ORCA.Tests/Helpers/RecordArgParser.cs`）。
- ORCA.GUIは`dotnet build`だと`.resx`の非文字列リソースでMSB3823/MSB3822が出て失敗する。この変更の前から同じなので、GUIのビルド確認にはVisual Studioのmsbuildが要る。
- `--arg correct=-3`と書いても`correct=-3`というトークンの先頭は`-`にならないため、`rerun`の`args[0].StartsWith('-')`や`connect`の同様の判定を壊さない。
- `Service.Handle`は全コマンドをtry-catchで包み、例外を`error: {ex.Message}`として返す。`CreateContext`から`paramName`を外したことで`(Parameter 'arguments')`の連結は消え、不足も全件まとめて報告されるようになった。
- `--arg`のトークンは`ParseLoop`の走査と干渉しない。`--loop --arg frame=1`と書いた場合、`--loop`の次のトークン`--arg`は整数としてパースできないので回数省略（無限ループ）と解釈される。
- `Program.cs`のクライアント側は`--json`と`--quiet`だけを除去して残りをそのまま送るため、`--arg`とその値は変更なしでデーモンに届く。ただし`run`は`rest[0]`をマクロファイルのパスとして読むため、`--arg`はパスより後ろに書く必要がある。これは`--dry-run`など既存のオプションと同じ制約。

## 注意が必要な難所

- 補正値の下限クランプは押下タイミングでは検出できない。クランプが無い場合の`border`は負になり、`CancelableWait`は負の待機時間でも即座に戻るため、クランプの有無で押下時刻が変わらない。`GetRemainingFrame()`の値（クランプ有りなら0付近、無しなら-90F付近）で検証する必要がある。
- xUnitはテストクラスを並列実行するため、実時間を測定するテストクラスが複数あると互いにCPUを奪い合って測定値がぶれる。`ORCA.Tests`では`Timing` collection（`DisableParallelization = true`）を定義し、`TimingTests`と`ArgumentTests`をそこに入れて直列化した。これを入れる前は全体実行で6件失敗していた。
- テストクラスは検証の観点で分ける。実時間を測るかどうかという検証方法でクラスを分けてはいけない。引数に関する観点は、実時間を測るものも含めて`ArgumentTests`に置く。
- `TimingTests.Hitコマンドの押下間隔が指定フレームの間隔と一致すること`は30F間隔を±10msで検証しており、実行環境の負荷で失敗することがある。この変更の前から同じ。
- `ORCA.Headless.Tests`では`service.Handle`を直接呼ぶとxUnit1051の警告が出る。`clientGone: TestContext.Current.CancellationToken`を渡す既存の書き方に合わせる。

## 残作業

なし。ORCA.Headlessへの組み込みまで完了し、`ORCA.Tests`（65件）と`ORCA.Headless.Tests`（61件）はいずれも全件成功する。ORCA.GUIは方針どおり手を入れていない。
