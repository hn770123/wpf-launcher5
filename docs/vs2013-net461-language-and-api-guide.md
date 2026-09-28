# Visual Studio 2013 (.NET Framework 4.6.1) 環境における言語機能・API 制約ガイド

## 1. 概要と前提環境

Visual Studio 2013（以下 VS2013）と .NET Framework 4.6.1 の組み合わせにおける標準コンパイラおよびランタイムのバージョン仕様は以下の通りです。

- **C# バージョン**: C# 5.0 (VS2013 標準コンパイラ)
- **VB.NET バージョン**: Visual Basic 11.0 / VB 2013 (VS2013 標準コンパイラ)
- **ターゲットランタイム**: .NET Framework 4.6.1 (2015年11月リリース)

後続のバージョンで追加された C# / VB.NET の言語機能、Roslyn コンパイラ導入後の構文、および .NET Framework 4.6.2 以降や Modern .NET (.NET Core / .NET 5〜10) で追加された API について、制限・代替記法・Polyfill の可否をまとめて解説します。

---

## 2. VS2013 (.NET Framework 4.6.1) で使用できない言語機能

VS2013 の標準コンパイラ環境では、**C# 6.0 以降** および **VB 14 以降** のすべての新構文・言語機能が使用できません。

### 2.1 C# 5.0 超過により使用できない主要な C# 言語機能

| バージョン | 機能名 | VS2013 での使用可否 |
|---|---|---|
| **C# 6.0** (VS2015) | 文字列補間 (`$"Hello {name}"`) |不可 |
| **C# 6.0** | Null 条件演算子 (`x?.Property`, `x?.Invoke()`) | 不可 |
| **C# 6.0** | `nameof` 演算子 (`nameof(variable)`) | 不可 |
| **C# 6.0** | 式形式のメンバー (`=> x + y`) | 不可 |
| **C# 6.0** | 自動プロパティの初期化子 (`public int X { get; } = 10;`) | 不可 |
| **C# 6.0** | 読み取り専用自動プロパティ (`public int X { get; }`) | 不可 |
| **C# 6.0** | 辞書初期化子 (`new Dictionary { ["key"] = "val" }`) | 不可 |
| **C# 6.0** | `using static` ディレクティブ | 不可 |
| **C# 6.0** | catch / finally ブロックでの `await` | 不可 |
| **C# 6.0** | 例外フィルター (`catch (Exception ex) when (...)`) | 不可 |
| **C# 7.0〜7.3** (VS2017) | タプル型とタプル文字通 (`(int x, int y) t = (1, 2);`) | 不可 |
| **C# 7.0〜7.3** | パターンマッチング (`is`, `switch` 式/パターン) | 不可 |
| **C# 7.0〜7.3** | ローカル関数 (`void LocalMethod() { ... }`) | 不可 |
| **C# 7.0〜7.3** | `out` 変数宣言 (`int.TryParse(s, out int result)`) | 不可 |
| **C# 7.0〜7.3** | 数値リテラルの区切り文字 (`1_000_000`) と 2進数リテラル (`0b1010`) | 不可 |
| **C# 7.0〜7.3** | 非同期 Main (`static async Task Main(...)`) | 不可 |
| **C# 8.0** (VS2019) | ヌル許容参照型 (`string? name`) | 不可 |
| **C# 8.0** | Switch 式 (`x switch { ... }`) | 不可 |
| **C# 8.0** | 非同期ストリーム (`IAsyncEnumerable<T>`, `await foreach`) | 不可 |
| **C# 8.0** | `using` 宣言 (`using var stream = ...;`) | 不可 |
| **C# 8.0** | インデックスとレンジ (`array[1..^1]`) | 不可 |
| **C# 8.0** | デフォルトインターフェイス実装 | 不可 |
| **C# 9.0** (VS2019) | レコード型 (`record Person(string Name);`) | 不可 |
| **C# 9.0** | init 専用プロパティ (`public int X { get; init; }`) | 不可 |
| **C# 9.0** | 最上位の文 (Top-level statements) | 不可 |
| **C# 9.0** | ターゲット型の `new` 式 (`Point p = new(1, 2);`) | 不可 |
| **C# 10.0〜13** (VS2022) | プライマリコンストラクタ (クラス/構造体) | 不可 |
| **C# 10.0〜13** | コレクション式 (`int[] a = [1, 2, 3];`) | 不可 |
| **C# 10.0〜13** | ファイルスコープの名前空間 (`namespace MyNamespace;`) | 不可 |
| **C# 10.0〜13** | グローバル `using` ディレクティブ (`global using System;`) | 不可 |

