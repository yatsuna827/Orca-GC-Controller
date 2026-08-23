# ORCA.Headless

GCコントローラのマクロを実行するCLIアプリケーションです。
名前付きパイプを介したデーモン/クライアント構成で動作します。

## 使い方

### デーモンの起動

```
orca daemon [--port <シリアルポート名>]
```

`--port` を指定すると、そのポートがデフォルトの接続先になります（起動後にクライアントから変更可能です）。

### コマンド一覧

| コマンド | 説明 |
|---|---|
| `orca ports [--json]` | 利用可能なシリアルポートを一覧表示 |
| `orca connect [port] [--no-rts] [--no-dtr] [--json]` | シリアルポートに接続 |
| `orca disconnect [--json]` | 接続を切断 |
| `orca set-port <port> [--json]` | デフォルトポートを設定 |
| `orca run <path> [--loop [count]] [--dry-run] [--json] [--quiet]` | マクロファイルを実行 |
| `orca rerun [number] [--loop [count]] [--no-dry-run] [--json] [--quiet]` | 履歴からマクロを再実行 |
| `orca status [--json]` | マクロの実行状態を表示 |
| `orca history [--json]` | マクロ実行履歴を表示 |
| `orca shutdown [--json]` | デーモンを停止 |

### オプション

- `--json`: レスポンスをJSON形式で出力します。
- `--quiet`: 実行中の進捗表示を抑制します。
- `--dry-run`: ポートに接続せず、マクロの動作だけ確認します。
- `--loop [count]`: マクロをループ実行します。countを省略すると無限ループになります。

### 使用例

```bash
# デーモンを起動
orca daemon --port COM3

# 別ターミナルから操作
orca connect
orca run macro.txt
orca run macro.txt --loop 5
orca run macro.txt --dry-run
orca rerun                    # `macro.txt --dry-run` が実行される
orca rerun --no-dry-run       # `macro.txt` が本実行される
orca shutdown
```

実行中のマクロはCtrl+Cでキャンセルできます。

## プラグイン

実行ファイルと同じ階層の `Plugin/` ディレクトリの中に、プラグインを実装したDLLを配置すると、プラグインコマンドとして認識されます。
読み込まれたプラグインはデーモン起動時にログに表示されます。
