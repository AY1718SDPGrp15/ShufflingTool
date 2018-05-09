packages = c("dplyr","forecast","tsintermittent", "prophet", "zoo")  #name of required packages used in this rscript

package.check <- lapply(packages, FUN = function(x) {   #function to to check if each package is on the local machine, if a package is installed, it will be loaded, else install and load.
  if(!require(x, character.only = TRUE)) {
    install.packages(x, dependencies = TRUE)
    library(x, character.only = TRUE)
  }
})

formatData <- function(data) {
  data$Document.Date <- as.Date(as.character(data$Document.Date),"%d/%m/%Y") #this part check w bilguun default date format that is passed from SQL 
  data$Order.Quantity<-as.numeric(as.character(data$Order.Quantity))
  return (data)
}

cleanData <- function (data) {
  data <- data[!(data$Material == "" | data$Storage.Location == "" | data$Order.Quantity < 0), ]
  data <- data[complete.cases(data),]
  return(data)
}

aggregateMonthlyData <- function(dataTable) {
  monthlyData <- aggregate(dataTable$Order.Quantity, by=list(Storage.Location = dataTable$Storage.Location, Material = dataTable$Material, Date = (substr(dataTable$Document.Date,1,7))), FUN=sum)
  colnames(monthlyData)[4] <- "Demand"
  return (monthlyData)
}

catByLocSKU <- function(dataTable){
  skuList <- split(dataTable, with(dataTable, interaction(Storage.Location,Material)), drop = TRUE)
  return (skuList)
}

createTimeLine <- function(dataTable) {               #create continuous monthly time seq
  dataTable <- dataTable[order(dataTable$Date),]
  startDate <- as.character(head(dataTable$Date,1))
  startDate <- as.Date(paste0(startDate,"01"), "%Y-%m%d")
  endDate <- as.character(tail(monthlyData$Date,1))
  endDate <- as.Date(paste0(endDate,"01"), "%Y-%m%d")
  if (startDate <= endDate){
    timeLine <- data.frame(date = format(seq(startDate,endDate,by="month"), "%Y-%m"))
    return (timeLine)
  } else {
    return (NULL)
  }
}

createTimeSeriesObj <- function(sku, tl) {        #create time series obj for demand forecast
  recordedValues <- data.frame(date = sku$Date, values = sku$Demand)
  timeValSeq <- merge(x = tl, y = recordedValues, by = "date", all.x = TRUE)
  timeValSeq[is.na(timeValSeq)] <- 0
  firstOccur <- c(substr(as.character(head(tl$date,1)),1,4), substr(as.character(head(tl$date,1)),6,7))
  lastOccur <- c(substr(as.character(tail(tl$date,1)),1,4), substr(as.character(tail(tl$date,1)),6,7))
  tsObj <- tryCatch( 
    {
      ts(timeValSeq[, 2],start=as.numeric(firstOccur), end=as.numeric(lastOccur), frequency=12)
    }, error=function(cond) {
      return (NULL)
    }, warning=function(cond) {
      return (NULL)
    }
  )
  return (tsObj)
} 

getForecast <- function(timeseries, num) {
  if (sum(timeseries == 0) < (length(timeseries))%/%2) {
    fitA <- auto.arima(timeseries)
    fcValuesA <- forecast(fitA, h = num)
    testA <- as.data.frame(fcValuesA)
    testA <- testA[c("Point Forecast")]
    
    fitE <- ets(timeseries)
    fcValuesE <- forecast(fitE, h = num)
    testE <- as.data.frame(fcValuesE)
    testE <- testE[c("Point Forecast")]
    
    dfP <- data.frame(ds = as.yearmon(time(timeseries)), y = timeseries)
    fitP <- prophet(dfP)
    future <- make_future_dataframe(fitP, periods = num, freq = 'month')
    fcValuesP <- predict(fitP, tail(future, num))
    testP <- as.data.frame(fcValuesP)
    testP <- testP[c("yhat")]
    rownames(testP) <- as.yearmon(fcValuesP$ds)
    
    consolidated <- cbind(testA,testE, testP)
    testAve <- as.data.frame(rowMeans(consolidated))
    return (testAve)
    
  } else {
    fcValues <- croston(timeseries, h = num)$mean
    test <- as.data.frame(fcValues)
    rownames(test) <- as.yearmon(time(fcValues))
    return (test)
  } 
}

