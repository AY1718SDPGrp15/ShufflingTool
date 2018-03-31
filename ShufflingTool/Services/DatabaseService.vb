Module DatabaseService

    Private Property KMEntity As KONICA_MINOLTA_DBEntities

    Public Function GetDatabase()
        If KMEntity Is Nothing Then
            KMEntity = New KONICA_MINOLTA_DBEntities
        End If
        Return KMEntity
    End Function

    Public Sub ClearEntity()
        If KMEntity IsNot Nothing Then
            KMEntity.Dispose()
            KMEntity = Nothing
        End If
    End Sub
End Module