### 2.2 VB 11.0 超過により使用できない主要な VB.NET 言語機能

| バージョン | 機能名 | VS2013 での使用可否 |
|---|---|---|
| **VB 14** (VS2015) | 文字列補間 (`$"Hello {name}"`) | 不可 |
| **VB 14** | Null 条件演算子 (`x?.Property`, `x?.Invoke()`) | 不可 |
| **VB 14** | `NameOf` 演算子 (`NameOf(variable)`) | 不可 |
| **VB 14** | 読み取り専用自動プロパティ (`ReadOnly Property X As Integer = 10`) | 不可 |
| **VB 14** | 行継続文字 `_` なしの複数行記述の拡張 | 不可 |
| **VB 14** | 構造体でのパラメーターなしコンストラクタ・初期化子 | 不可 |
| **VB 14** | #If ブロック内でのインライン注釈・コメント制限の緩和 | 不可 |
| **VB 15.0〜15.8** (VS2017) | タプル記法 (`Dim t = (x:=1, y:=2)`) | 不可 |
| **VB 15.0〜15.8** | 2進数リテラル (`&B1010`) と数値区切り文字 (`1_000_000`) | 不可 |
| **VB 15.0〜15.8** | `ByRef` 戻り値 | 不可 |

---

## 3. VS2013 コンパイラ制限下での代替記法・書き方の制約

VS2013 を使用する場合、上記の新機能はすべて**旧来の C# 5.0 / VB 11.0 の文法で記述する**必要があります。

### 3.1 C# 5.0 における代替記法一覧

#### 1) 文字列補間 (String Interpolation) の代替
- **C# 6.0+**: `string msg = $"Name: {name}, Age: {age}";`
- **C# 5.0 代替**:
  ```csharp
  string msg = string.Format("Name: {0}, Age: {1}", name, age);
  ```

#### 2) Null 条件演算子 (Null-Conditional Operator) の代替
- **C# 6.0+**: `int? length = list?.Count;` / `item?.Execute();`
- **C# 5.0 代替**:
  ```csharp
  int? length = (list != null) ? (int?)list.Count : null;

  if (item != null)
  {
      item.Execute();
  }
  ```

#### 3) `nameof` 演算子の代替
- **C# 6.0+**: `throw new ArgumentNullException(nameof(paramName));`
- **C# 5.0 代替**:
  ```csharp
  // 文字列リテラルを直接記述する（リファクタリング時に手動更新が必要）
  throw new ArgumentNullException("paramName");
  ```

#### 4) 自動プロパティ初期化子・読み取り専用プロパティの代替
- **C# 6.0+**: `public string Id { get; } = Guid.NewGuid().ToString();`
- **C# 5.0 代替**:
  ```csharp
  private readonly string _id;
  public string Id { get { return _id; } }

  public MyClass()
  {
      _id = Guid.NewGuid().ToString();
  }
  ```

#### 5) 式形式のメンバー (Expression-bodied members) の代替
- **C# 6.0+**: `public int Sum(int a, int b) => a + b;`
- **C# 5.0 代替**:
  ```csharp
  public int Sum(int a, int b)
  {
      return a + b;
  }
  ```

#### 6) パターンマッチング・`out` 変数宣言の代替
- **C# 7.0+**: `if (obj is Person p) { ... }` / `int.TryParse(s, out int val);`
- **C# 5.0 代替**:
  ```csharp
  Person p = obj as Person;
  if (p != null)
  {
      // ...
  }

  int val;
  if (int.TryParse(s, out val))
  {
      // ...
  }
  ```

### 3.2 VB 11.0 (VS2013) における代替記法一覧

#### 1) 文字列補間の代替
- **VB 14+**: `Dim msg = $"Name: {name}, Age: {age}"`
- **VB 11.0 代替**:
  ```vb
  Dim msg As String = String.Format("Name: {0}, Age: {1}", name, age)
  ```

#### 2) Null 条件演算子の代替
- **VB 14+**: `Dim count = list?.Count`
- **VB 11.0 代替**:
  ```vb
  Dim count As Nullable(Of Integer) = If(list IsNot Nothing, New Nullable(Of Integer)(list.Count), Nothing)
  ```

