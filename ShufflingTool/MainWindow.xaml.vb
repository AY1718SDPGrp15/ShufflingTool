Class MainWindow
    Private Sub updateSqlDbButton_Click(sender As Object, e As RoutedEventArgs)
        Me.Content = New SQLUpdaterView
    End Sub

    Private Sub reshuffleButton_Click(sender As Object, e As RoutedEventArgs)
        Me.Content = New MainView
    End Sub
End Class
