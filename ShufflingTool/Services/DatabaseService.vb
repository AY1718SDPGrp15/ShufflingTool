Module DatabaseService

    Private Property KMEntity As KONICA_MINOLTA_DBEntities

    Public Function GetDatabase()
        If KMEntity Is Nothing Then
            KMEntity = New KONICA_MINOLTA_DBEntities
        End If
        Return KMEntity
    End Function

End Module
