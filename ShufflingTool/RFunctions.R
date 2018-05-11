packages = c("dplyr","forecast","tsintermittent", "prophet", "zoo")  #name of required packages used in this rscript

package.check <- lapply(packages, FUN = function(x) {   #function to to check if each package is on the local machine, if a package is installed, it will be loaded, else install and load.
  if(!require(x, character.only = TRUE)) {
    install.packages(x, dependencies = TRUE)
    library(x, character.only = TRUE)
  }
})

formatData <- function(data) {   #fn to map data into appropriate format for manupulation
  data$Document.Date <- as.Date(as.character(data$Document.Date),"%m/%d/%Y") #this part check w bilguun default date format that is passed from SQL, should be dmy
  data$Order.Quantity<-as.numeric(as.character(data$Order.Quantity))
  return (data)
}

cleanData <- function (data) { #fn to extract relevant data and clean data, removing missing values
  data <- data[!(data$Material == "" | data$Storage.Location == "" | data$Order.Quantity < 0), ]
  data <- data[complete.cases(data),]
  return(data)
}

aggregateMonthlyData <- function(dataTable) {  #fn to aggregate demand by month, can be further developed to aggregate in diff ways e.g. not 1st to 31st to include lead time
  monthlyData <- aggregate(dataTable$Order.Quantity, by=list(Storage.Location = dataTable$Storage.Location, Material = dataTable$Material, Date = (substr(dataTable$Document.Date,1,7))), FUN=sum)
  colnames(monthlyData)[4] <- "Demand"
  return (monthlyData)
}

catByLocSKU <- function(dataTable){  #fn to categorise the entire data
  skuList <- split(dataTable, with(dataTable, interaction(Storage.Location,Material)), drop = TRUE)
  return (skuList)
}

createTimeLine <- function(dataTable) {           #fn to create continuous monthly time seq
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

createTimeSeriesObj <- function(sku, tl) {        #fn to create time series obj for demand forecast
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

getForecast <- function(timeseries, num) {  #fn to return forecast of the diff types of demand
  if (sum(timeseries == 0) < (length(timeseries))%/%2) {  #non-intermittent type
    fitA <- auto.arima(timeseries)          #auto.arima algo
    fcValuesA <- forecast(fitA, h = num)
    testA <- as.data.frame(fcValuesA)
    testA <- testA[c("Point Forecast")]
    
    fitE <- ets(timeseries)               #ets algo
    fcValuesE <- forecast(fitE, h = num)
    testE <- as.data.frame(fcValuesE)
    testE <- testE[c("Point Forecast")]
    
    dfP <- data.frame(ds = as.yearmon(time(timeseries)), y = timeseries)   #prophet algo
    fitP <- prophet(dfP)
    future <- make_future_dataframe(fitP, periods = num, freq = 'month')
    fcValuesP <- predict(fitP, tail(future, num))
    testP <- as.data.frame(fcValuesP)
    testP <- testP[c("yhat")]
    rownames(testP) <- as.yearmon(fcValuesP$ds)
    
    # fitN <- nnetar(tsclean(timeseries))    # can include nueral network forecast algo when there are more demand data, cant work currently as data is less than 2 periods 
    # fcValuesN <- forecast(fitN, h = 5)
    # testN <- as.data.frame(fcValuesN)
    # testN <- testN[c("Point Forecast")]
    
    consolidated <- cbind(testA,testE, testP)      #ave of the algo
    testAve <- as.data.frame(rowMeans(consolidated))
    return (testAve)
    
  } else {            #intermittent type
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

testAccuracy <- function (tsObj, startDate) {   #fn to return accuracy indicator
 
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
  
  accuracyInd <- tryCatch( 
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
        MAE <- computeMAE(as.data.frame(testset),testForecastAve)
        MASE <- computeMASE(testForecastAve[,1], as.vector(trainset), as.vector(testset))
        data.frame(MAE = MAE,MASE = MASE)
        
      } else if (sum(trainset == 0) > (length(trainset)-2)) {
        testForecastC <- croston(trainset, h = testSize)
        MAE <- computeMAE(as.data.frame(testset),as.data.frame(testForecastC$mean))
        MASE <- computeMASE(as.vector(testForecastC$mean), as.vector(trainset), as.vector(testset))
        data.frame(MAE = MAE, MASE = MASE)
        
      } else {
        testForecastCr <- crost(trainset, h = testSize)
        MAE <- computeMAE(as.data.frame(testset),as.data.frame(testForecastCr$frc.out))
        MASE <- computeMASE(as.vector(testForecastCr$frc.out), as.vector(trainset), as.vector(testset))
        data.frame(MAE = MAE, MASE = MASE)
      }
      
    }, error=function(cond) {
      return (NA)
    }, warning=function(cond) {
      return(NA)
    }
  )
  return (accuracyInd)
}


forecastDemand <- function(skuData, timeLine, period) {   #main fn to get consolidated forecast results
  allForecastTable <- data.frame()
  if (is.null(skuData) | length(skuData) == 0 | is.null(timeLine)) {
    return (NULL)
  } else {
    for (item in skuData) {
      demandTS <- createTimeSeriesObj(item, timeLine)
      if (!is.null(demandTS)) {
        forecastVal <- getForecast(demandTS, period) 
        firstDate <- c(substr(as.character(head(timeLine$date,1)),1,4), substr(as.character(head(timeLine$date,1)),6,7))
        accuracy <- testAccuracy(demandTS, firstDate)
        outputTable <- data.frame(Date = rownames(forecastVal), Location = item$Storage.Location[[1]], SKU = item$Material[[1]], PointForecast = unname(forecastVal), MAE= accuracy["MAE"], MASE = accuracy["MASE"])
        allForecastTable <- rbind(allForecastTable,outputTable)
      }
    }
    return (allForecastTable)
  }
}