#### 3) `NameOf` 演算子の代替
- **VB 14+**: `Throw New ArgumentNullException(NameOf(paramName))`
- **VB 11.0 代替**:
  ```vb
  Throw New ArgumentNullException("paramName")
  ```

#### 4) 読み取り専用自動プロパティ初期化子の代替
- **VB 14+**: `Public ReadOnly Property Id As String = Guid.NewGuid().ToString()`
- **VB 11.0 代替**:
  ```vb
  Private ReadOnly _id As String

  Public ReadOnly Property Id As String
      Get
          Return _id
      End Get
  End Property

  Public Sub New()
      _id = Guid.NewGuid().ToString()
  End Sub
  ```

---

## 4. 新しい Visual Studio (Roslyn) で .NET Framework 4.6.1 をターゲットにする場合の挙動と Polyfill

VS2019 や VS2022 などの**最新の Visual Studio (MSBuild / Roslyn コンパイラ)** を使用して .NET Framework 4.6.1 向けにビルドする場合、コンパイラ構文とランタイムサポートの分離について理解しておく必要があります。

### 4.1 コンパイラ機能とランタイム依存機能の分類

Roslyn コンパイラを使用する場合、機能は以下の3つに分類されます。

1. **純粋なコンパイラ機能（4.6.1 でそのまま使用可能）**:
   - 文字列補間 (`$"..."`)
   - Null 条件演算子 (`?.`)
   - `nameof` 演算子
   - 式形式のメンバー (`=>`)
   - 自動プロパティ初期化子
   - パターンマッチング (一部)
   - `out` 変数宣言
   - 最上位の文、ファイルスコープ名前空間、グローバル using
   - コレクション式 (配列や標準 List への展開)

