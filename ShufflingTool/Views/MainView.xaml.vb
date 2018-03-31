Public Class MainView
    Private Sub updateSqlDbButton_Click(sender As Object, e As RoutedEventArgs)
        CallbackWriteToConsoleOutput("Clicked Update SQL")
        Me.Content = New SQLUpdaterView
    End Sub

    Private Sub reshuffleButton_Click(sender As Object, e As RoutedEventArgs)
        CallbackWriteToConsoleOutput("Clicked Reshuffle")
        Me.Content = New ReshufflingView
        'RService.Test()
    End Sub
End Class
