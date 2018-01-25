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
                '     PopulateSQLFromDatabase(dt)
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub PopulateSQLFromDatabase(dt As DataTable)
        Dim rows = dt.Rows
        Dim columns = dt.Columns
        Dim KMEntity As KONICA_MINOLTA_DBEntities = DatabaseService.GetDatabase
        Dim count = 0
        For Each row As DataRow In rows
            If String.IsNullOrEmpty(row.Item(0).ToString) = False Then
                Dim demand As DEMAND = New DEMAND
                Dim order As ORDER_DETAILS = New ORDER_DETAILS
                Dim SKU As SKU = New SKU
                Dim location As New LOCATION
                Dim index = 0
                While index < row.Table.Columns.Count
                    Select Case row.Table.Columns(index).ColumnName
                        Case "Purchasing Document"
                            order.TYPE = row.Item(index).ToString
                        Case "Short Text"
                            order.NAME = row.Item(index).ToString
                        Case "Material"
                            SKU.NAME = row.Item(index).ToString
                        Case "Document Date"
                            demand.DATE_TIME = row.Item(index).ToString
                        Case "Storage Location"
                            location.LOCATION_CODE = row.Item(index).ToString
                        Case "Order Quantity"
                            order.ORDER_QTY = row.Item(index).ToString
                        Case "Plant"
                            location.BRANCH_NAME = row.Item(index).ToString
                            location.COUNTRY_NAME = "THAILAND"
                    End Select
                    index += 1
                End While
                KMEntity.SKUs.Add(SKU)
                KMEntity.LOCATIONs.Add(location)
                KMEntity.ORDER_DETAILS.Add(order)
                KMEntity.SaveChanges()
                demand.LOCATION_LINK_ID = location.ID
                demand.ORDER_DETAILS_LINK_ID = order.ID
                demand.SKU_LINK_ID = SKU.ID
                KMEntity.DEMANDs.Add(demand)
                KMEntity.SaveChanges()
            End If
        Next
    End Sub
End Class
