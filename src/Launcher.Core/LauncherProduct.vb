' LauncherProduct.vb
' ランチャー全体で共有する製品情報を提供するモジュールです。
' UI に依存しない値だけを保持し、Core 単体で検証できる境界を作ります。

Namespace Product
    ''' <summary>
    ''' ランチャーの製品情報を提供します。
    ''' </summary>
    Public NotInheritable Class LauncherProduct
        Private Sub New()
            ' このクラスは定数の提供だけを目的とするため、インスタンス化を禁止します。
        End Sub

        ''' <summary>
        ''' 画面とログで使用する既定の製品名を返します。
        ''' </summary>
        ''' <returns>日本語の既定製品名。</returns>
        Public Shared Function GetDisplayName() As String
            Return "業務ランチャー"
        End Function
    End Class
End Namespace
