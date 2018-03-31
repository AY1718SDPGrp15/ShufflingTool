Class MainWindow



    Private Sub mainMenuButton_Click(sender As Object, e As RoutedEventArgs)
        CallbackWriteToConsoleOutput("Clicked Main Menu")
        mainView.Content = New MainView
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GlobalVariables.CallbackWriteToConsoleOutput = AddressOf UpdateLog
        GlobalVariables.CallbackToggleProgressBar = AddressOf ToggleProgressBar
        CallbackWriteToConsoleOutput("Loaded")
    End Sub

    Private Sub UpdateLog(output As String)
        Application.Current.Dispatcher.BeginInvoke(Sub()
                                                       outputTextBox.AppendText(Environment.NewLine & output)
                                                       outputTextBox.ScrollToEnd()
                                                   End Sub)
    End Sub

    Private Sub ToggleProgressBar()
        Application.Current.Dispatcher.BeginInvoke(Sub()
                                                       If progressBar.Visibility = Visibility.Visible Then
                                                           progressBar.Visibility = Visibility.Collapsed
                                                       Else
                                                           progressBar.Visibility = Visibility.Visible
                                                       End If
                                                   End Sub)
    End Sub


End Class

Public Module GlobalVariables

    Public Delegate Sub WriteToConsoleOutput(output As String)
    Public CallbackWriteToConsoleOutput As WriteToConsoleOutput = Nothing

    Public Delegate Sub ToggleProgressBar()
    Public CallbackToggleProgressBar As ToggleProgressBar = Nothing


End Module