' LauncherProductTests.vb
' UI に依存しない製品情報が期待どおり提供されることを検証するテストモジュールです。
' ステップ1ではテスト基盤そのものが動作することを、この小さなテストで確認します。

Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Launcher.Core.Product

''' <summary>
''' <see cref="LauncherProduct"/> の既定値を検証します。
''' </summary>
<TestClass>
Public Class LauncherProductTests
    ''' <summary>
    ''' 既定の製品名が空でなく、画面の名称と一致することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub GetDisplayName_ReturnsJapaneseProductName()
        Dim actual As String = LauncherProduct.GetDisplayName()

        Assert.AreEqual("業務ランチャー", actual)
    End Sub
End Class
