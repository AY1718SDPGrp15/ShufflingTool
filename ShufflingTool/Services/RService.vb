Imports RDotNet

Module RService

    Public Sub Test()
        Try
            Dim REngine As REngine = REngine.GetInstance()
            REngine.Initialize()
            Dim directory = System.IO.Directory.GetCurrentDirectory

            REngine.Evaluate("source('C:/Users/pc/Desktop/test.R')")
            Dim a = REngine.Evaluate("testFunction('hello')").AsVector
            MsgBox(a.First)
        Catch ex As Exception

        End Try

    End Sub

End Module
