Imports System.Data
Imports System.IO
Imports System.Windows
Imports RDotNet

Public Class ReshufflingView
    Private reshuffleDataTable As New DataTable
    Public KMEntity As KONICA_MINOLTA_DBEntities = GetDatabase()
    Private inventoryDt As DataTable
    Private fileUploadName
    Private shufflePeriod As Integer
    Private positiveShuffleLoc As List(Of ShuffleArrayClass) = New List(Of ShuffleArrayClass)
    Private negativeShuffleLoc As List(Of ShuffleArrayClass) = New List(Of ShuffleArrayClass)
    Private shuffledArray As List(Of ShufflingClass) = New List(Of ShufflingClass)
    Private reshuffledDatatable As New DataTable

    Private Sub reshuffleButton_Click(sender As Object, e As RoutedEventArgs)
        If CheckForNumericInput() = False Then
            MsgBox("Please check for numeric input", , "Alert")
            Return
        End If
        shufflePeriod = CInt(shuffleSkuPeriod.Text)
        Dim thread As System.Threading.Thread
        thread = New System.Threading.Thread(AddressOf Reshuffle)
        thread.Start()
    End Sub

    Private Sub Reshuffle()
        ClearEntity()
        KMEntity = GetDatabase()
        shuffledArray.Clear()
        CallbackToggleProgressBar()
        Try
            Dim KMEntity As KONICA_MINOLTA_DBEntities = DatabaseService.GetDatabase
            Dim demandList = KMEntity.DEMANDs.ToList
            Dim sourceTable As DataTable = New DataTable()

            sourceTable.Columns.AddRange(New DataColumn() {
            New DataColumn(My.Settings.R_ORDER_QTY, GetType(String)),
            New DataColumn(My.Settings.R_STORAGE_LOCATION, GetType(String)),
            New DataColumn(My.Settings.R_MATERIAL, GetType(String)),
            New DataColumn(My.Settings.R_DOCUMENT_DATE, GetType(String))
        })

            For Each demand In demandList
                Dim dateTime As Date = demand.DATE_TIME
                Dim dateString = dateTime.ToString(My.Settings.DATE_FORMAT)
                sourceTable.Rows.Add(demand.ORDER_DETAILS.ORDER_QTY, demand.LOCATION.LOCATION_CODE, demand.SKU.NAME, dateString)
            Next

            Dim fileLocation = Environment.CurrentDirectory + "\SQLDataFrame.csv"
            Using writer As StreamWriter = New StreamWriter(fileLocation)
                Rfc4180Writer.WriteDataTable(sourceTable, writer, True)
            End Using
            'Using writer As StreamWriter = New StreamWriter(fileLocation)
            '    Rfc4180Writer.WriteDataTable(sourceTable, writer, True)
            'End Using
            'MsgBox("Imported to CSV", MsgBoxStyle.Information)
            Dim dataFrame As DataFrame = RService.Test2(fileLocation, shufflePeriod)
            Dim dataTable As New DataTable
            For i = 0 To dataFrame.ColumnCount - 1
                dataTable.Columns.Add(dataFrame.ColumnNames(i).ToString, GetType(String))
            Next

            For i = 0 To dataFrame.RowCount - 1
                Dim row As DataRow = dataTable.NewRow()
                For k = 0 To dataFrame.ColumnCount - 1
                    row(k) = dataFrame(i, k).ToString
                Next
                dataTable.Rows.Add(row)
            Next

            Dim location = Environment.CurrentDirectory + "\DataFromR.csv"
            Using writer As StreamWriter = New StreamWriter(location)
                Rfc4180Writer.WriteDataTable(dataTable, writer, True)
            End Using
            Process.Start(location)

            reshuffledDatatable = New DataTable
            reshuffledDatatable.Columns.Add("SKU Name")
            reshuffledDatatable.Columns.Add("Location")
            reshuffledDatatable.Columns.Add("Received From Location")
            reshuffledDatatable.Columns.Add("Send To Location")
            reshuffledDatatable.Columns.Add("Quantity")
            Dim dateColumn As DataColumn = reshuffledDatatable.Columns.Add("Date", GetType(Date))

            For i = 0 To shufflePeriod Step 1
                Dim dtCopy = dataTable.Copy
                positiveShuffleLoc.Clear()
                negativeShuffleLoc.Clear()
                For index = i To dtCopy.Rows.Count - shufflePeriod Step shufflePeriod + 1
                    Dim row As DataRow = dtCopy.Rows(index)
                    If row.Item("SKU") = "NONE" Then
                        Continue For
                    End If
                    Dim newForecast As ShuffleArrayClass = New ShuffleArrayClass
                    Dim forecastQty As Integer = row.Item("PointForecast")
                    Dim nextForecastQty As Integer = dtCopy.Rows(index + 1).Item("PointForecast")
                    Dim SKU As String = row.Item("SKU")
                    Dim storageLocation = row.Item("Location")
                    Dim inventory As INVENTORY = FindInventory(storageLocation, SKU)
                    If inventory IsNot Nothing Then
                        newForecast.CHANGE_QTY = inventory.QTY - forecastQty - nextForecastQty
                        newForecast.FORECAST_DATE = row.Item("Date")
                        newForecast.LOCATION = storageLocation
                        newForecast.SKU_NAME = SKU
                    End If
                    If newForecast.CHANGE_QTY > 0 Then
                        positiveShuffleLoc.Add(newForecast)
                    Else
                        negativeShuffleLoc.Add(newForecast)
                    End If
                    row.Item("SKU") = "NONE"
                Next

                For Each negativeLocation As ShuffleArrayClass In negativeShuffleLoc
                    Dim locationToRemove As List(Of ShuffleArrayClass) = New List(Of ShuffleArrayClass)
                    While negativeLocation.CHANGE_QTY < 0
                        Dim recLocation As ShuffleArrayClass = positiveShuffleLoc.Where(Function(p) p.SKU_NAME = negativeLocation.SKU_NAME _
                                                                                           AndAlso p.LOCATION <> negativeLocation.LOCATION AndAlso p.CHANGE_QTY > 0).FirstOrDefault
                        If recLocation Is Nothing Then
                            reshuffledDatatable.Rows.Add(negativeLocation.SKU_NAME, negativeLocation.LOCATION, "Order from DC", "Order From DC", negativeLocation.CHANGE_QTY, negativeLocation.FORECAST_DATE.ToString(My.Settings.DATE_FORMAT))
                            Exit While
                        End If
                        Dim shuffleQty As Integer = If(recLocation.CHANGE_QTY > Math.Abs(negativeLocation.CHANGE_QTY), Math.Abs(negativeLocation.CHANGE_QTY), recLocation.CHANGE_QTY)
                        negativeLocation.CHANGE_QTY += shuffleQty
                        locationToRemove.Add(recLocation)
                        Dim recShuffleLoc As New ShufflingClass
                        recShuffleLoc.REC_FROM_LOC = recLocation.LOCATION
                        recShuffleLoc.LOCATION = negativeLocation.LOCATION
                        recShuffleLoc.QTY = shuffleQty
                        recShuffleLoc.ShuffleDate = recLocation.FORECAST_DATE
                        recShuffleLoc.SKU_NAME = recLocation.SKU_NAME
                        shuffledArray.Add(recShuffleLoc)
                        UpdateInventoryQty(recShuffleLoc.LOCATION, recShuffleLoc.SKU_NAME, shuffleQty)
                        Dim sendShuffleLoc As New ShufflingClass
                        sendShuffleLoc.SEND_TO_LOC = negativeLocation.LOCATION
                        sendShuffleLoc.LOCATION = recLocation.LOCATION
                        sendShuffleLoc.QTY = shuffleQty
                        sendShuffleLoc.ShuffleDate = negativeLocation.FORECAST_DATE
                        sendShuffleLoc.SKU_NAME = negativeLocation.SKU_NAME
                        shuffledArray.Add(sendShuffleLoc)
                        UpdateInventoryQty(sendShuffleLoc.LOCATION, sendShuffleLoc.SKU_NAME, -shuffleQty)
                        recLocation.CHANGE_QTY = 0
                        positiveShuffleLoc.Remove(recLocation)
                    End While
                Next
            Next


            For Each shuffledItem As ShufflingClass In shuffledArray
                If shuffledItem.REC_FROM_LOC IsNot Nothing Then
                    reshuffledDatatable.Rows.Add(shuffledItem.SKU_NAME, shuffledItem.LOCATION, shuffledItem.REC_FROM_LOC, "N.A", shuffledItem.QTY, shuffledItem.ShuffleDate.ToString(My.Settings.DATE_FORMAT))
                Else
                    reshuffledDatatable.Rows.Add(shuffledItem.SKU_NAME, shuffledItem.LOCATION, "N.A", shuffledItem.SEND_TO_LOC, shuffledItem.QTY, shuffledItem.ShuffleDate.ToString(My.Settings.DATE_FORMAT))
                End If
            Next
            Application.Current.Dispatcher.BeginInvoke(Sub()
                                                           TestDataGrid.ItemsSource = reshuffledDatatable.AsDataView
                                                       End Sub)

        Catch ex As Exception
            CallbackWriteToConsoleOutput(ex.ToString)
        Finally
            CallbackToggleProgressBar()
            CallbackWriteToConsoleOutput("Shuffling Completed")
        End Try
        'Try
        '    Dim KMEntity As KONICA_MINOLTA_DBEntities = DatabaseService.GetDatabase
        '    Dim demandList = KMEntity.DEMANDs.ToList
        '    Dim sourceTable As DataTable = New DataTable()

        '    sourceTable.Columns.AddRange(New DataColumn() {
        '    New DataColumn("Order.Quantity", GetType(String)),
        '    New DataColumn("Storage.Location", GetType(String)),
        '    New DataColumn("Material", GetType(String)),
        '    New DataColumn("Document.Date", GetType(String))
        '})

        '    For Each demand In demandList
        '        Dim dateTime As Date = demand.DATE_TIME
        '        Dim dateString = dateTime.ToString("dd/MM/yyyy")
        '        sourceTable.Rows.Add(demand.ORDER_DETAILS.ORDER_QTY, demand.LOCATION.LOCATION_CODE, demand.SKU.NAME, dateString)
        '    Next

        '    Dim fileLocation = Environment.CurrentDirectory + "\SQLDataFrame.csv"
        '    Using writer As StreamWriter = New StreamWriter(fileLocation)
        '        Rfc4180Writer.WriteDataTable(sourceTable, writer, True)
        '    End Using
        '    MsgBox("Imported to CSV", MsgBoxStyle.Information)
        '    RService.Test2(fileLocation)
        'Catch ex As Exception

        'End Try
    End Sub

    Private Function FindInventory(ByVal location As Integer, ByVal sku As String) As INVENTORY
        Dim inventory As INVENTORY = Nothing
        inventory = KMEntity.INVENTORies.Where(Function(p) p.LOCATION.LOCATION_CODE = location AndAlso p.SKU.NAME = sku).FirstOrDefault
        Return inventory
    End Function

    Private Sub UpdateInventoryQty(ByVal location As Integer, ByVal sku As String, ByVal qty As Integer)
        Dim inventory As INVENTORY = Nothing
        inventory = KMEntity.INVENTORies.Where(Function(p) p.LOCATION.LOCATION_CODE = location AndAlso p.SKU.NAME = sku).FirstOrDefault
        If inventory IsNot Nothing Then
            inventory.QTY += qty
        End If
    End Sub

    Private Function CheckForNumericInput() As Boolean
        If IsNumeric(shuffleSkuPeriod.Text) = False Then
            Return False
        End If
        'If IsNumeric(shuffleSkuNumber.Text) = False Then
        '    Return False
        'End If
        Return True
    End Function

    Private Sub ReshufflingView_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        reshuffleDataTable.Columns.Add("SKU", GetType(String))
        reshuffleDataTable.Columns.Add("Location Code", GetType(String))
        Dim outgoingColumn As DataColumn = reshuffleDataTable.Columns.Add("Is Outgoing", GetType(Boolean))
        outgoingColumn.ReadOnly = True
        Dim incomingColumn As DataColumn = reshuffleDataTable.Columns.Add("Is Incoming", GetType(Boolean))
        incomingColumn.ReadOnly = True
        reshuffleDataTable.Columns.Add("January")
        reshuffleDataTable.Columns.Add("February")
        reshuffleDataTable.Columns.Add("March")
        reshuffleDataTable.Columns.Add("Confirm?", GetType(Boolean))
        reshuffleDataTable.Rows.Add("AX0123", "1001", True, False, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, True)
        reshuffleDataTable.Rows.Add("AX0124", "1005", False, True, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, True)
        reshuffleDataTable.Rows.Add("AX0125", "2003", True, False, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, True)
        reshuffleDataTable.Rows.Add("AX0126", "1006", False, True, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, True)
        reshuffleDataTable.Rows.Add("AX0127", "2009", True, False, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, Int(Rnd() * 100) + 1, True)
        TestDataGrid.ItemsSource = reshuffleDataTable.DefaultView



    End Sub

    Private Sub updateInventory_Click(sender As Object, e As RoutedEventArgs)
        Dim fileUpLoadDialog As New Forms.OpenFileDialog()
        fileUpLoadDialog.Filter = "All Files (*.*)|*.*"
        fileUpLoadDialog.Multiselect = False
        Dim fileUploadResult As String = fileUpLoadDialog.ShowDialog.ToString
        fileUploadName = fileUpLoadDialog.FileName
        If fileUploadResult.ToUpper.Equals("OK") Then
            Try
                Dim thread As System.Threading.Thread
                thread = New System.Threading.Thread(AddressOf UpdateInventoryData)
                thread.Start()
            Catch ex As Exception

            End Try
        End If
    End Sub

    Private Sub UpdateInventoryData()
        CallbackToggleProgressBar()
        inventoryDt = ExcelFunctions.ImportExceltoDatatable(fileUploadName)
        KMEntity.Database.ExecuteSqlCommand("TRUNCATE TABLE [INVENTORY]")
        CallbackWriteToConsoleOutput("Beginning to update inventory.")
        Try
        For Each row As DataRow In inventoryDt.Rows
                If String.IsNullOrEmpty(row.Item(0).ToString) = False Then
                    Dim inventory As INVENTORY = New INVENTORY
                    Dim index = 0
                    Dim skuName = row.Item(My.Settings.MATERIAL).ToString
                    Dim existingSku = KMEntity.SKUs.Where(Function(p) p.NAME = skuName).FirstOrDefault
                    If existingSku IsNot Nothing Then
                        inventory.SKU_LINK_ID = existingSku.ID
                    Else
                        Continue For
                    End If
                    If IsNumeric(row.Item(My.Settings.LOCATION)) = False Then
                        Continue For
                    End If
                    Dim locationName As Integer = row.Item(My.Settings.LOCATION)
                    Dim existingLocation = KMEntity.LOCATIONs.Where(Function(p) p.LOCATION_CODE = locationName).FirstOrDefault
                    If existingLocation IsNot Nothing Then
                        inventory.LOCATION_LINK_ID = existingLocation.ID
                    Else
                        Continue For
                    End If
                    inventory.QTY = row.Item(My.Settings.INVENTORY_QTY).ToString
                    If inventory.SKU_LINK_ID IsNot Nothing AndAlso inventory.LOCATION_LINK_ID IsNot Nothing Then
                        KMEntity.INVENTORies.Add(inventory)
                        KMEntity.SaveChanges()
                    Else
                    End If
                    'While index < row.Table.Columns.Count
                    '    Select Case row.Table.Columns(index).ColumnName
                    '        Case My.Settings.MATERIAL
                    '            Dim skuName = row.Item(index).ToString
                    '            Dim existingSku = KMEntity.SKUs.Where(Function(p) p.NAME = skuName).FirstOrDefault
                    '            If existingSku IsNot Nothing Then
                    '                inventory.SKU_LINK_ID = existingSku.ID
                    '            End If
                    '        Case My.Settings.LOCATION
                    '            If IsNumeric(row.Item(index)) = False Then
                    '                Continue For
                    '            End If

                    '        Case My.Settings.INVENTORY_QTY
                    '            If IsNumeric(row.Item(index)) = False Then
                    '                Continue For
                    '            End If
                    '            inventory.QTY = row.Item(index).ToString
                    '    End Select
                    '    If inventory.SKU_LINK_ID IsNot Nothing AndAlso inventory.LOCATION_LINK_ID IsNot Nothing Then
                    '        KMEntity.INVENTORies.Add(inventory)
                    '        KMEntity.SaveChanges()
                    '    Else
                    '    End If
                    '    index += 1
                    'End While
                End If
            Next
        Catch ex As Exception
            CallbackWriteToConsoleOutput(ex.ToString)
        Finally
            CallbackWriteToConsoleOutput(String.Format("Update Finished. Inventory Count = {0}", KMEntity.INVENTORies.Count))
            CallbackToggleProgressBar()
        End Try

    End Sub
