' ConfigurationException.vb
' 利用者が設定を修正できる日本語メッセージを伴う例外を定義します。

Namespace Services
    ''' <summary>
    ''' XML 設定が安全性または形式の検証に失敗したことを表します。
    ''' </summary>
    Public NotInheritable Class ConfigurationException
        Inherits Exception

        ''' <summary>
        ''' 利用者向けメッセージと原因例外を保持します。
        ''' </summary>
        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

        ''' <summary>
        ''' 利用者向けメッセージを保持します。
        ''' </summary>
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class
End Namespace
