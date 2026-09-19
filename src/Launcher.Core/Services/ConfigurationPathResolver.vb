' ConfigurationPathResolver.vb
' 設定値に含まれる環境変数と相対パスを一貫した規則で解決します。

Imports System.IO

Namespace Services
    ''' <summary>
    ''' 設定ファイルの配置場所を基準にパスを絶対パスへ変換します。
    ''' </summary>
    Public NotInheritable Class ConfigurationPathResolver
        ''' <summary>
        ''' 環境変数を展開し、相対パスなら設定ディレクトリを基準に解決します。
        ''' </summary>
        Public Function Resolve(pathValue As String, configurationDirectory As String) As String
            If String.IsNullOrWhiteSpace(pathValue) Then
                Return String.Empty
            End If

            Dim expanded As String = Environment.ExpandEnvironmentVariables(pathValue.Trim())
            If Path.IsPathRooted(expanded) Then
                Return Path.GetFullPath(expanded)
            End If

            Return Path.GetFullPath(Path.Combine(configurationDirectory, expanded))
        End Function
    End Class
End Namespace
