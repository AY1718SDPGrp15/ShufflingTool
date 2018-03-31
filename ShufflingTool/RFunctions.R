packages = c("dplyr","forecast","tsintermittent", "prophet")  #name of required packages used in this rscript

package.check <- lapply(packages, FUN = function(x) {   #function to to check if each package is on the local machine, if a package is installed, it will be loaded, else install and load.
  if(!require(x, character.only = TRUE)) {
    install.packages(x, dependencies = TRUE)
    library(x, character.only = TRUE)
  }
})

formatData <- function(data) {
  data$Document.Date <- as.Date(as.character(data$Document.Date),"%m/%d/%Y") #this part check w bilguun default date format that is passed 
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
    test <- as.data.frame(fcValuesA)
    test <- test[c("Point Forecast")]
    return (test)
    
  } else if (sum(timeseries == 0) > (length(timeseries)-2)) {
    fcValues <- croston(timeseries, h = num)$mean
    test <- as.data.frame(fcValues)
    rownames(test) <- as.yearmon(time(fcValues))
    return (test)
    
  } else {
    fcValues <- crost(timeseries, h = num)$frc.out
    test <- as.data.frame(fcValues)
    rownames(test) <- as.yearmon(time(fcValues))
    return (test)
  }
}

testAccuracy <- function (tsObj, startDate) {
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
        testForecast <- forecast(auto.arima(trainset),h=testSize)
        accuracy(testForecast$mean,testset)[,"MAE"]
        
      } else if (sum(trainset == 0) > (length(trainset)-2)) {
        testForecast <- croston(trainset, h = testSize)
        accuracy(testForecast$mean,testset)[,"MAE"]
        
      } else {
        testForecast <- crost(trainset, h = testSize)
        accuracy(testForecast$frc.out,testset)[,"MAE"]
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