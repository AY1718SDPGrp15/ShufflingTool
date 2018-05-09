Imports System.Data

Public Class SQLUpdaterView

    Dim uploadedDt As DataTable
    Dim displayDt As DataTable
    Dim lastDocumentDate As Date = Nothing
    Dim fileUploadName
    Public KMEntity As KONICA_MINOLTA_DBEntities = GetDatabase()
    Public locationName As String = ""

    ' when Update button is triggered
    Private Sub openRawDataButton_Click(sender As Object, e As RoutedEventArgs)
        If checkFail() Then
            MsgBox("Pls check for empty fields")
            Return
        End If
        locationName = countryComboBox.Text
        Dim fileUpLoadDialog As New Forms.OpenFileDialog()
        fileUpLoadDialog.Filter = "All Files (*.*)|*.*"
        fileUpLoadDialog.Multiselect = False
        Dim fileUploadResult As String = fileUpLoadDialog.ShowDialog.ToString
        fileUploadName = fileUpLoadDialog.FileName
        If fileUploadResult.ToUpper.Equals("OK") Then
            Try
                If lastUpdatedTimeText.Text <> "" Then
                    lastDocumentDate = lastUpdatedTimeText.Text
                End If
                Dim thread As System.Threading.Thread
                thread = New System.Threading.Thread(AddressOf DisplayDataTableFromExcel)
                thread.Start()
            Catch ex As Exception
                CallbackWriteToConsoleOutput(ex.ToString)
            End Try
        End If
    End Sub

    ' checks if location is entered
    Private Function checkFail() As Boolean
        If String.IsNullOrEmpty(countryComboBox.Text) = False Then
            Return False
        End If
        Return True
    End Function

    'displays the datatable from excel
    Private Sub DisplayDataTableFromExcel()
        CallbackToggleProgressBar()
        displayDt = New DataTable
        uploadedDt = ImportExceltoDatatable(fileUploadName)
        displayDt = uploadedDt.Clone
        Dim rows = uploadedDt.Rows
        Dim columns = uploadedDt.Columns
        Dim count = 0
        'gets the latest available demand data for the country
        Dim lastDemandData As DEMAND = (From query In KMEntity.DEMANDs Where query.LOCATION.COUNTRY_NAME = locationName Order By query.ID Descending).FirstOrDefault
        If lastDemandData IsNot Nothing Then
            lastDocumentDate = lastDemandData.DATE_TIME
        End If
        For Each row As DataRow In rows
            Dim rowDate As Date = If(Not row(My.Settings.DOCUMENT_DATE).Equals(DBNull.Value), row(My.Settings.DOCUMENT_DATE), Nothing)
            If rowDate = Nothing OrElse rowDate <= lastDocumentDate Then
                Continue For
            End If
            displayDt.ImportRow(row)
        Next
        Application.Current.Dispatcher.BeginInvoke(Sub()
                                                       lastUpdatedTimeText.Text = If(lastDemandData IsNot Nothing, lastDemandData.DATE_TIME.ToString, "")
                                                       GridControl.ItemsSource = displayDt.AsDataView
                                                       GridControl.Visibility = Visibility.Visible
                                                       submitButton.Visibility = Visibility.Visible
                                                   End Sub)
        CallbackToggleProgressBar()
    End Sub

    'populates the sql
    Private Sub PopulateSQLFromDatabase()
        CallbackToggleProgressBar()
        Try
            For Each row As DataRow In displayDt.Rows
                If String.IsNullOrEmpty(row.Item(0).ToString) = False Then
                    Dim demand As DEMAND = New DEMAND
                    Dim order As ORDER_DETAILS = New ORDER_DETAILS
                    Dim location As LOCATION = Nothing
                    Dim existingSku As SKU = Nothing
                    Dim index = 0
                    While index < row.Table.Columns.Count
                        Select Case row.Table.Columns(index).ColumnName
                            Case My.Settings.PURCHASING_DOCUMENT
                                order.TYPE = row.Item(index).ToString
                            Case My.Settings.DESCRIPTION
                                order.NAME = row.Item(index).ToString
                            Case My.Settings.MATERIAL
                                Dim skuName = row.Item(index).ToString
                                existingSku = KMEntity.SKUs.Where(Function(p) p.NAME = skuName).FirstOrDefault
                                If existingSku Is Nothing Then
                                    existingSku = New SKU
                                    existingSku.NAME = skuName
                                    KMEntity.SKUs.Add(existingSku)
                                End If
                            Case My.Settings.DOCUMENT_DATE
                                demand.DATE_TIME = row.Item(index).ToString
                            Case My.Settings.LOCATION
                                Dim locationName = row.Item(index).ToString
                                location = KMEntity.LOCATIONs.Where(Function(p) p.LOCATION_CODE = locationName).FirstOrDefault
                                If location Is Nothing Then
                                    location = New LOCATION
                                    location.LOCATION_CODE = locationName
                                    location.BRANCH_NAME = row.Item(My.Settings.BRANCH).ToString
                                    Application.Current.Dispatcher.BeginInvoke(Sub()
                                                                                   location.COUNTRY_NAME = If(String.IsNullOrEmpty(countryComboBox.Text) = False, countryComboBox.Text, "NONE")
                                                                               End Sub)
                                    KMEntity.LOCATIONs.Add(location)
                                End If
                            Case My.Settings.QTY
                                order.ORDER_QTY = row.Item(index).ToString
                        End Select
                        index += 1
                    End While
                    KMEntity.SaveChanges()
                    demand.LOCATION_LINK_ID = location.ID
                    KMEntity.ORDER_DETAILS.Add(order)
                    KMEntity.SaveChanges()
                    demand.ORDER_DETAILS_LINK_ID = order.ID
                    demand.SKU_LINK_ID = existingSku.ID
                    KMEntity.DEMANDs.Add(demand)
                    KMEntity.SaveChanges()
                    CallbackWriteToConsoleOutput(String.Format("Added {0} into database", order.NAME))
                End If
            Next
        Catch ex As Exception
            CallbackWriteToConsoleOutput(ex.ToString)
        Finally
            CallbackWriteToConsoleOutput(String.Format("SKU Count = {0} LOCATION Count = {1}", KMEntity.SKUs.Count, KMEntity.LOCATIONs.Count))
            CallbackToggleProgressBar()
        End Try

    End Sub

    Private Sub SQLUpdaterView_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

    End Sub

    Private Sub submitButton_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim thread As System.Threading.Thread
            thread = New System.Threading.Thread(AddressOf PopulateSQLFromDatabase)
            thread.Start()
        Catch ex As Exception
        End Try
    End Sub
End Class