End Class

Public Class Rfc4180Writer
    Public Shared Sub WriteDataTable(ByVal sourceTable As DataTable,
        ByVal writer As TextWriter, ByVal includeHeaders As Boolean)
        If (includeHeaders) Then
            Dim headerValues As IEnumerable(Of String) = sourceTable.Columns.OfType(Of DataColumn).Select(Function(column) QuoteValue(column.ColumnName))

            writer.WriteLine(String.Join(",", headerValues))
        End If

        Dim items As IEnumerable(Of String) = Nothing
        For Each row As DataRow In sourceTable.Rows
            items = row.ItemArray.Select(Function(obj) QuoteValue(If(obj?.ToString(), String.Empty)))
            writer.WriteLine(String.Join(",", items))
        Next

        writer.Flush()
    End Sub

    Private Shared Function QuoteValue(ByVal value As String) As String
        Return String.Concat("""", value.Replace("""", """"""), """")
    End Function
End Class

Public Class ShufflingClass
    Public Property REC_FROM_LOC As Integer?
    Public Property SEND_TO_LOC As Integer?
    Public Property LOCATION As Integer
    Public Property SKU_NAME As String
    Public Property QTY As Integer
    Public Property ShuffleDate As Date
End Class

Public Class ShuffleArrayClass
    Public Property FORECAST_DATE As Date
    Public Property LOCATION As Integer
    Public Property SKU_NAME As String
    Public Property CHANGE_QTY As Integer
End Class
