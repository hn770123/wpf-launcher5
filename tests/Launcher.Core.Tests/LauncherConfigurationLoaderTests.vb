' LauncherConfigurationLoaderTests.vb
' XML 設定の正常変換、入力検証、XXE 対策、ファイル欠落時の縮退動作を検証します。

Imports System.IO
Imports Launcher.Core.Services
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' <see cref="LauncherConfigurationLoader"/> の設定境界を一時ファイルで検証します。
''' </summary>
<TestClass>
Public Class LauncherConfigurationLoaderTests
    Private _temporaryDirectory As String

    ''' <summary>
    ''' 各テストで分離された設定ディレクトリを作成します。
    ''' </summary>
    <TestInitialize>
    Public Sub Initialize()
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "Launcher.Core.Tests", Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(_temporaryDirectory)
    End Sub

    ''' <summary>
    ''' テストで作成した設定と疑似ファイルを削除します。
    ''' </summary>
    <TestCleanup>
    Public Sub Cleanup()
        If Directory.Exists(_temporaryDirectory) Then Directory.Delete(_temporaryDirectory, recursive:=True)
    End Sub

    ''' <summary>
    ''' 正常な設定が order 順のモデルと解決済みパスへ変換されることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_ValidConfiguration_ReturnsSortedResolvedModel()
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.exe"), String.Empty)
        File.WriteAllText(Path.Combine(_temporaryDirectory, "icon.png"), String.Empty)
        Dim configurationPath As String = WriteConfiguration(
            "<category id=""later"" name=""後"" order=""20"">" &
            "<button id=""second"" name=""第二"" order=""20"" executable=""app.exe"" />" &
            "<button id=""first"" name=""第一"" order=""10"" executable=""app.exe"" image=""icon.png"" />" &
            "</category>" &
            "<category id=""earlier"" name=""前"" order=""10"">" &
            "<button id=""only"" name=""一件"" order=""10"" executable=""app.exe"" />" &
            "</category>")

        Dim result As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)

        Assert.AreEqual("テストランチャー", result.Configuration.Title)
        Assert.AreEqual(1, result.Configuration.Version)
        Assert.AreEqual("earlier", result.Configuration.Categories(0).Id)
        Assert.AreEqual("first", result.Configuration.Categories(1).Buttons(0).Id)
        Assert.AreEqual(Path.Combine(_temporaryDirectory, "icon.png"), result.Configuration.Categories(1).Buttons(0).ImagePath)
        Assert.IsTrue(result.Configuration.Categories(1).Buttons(0).IsEnabled)
        Assert.AreEqual(0, result.Warnings.Count)
    End Sub

    ''' <summary>
    ''' 必須属性がない XML を行列情報付きの設定エラーとして拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_MissingRequiredAttribute_ThrowsActionableError()
        Dim configurationPath As String = WriteRaw("<launcher title=""テスト"" version=""1""><categories>" &
                                      "<category id=""daily"" name=""日常"" order=""1"">" &
                                      "<button id=""app"" order=""1"" executable=""app.exe"" />" &
                                      "</category></categories></launcher>")

        Dim exception As ConfigurationException = CaptureConfigurationException(
            Sub()
                Dim unused As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)
            End Sub)

        StringAssert.Contains(exception.Message, "スキーマに適合しません")
        StringAssert.Contains(exception.Message, "行")
    End Sub

    ''' <summary>
    ''' 大文字小文字だけが異なるボタン ID も重複として拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_DuplicateButtonId_ThrowsActionableError()
        Dim configurationPath As String = WriteConfiguration(
            "<category id=""daily"" name=""日常"" order=""1"">" &
            "<button id=""App"" name=""一"" order=""1"" executable=""one.exe"" />" &
            "<button id=""app"" name=""二"" order=""2"" executable=""two.exe"" />" &
            "</category>")

        Dim exception As ConfigurationException = CaptureConfigurationException(
            Sub()
                Dim unused As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)
            End Sub)

        StringAssert.Contains(exception.Message, "ボタン ID 'app' が重複")
    End Sub

    ''' <summary>
    ''' 整数でない order を XSD 検証で拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_InvalidOrder_ThrowsSchemaError()
        Dim configurationPath As String = WriteConfiguration(
            "<category id=""daily"" name=""日常"" order=""先頭"">" &
            "<button id=""app"" name=""アプリ"" order=""1"" executable=""app.exe"" />" &
            "</category>")

        Dim exception As ConfigurationException = CaptureConfigurationException(
            Sub()
                Dim unused As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)
            End Sub)

        StringAssert.Contains(exception.Message, "スキーマに適合しません")
        StringAssert.Contains(exception.Message, "order")
    End Sub

    ''' <summary>
    ''' DTD を含む XXE 入力を外部ファイルへアクセスする前に拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_XxeInput_RejectsDtd()
        Dim secretPath As String = Path.Combine(_temporaryDirectory, "secret.txt")
        File.WriteAllText(secretPath, "読み取ってはいけない値")
        Dim uri As String = New Uri(secretPath).AbsoluteUri
        Dim configurationPath As String = WriteRaw("<!DOCTYPE launcher [<!ENTITY xxe SYSTEM """ & uri & """>]>" &
                                      "<launcher title=""&xxe;"" version=""1""><categories>" &
                                      "<category id=""daily"" name=""日常"" order=""1"">" &
                                      "<button id=""app"" name=""アプリ"" order=""1"" executable=""app.exe"" />" &
                                      "</category></categories></launcher>")

        Dim exception As ConfigurationException = CaptureConfigurationException(
            Sub()
                Dim unused As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)
            End Sub)

        StringAssert.Contains(exception.Message, "確認してください")
        Assert.IsFalse(exception.Message.Contains("読み取ってはいけない値"))
    End Sub

    ''' <summary>
    ''' 画像欠落時はボタンを有効なままにし、既定画像への切替警告を返すことを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_MissingImage_ReturnsWarningAndEmptyImagePath()
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.exe"), String.Empty)
        Dim configurationPath As String = WriteConfiguration(
            "<category id=""daily"" name=""日常"" order=""1"">" &
            "<button id=""app"" name=""アプリ"" order=""1"" executable=""app.exe"" image=""missing.png"" />" &
            "</category>")

        Dim result As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)

        Assert.IsTrue(result.Configuration.Categories(0).Buttons(0).IsEnabled)
        Assert.AreEqual(String.Empty, result.Configuration.Categories(0).Buttons(0).ImagePath)
        StringAssert.Contains(result.Warnings(0), "既定画像を使用")
    End Sub

    ''' <summary>
    ''' 実行ファイル欠落時は該当ボタンだけを無効化して警告することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Load_MissingExecutable_DisablesButtonAndReturnsWarning()
        Dim configurationPath As String = WriteConfiguration(
            "<category id=""daily"" name=""日常"" order=""1"">" &
            "<button id=""app"" name=""アプリ"" order=""1"" executable=""missing.exe"" />" &
            "</category>")

        Dim result As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)

        Assert.IsFalse(result.Configuration.Categories(0).Buttons(0).IsEnabled)
        StringAssert.Contains(result.Warnings(0), "実行ファイルが見つからない")
    End Sub

    ''' <summary>
    ''' 環境変数を展開した後に相対パスとして設定ディレクトリから解決することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Resolve_EnvironmentVariable_ExpandsValue()
        Const variableName As String = "LAUNCHER_TEST_FILE"
        Environment.SetEnvironmentVariable(variableName, "nested\app.exe")
        Try
            Dim actual As String = New ConfigurationPathResolver().Resolve("%" & variableName & "%", _temporaryDirectory)

            Assert.AreEqual(Path.Combine(_temporaryDirectory, "nested", "app.exe"), actual)
        Finally
            Environment.SetEnvironmentVariable(variableName, Nothing)
        End Try
    End Sub

    ''' <summary>
    ''' カテゴリー断片を version 1 の完全な設定として一時ファイルへ保存します。
    ''' </summary>
    Private Function WriteConfiguration(categories As String) As String
        Return WriteRaw("<launcher title=""テストランチャー"" version=""1""><categories>" &
                        categories & "</categories></launcher>")
    End Function

    ''' <summary>
    ''' 指定 XML を一時設定ファイルへ UTF-8 で保存し、そのパスを返します。
    ''' </summary>
    Private Function WriteRaw(xml As String) As String
        Dim outputPath As String = Path.Combine(_temporaryDirectory, "launcher.config.xml")
        File.WriteAllText(outputPath, xml)
        Return outputPath
    End Function

    ''' <summary>
    ''' 対象処理が返した設定例外を取得し、例外がなければテストを失敗させます。
    ''' </summary>
    Private Shared Function CaptureConfigurationException(action As Action) As ConfigurationException
        Try
            action()
        Catch ex As ConfigurationException
            Return ex
        End Try

        Assert.Fail("ConfigurationException が発生する必要があります。")
        Return Nothing
    End Function
End Class
