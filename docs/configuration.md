# ランチャー設定仕様

## 配置と文字コード

既定の設定ファイル名は実行ファイルと同じディレクトリの `launcher.config.xml` です。XML 宣言を付け、UTF-8 で保存してください。現在対応する `version` は `1` だけです。

相対パスは設定ファイルがあるディレクトリを基準に解決します。`executable`、`workingDirectory`、`image` では `%WINDIR%` のような環境変数を利用できます。DTD と外部エンティティは安全のため使用できません。設定ファイルは 1 MiB 以下にしてください。

## XML 構造

```xml
<launcher title="業務ランチャー" version="1">
  <categories>
    <category id="daily" name="日常業務" order="10">
      <button id="calculator" name="電卓" order="10"
              executable="%WINDIR%\System32\calc.exe"
              arguments="" workingDirectory="%WINDIR%\System32"
              image="images\calculator.png" />
    </category>
  </categories>
</launcher>
```

完全な定義は `schemas/launcher-config.xsd`、動作例は `samples/launcher.config.xml` を参照してください。

## 属性

| 要素 | 属性 | 必須 | 内容 |
|---|---|---:|---|
| `launcher` | `title` | はい | ウィンドウに表示する空でない名称 |
| `launcher` | `version` | はい | 現在は `1` 固定 |
| `category` | `id` / `name` / `order` | はい | 一意な ID、表示名、32 bit 整数の表示順 |
| `button` | `id` / `name` / `order` | はい | 一意な ID、表示名、32 bit 整数の表示順 |
| `button` | `executable` | はい | 起動対象の絶対パスまたは相対パス |
| `button` | `arguments` | いいえ | 起動時に渡す引数。既定値は空文字 |
| `button` | `workingDirectory` | いいえ | 作業ディレクトリ。既定値は空文字 |
| `button` | `image` | いいえ | PNG、JPEG、ICO 画像へのパス |

カテゴリー ID 同士とボタン ID 同士は、大文字小文字を区別せず一意にしてください。同じ `order` の項目は XML に書いた順序を維持します。カテゴリーと各カテゴリーには、それぞれ 1 件以上の項目が必要です。

## エラーと警告

- 必須属性の不足、不正な `order`、重複 ID、未対応バージョン、DTD は設定エラーです。読み込みを中止し、可能な場合はファイル名・行・列と修正理由を表示します。
- `executable` が存在しないボタンは無効化し、ほかのボタンは利用可能なままにします。
- `image` が存在しない場合は警告を記録し、既定画像を使います。