computeMAE <- function(actual, predicted) {
  error <- actual - predicted
  return(colMeans(abs(error)))
}

computeMASE <- function(forecast,train,test) {
  n <- length(train)
  scalingFactor <- sum(abs(train[2:n] - train[1:(n-1)])) / (n-1)
  et <- abs(test-forecast)
  qt <- et/scalingFactor
  meanMASE <- mean(qt)
  return(meanMASE)
}

testAccuracy <- function (tsObj, startDate) {
  accuracyInd <- data.frame(MAE = NA, y = NA)
  
  if(length(tsObj) <= 4) {
    return(NA)
    
  } else if(length(tsObj) <= 10) {
    trainSize <- length(tsObj) - 1
    testSize <- 1
    
  } else if(length(tsObj) <= 25) {
    trainSize <- length(tsObj) - 3
    testSize <- 3
    
  } else {
    trainSize <- length(tsObj) - 5
    testSize <- 5
  }
  
  MAE <- tryCatch( 
    {
      trainset <- ts(tsObj[1:trainSize],start=as.numeric(startDate),frequency = 12)
      testset <- tsObj[length(tsObj) - ((testSize-1):0)]
      
      if (sum(tsObj == 0) < (length(tsObj))%/%2) {
        testForecastA <- as.data.frame(forecast(auto.arima(trainset),h=testSize))
        testForecastA <- testForecastA[c("Point Forecast")]
        testForecastS <- as.data.frame(forecast(ets(trainset),h=testSize))
        testForecastS <- testForecastS[c("Point Forecast")]
        testdfP <- data.frame(ds = as.yearmon(time(trainset)), y = trainset)
        testFitP <- prophet(testdfP)
        testFuture <- make_future_dataframe(testFitP, periods = testSize, freq = 'month')
        testValP <- predict(testFitP, tail(testFuture, testSize))
        testForecastP <- as.data.frame(testValP)
        testForecastP <- testForecastP[c("yhat")]
        rownames(testForecastP) <- as.yearmon(testValP$ds)
        testForecastAve <- cbind(testForecastA,testForecastS,testForecastP)
        testForecastAve <- as.data.frame(rowMeans(testForecastAve))
        computeMAE(as.data.frame(testset),testForecastAve)
        # computeMASE(testForecastAve[,1], as.vector(trainset), as.vector(testset))
        
      } else if (sum(trainset == 0) > (length(trainset)-2)) {
        testForecastC <- croston(trainset, h = testSize)
        accuracy(testForecastC$mean,testset)[,"MAE"]
        #accuracy(testForecastC$mean,testset,d=1, D=0)[2,"MASE"]
        
      } else {
        testForecastC <- crost(trainset, h = testSize)
        accuracy(testForecastC$frc.out,testset)[,"MAE"]
        #accuracy(testForecastC$frc.out,testset,d=1, D=0)[2,"MASE"]
      }
      
    }, error=function(cond) {
      return (NA)
    }, warning=function(cond) {
      return(NA)
    }
  )
  return (MAE)
}


forecastDemand <- function(skuData, timeLine, period) {
  allForecastTable <- data.frame()
  if (is.null(skuData) | length(skuData) == 0 | is.null(timeLine)) {
    return (NULL)
  } else {
    for (item in skuData) {
      demandTS <- createTimeSeriesObj(item, timeLine)
      if (!is.null(demandTS)) {
        forecastVal <- getForecast(demandTS, period) 
        firstDate <- c(substr(as.character(head(timeLine$date,1)),1,4), substr(as.character(head(timeLine$date,1)),6,7))
        MAE <- testAccuracy(demandTS, firstDate)
        outputTable <- data.frame(Date = rownames(forecastVal), Location = item$Storage.Location[[1]], SKU = item$Material[[1]], PointForecast = unname(forecastVal), MAE = MAE)
        allForecastTable <- rbind(allForecastTable,outputTable)
      }
    }
    return (allForecastTable)
  }
}