2. **Polyfill (NuGet パッケージ追加や属性定義) により 4.6.1 で使用可能になる機能**:
   - **ValueTuple (`(int, string)`)**: `System.ValueTuple` (NuGet) を追加することで使用可能。
   - **`Span<T>` / `ReadOnlySpan<T>` / `Memory<T>`**: `System.Memory` (NuGet) を追加することでポータブル版が使用可能。
   - **`init` 専用プロパティ**: 以下の `IsExternalInit` 属性をプロジェクト内に定義することで使用可能。
     ```csharp
     namespace System.Runtime.CompilerServices
     {
         internal static class IsExternalInit { }
     }
     ```
   - **Index / Range (`array[1..^1]`)**: `System.Index` / `System.Range` 構造体および `RuntimeHelpers.GetSubArray` 等の Polyfill (例: `Microsoft.Bcl.HashCode` や PolySharp ライブラリ) を配置することで使用可能。
   - **`Nullable` 属性 (C# 8.0 ヌル許容参照型)**: コンパイラが要求する内部属性 (`NullableAttribute`, `NullableContextAttribute`) を定義または Polyfill ライブラリを読み込むことで静的解析機能が有効化。

3. **4.6.1 / Mono / Legacy CLR では Polyfill をしても使用できない（または極めて制限される）機能**:
   - **デフォルトインターフェイス実装 (C# 8.0)**: ランタイム (CoreCLR / .NET Core 3.0+) の JIT サポートが必須。.NET Framework 4.6.1 では動作不可。
   - **`ref struct` の完全な機能 (C# 7.2+)**: .NET Framework の JIT は `ref struct` のスタック境界安全性を完全に保証しないため、一部制約や警告が発生。
   - **`IAsyncEnumerable<T>` (C# 8.0)**: `Microsoft.Bcl.AsyncInterfaces` NuGet パッケージを追加すればインターフェイス自体は使用可能だが、.NET Framework 上での非同期ストリームパフォーマンスは .NET Core / 5+ に及ばない。
   - **Generic Math (C# 11)**: ランタイムの Static Abstract Members in Interfaces サポート (.NET 7+) が必須のため不可。

---

## 5. .NET Framework 4.6.1 より後に追加された API

.NET Framework の後続バージョン（4.6.2 〜 4.8.1）および Modern .NET (.NET Core 1.0 〜 .NET 10) で追加された主要な API の整理です。これらは **.NET Framework 4.6.1 の標準アセンブリには含まれていません**。

### 5.1 .NET Framework 4.6.2 〜 4.8.1 で追加された API

| .NET バージョン | 主な追加 API / 機能 | 4.6.1 での使用可否・代替策 |
|---|---|---|
| **.NET 4.6.2** | WPF Per-Monitor DPI V2 サポート (`DpiChanged` イベント等) | 4.6.1 では System DPI / Per-Monitor V1 のみ。P/Invoke (`SetProcessDpiAwarenessContext`) で一部代替 |
| **.NET 4.6.2** | 暗号化 API の拡張 (`ECDsaCng`, `DSA.Create()`, `RSA.Create()`) | 4.6.1 では旧来の `RSACryptoServiceProvider` 等を使用 |
| **.NET 4.6.2** | SignedXml の SHA-256 サポート強化 | NuGet 補完または設定ファイルでのアルゴリズム登録 |
| **.NET 4.7** | `System.ValueTuple` 構造体が標準アセンブリに統合 | 4.6.1 では NuGet (`System.ValueTuple` v4.5.0) が必須 |
| **.NET 4.7** | High DPI の機能強化 (Windows Forms / WPF) | 4.6.1 では利用不可 |
| **.NET 4.7.1** | `System.Text.Json` パッケージの互換基礎 | 4.6.1 では `Newtonsoft.Json` (Json.NET) の使用が推奨 |
| **.NET 4.7.2** | `RSA.Create()` / `ECDsa.Create()` の強化、 ephemeral 鍵サポート | 4.6.1 では限定的 |
| **.NET 4.8 / 4.8.1** | TLS 1.3 サポート (OS 依存: Windows 11 / Server 2022) | 4.6.1 では TLS 1.2 まで (OS 設定および `ServicePointManager.SecurityProtocol` の明示が必要) |
| **.NET 4.8.1** | ARM64 Win32 Native サポート | 4.6.1 では x86/x64 エミュレーションのみ |

### 5.2 Modern .NET (.NET Core 1.0 〜 .NET 10) で導入された主要 API

以下の API は Modern .NET で新設されたものであり、.NET Framework 4.6.1 の標準アセンブリには存在しません（一部は Out-of-band NuGet パッケージにより利用可能）。

| API / 機能名 | 導入バージョン | .NET 4.6.1 での利用可否と対応 |
|---|---|---|
| **`Span<T>` / `ReadOnlySpan<T>`** | .NET Core 2.1 / .NET Standard 2.1 | NuGet `System.Memory` を追加することでポータブル版が使用可能（ただし JIT 最適化は制限される） |
| **`System.Text.Json`** | .NET Core 3.0 / .NET Standard 2.0 | NuGet `System.Text.Json` を追加して使用可能（ただし .NET 4.6.1 では `Newtonsoft.Json` が一般的） |
| **`System.Threading.Channels`** | .NET Core 3.0 | NuGet `System.Threading.Channels` を追加して使用可能 |
| **Minimal API** | .NET 6 (ASP.NET Core) | **利用不可** (.NET 6+ の ASP.NET Core ランタイム専用) |
| **Generic Math (`INumber<T>`)** | .NET 7 | **利用不可** (静的抽象メンバーのランタイムサポートが必要) |
| **`FrozenDictionary<TKey, TValue>`** | .NET 8 | **利用不可** (NuGet なし、.NET 8 ランタイム専用) |
| **`System.Threading.Lock`** | .NET 9 | **利用不可** (`lock (obj)` の旧来記法を使用) |
| **HybridCache** | .NET 9 | **利用不可** |

---

## 6. まとめと開発上の推奨事項

1. **VS2013 を開発環境として強制使用する場合**:
   - C# 5.0 / VB 11.0 の文法規則に厳密に従う必要があります。
   - `$"..."` や `?.` などのモダンな記法は一切使用せず、`string.Format` や明示的な `null` チェック、従来型のプロパティ定義を行ってください。

2. **最新の VS (2022等) で .NET Framework 4.6.1 をターゲットにする場合**:
   - 最新の C# コンパイラ機能 (C# 10〜12) の多くは利用可能ですが、`System.ValueTuple` や `System.Memory` などの NuGet パッケージの導入が必要です。
   - ただし、ランタイム特有の機能（デフォルトインターフェイス実装や Generic Math、Minimal API 等）は利用できないため、アーキテクチャ設計時に注意が必要です。

3. **JSON や非同期処理、ネットワーク通信**:
   - .NET 4.6.1 では `Newtonsoft.Json` や `HttpClient` (`System.Net.Http` NuGet) の利用が標準的であり、TLS 1.2 を明示的に有効化する設定などを考慮する必要があります。
