' LauncherImageConverter.vb
' 設定された画像を必要になった時点で読み込み、ファイルをロックせず表示します。
' 未指定・破損時は埋め込み不要の既定アイコンを描画して継続します。

Imports System.Globalization
Imports System.IO
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace Launcher.App
    ''' <summary>
    ''' 画像パスを WPF の ImageSource へ遅延変換します。
    ''' </summary>
    Public NotInheritable Class LauncherImageConverter
        Implements IValueConverter

        ''' <summary>
        ''' ファイルをメモリへ完全に読み込み、失敗時は既定の図形を返します。
        ''' </summary>
        Public Function Convert(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim path As String = TryCast(value, String)
            If Not String.IsNullOrWhiteSpace(path) Then
                Try
                    Using stream As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                        Dim bitmap As New BitmapImage()
                        bitmap.BeginInit()
                        bitmap.CacheOption = BitmapCacheOption.OnLoad
                        bitmap.StreamSource = stream
                        bitmap.EndInit()
                        bitmap.Freeze()
                        Return bitmap
                    End Using
                Catch ex As Exception
                    ' 形式不正やアクセス拒否を含む個別画像の問題では、画面全体を止めず既定画像へ切り替えます。
                End Try
            End If

            Return CreateFallbackImage()
        End Function

        ''' <summary>
        ''' 一方向バインディング専用のため逆変換は提供しません。
        ''' </summary>
        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                    culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return DependencyProperty.UnsetValue
        End Function

        ''' <summary>
        ''' 外部ファイルなしで表示できる四分割の既定アイコンを生成します。
        ''' </summary>
        Private Shared Function CreateFallbackImage() As ImageSource
            Dim drawing As New DrawingGroup()
            Using context As DrawingContext = drawing.Open()
                Dim brush As New SolidColorBrush(Color.FromRgb(&H31, &H5E, &HFB))
                context.DrawRoundedRectangle(brush, Nothing, New Rect(5, 5, 18, 18), 3, 3)
                context.DrawRoundedRectangle(brush, Nothing, New Rect(27, 5, 18, 18), 3, 3)
                context.DrawRoundedRectangle(brush, Nothing, New Rect(5, 27, 18, 18), 3, 3)
                context.DrawRoundedRectangle(brush, Nothing, New Rect(27, 27, 18, 18), 3, 3)
            End Using
            Dim image As New DrawingImage(drawing)
            image.Freeze()
            Return image
        End Function
    End Class
End Namespace
