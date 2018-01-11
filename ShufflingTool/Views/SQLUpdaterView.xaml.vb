Imports System.Data

Public Class SQLUpdaterView

    Private Sub openRawDataButton_Click(sender As Object, e As RoutedEventArgs)
        Dim fileUpLoadDialog As New Forms.OpenFileDialog()
        fileUpLoadDialog.Filter = "All Files (*.*)|*.*"
        fileUpLoadDialog.Multiselect = False

        Dim fileUploadResult As String = fileUpLoadDialog.ShowDialog.ToString

        If fileUploadResult.ToUpper.Equals("OK") Then
            Try
                Dim dt As DataTable
                dt = ImportExceltoDatatable(fileUpLoadDialog.FileName)
                GridControl.ItemsSource = dt.AsDataView
                GridControl.Visibility = Visibility.Visible
                MsgBox(" Imported New Data ", MsgBoxStyle.Information)
            Catch ex As Exception

            End Try
        End If

    End Sub
End Class
