Imports System.Data
Imports RDotNet

Module RService

    Public Property REngine As REngine = REngine.GetInstance()

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

    Public Function Test2(filePath As String, forecastPeriod As String) As DataFrame
        CallbackWriteToConsoleOutput("Starting R Script")
        REngine.Initialize()
        Dim directory = System.IO.Directory.GetCurrentDirectory.ToString.Replace("\", "/") + "/RFunctions.R"
        REngine.Evaluate(String.Format("source('{0}')", directory))
        REngine.Evaluate(String.Format("newData <- read.csv('{0}')", filePath.Replace("\", "/")))
        REngine.Evaluate("newData <- formatData(newData)")
        REngine.Evaluate("newData <- cleanData(newData)")
        REngine.Evaluate("monthlyData <- aggregateMonthlyData(newData)")
        REngine.Evaluate("skuList <- catByLocSKU(monthlyData)")
        'REngine.Evaluate("skuList <- skuList[c(1:200)]")
        REngine.Evaluate("timeLine <- createTimeLine(monthlyData)")
        REngine.Evaluate(String.Format("forecastResult <- forecastDemand(skuList, timeLine, {0})", forecastPeriod + 1))
        Dim forecastResult As DataFrame = REngine.GetSymbol("forecastResult").AsDataFrame()
        Return forecastResult
    End Function



End Module
