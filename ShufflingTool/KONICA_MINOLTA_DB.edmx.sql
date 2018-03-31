
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 03/31/2018 14:55:42
-- Generated from EDMX file: C:\Users\pc\Documents\Visual Studio 2017\Projects\ShufflingTool\ShufflingTool\KONICA_MINOLTA_DB.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [KONICA_MINOLTA_DB];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_DEMAND_LOCATION_DEMAND]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DEMAND] DROP CONSTRAINT [FK_DEMAND_LOCATION_DEMAND];
GO
IF OBJECT_ID(N'[dbo].[FK_DEMAND_ORDER_DETAILS_DEMAND]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DEMAND] DROP CONSTRAINT [FK_DEMAND_ORDER_DETAILS_DEMAND];
GO
IF OBJECT_ID(N'[dbo].[FK_DEMAND_SKU_DEMAND]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DEMAND] DROP CONSTRAINT [FK_DEMAND_SKU_DEMAND];
GO
IF OBJECT_ID(N'[dbo].[FK_INVENTORY_LOCATION_INVENTORY]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[INVENTORY] DROP CONSTRAINT [FK_INVENTORY_LOCATION_INVENTORY];
GO
IF OBJECT_ID(N'[dbo].[FK_INVENTORY_SKU_INVENTORY]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[INVENTORY] DROP CONSTRAINT [FK_INVENTORY_SKU_INVENTORY];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[DEMAND]', 'U') IS NOT NULL
    DROP TABLE [dbo].[DEMAND];
GO
IF OBJECT_ID(N'[dbo].[INVENTORY]', 'U') IS NOT NULL
    DROP TABLE [dbo].[INVENTORY];
GO
IF OBJECT_ID(N'[dbo].[LOCATION]', 'U') IS NOT NULL
    DROP TABLE [dbo].[LOCATION];
GO
IF OBJECT_ID(N'[dbo].[ORDER_DETAILS]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ORDER_DETAILS];
GO
IF OBJECT_ID(N'[dbo].[SKU]', 'U') IS NOT NULL
    DROP TABLE [dbo].[SKU];
GO
IF OBJECT_ID(N'[KONICA_MINOLTA_DBModelStoreContainer].[LAST_DEMAND_DATA]', 'U') IS NOT NULL
    DROP TABLE [KONICA_MINOLTA_DBModelStoreContainer].[LAST_DEMAND_DATA];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'DEMANDs'
CREATE TABLE [dbo].[DEMANDs] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [DATE_TIME] datetime  NULL,
    [SKU_LINK_ID] int  NULL,
    [LOCATION_LINK_ID] int  NULL,
    [ORDER_DETAILS_LINK_ID] int  NULL
);
GO

-- Creating table 'LOCATIONs'
CREATE TABLE [dbo].[LOCATIONs] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [COUNTRY_NAME] nvarchar(max)  NULL,
    [BRANCH_NAME] nvarchar(max)  NULL,
    [LOCATION_CODE] int  NULL
);
GO

-- Creating table 'ORDER_DETAILS'
CREATE TABLE [dbo].[ORDER_DETAILS] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [NAME] nvarchar(max)  NULL,
    [TYPE] nvarchar(max)  NULL,
    [ORDER_QTY] int  NULL
);
GO

-- Creating table 'SKUs'
CREATE TABLE [dbo].[SKUs] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [NAME] nvarchar(max)  NULL,
    [PRICE] int  NULL
);
GO

-- Creating table 'INVENTORies'
CREATE TABLE [dbo].[INVENTORies] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [LOCATION_LINK_ID] int  NULL,
    [SKU_LINK_ID] int  NULL,
    [QTY] int  NULL
);
GO

-- Creating table 'LAST_DEMAND_DATA'
CREATE TABLE [dbo].[LAST_DEMAND_DATA] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [DATE_TIME] datetime  NULL,
    [SKU_LINK_ID] int  NULL,
    [LOCATION_LINK_ID] int  NULL,
    [ORDER_DETAILS_LINK_ID] int  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [ID] in table 'DEMANDs'
ALTER TABLE [dbo].[DEMANDs]
ADD CONSTRAINT [PK_DEMANDs]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'LOCATIONs'
ALTER TABLE [dbo].[LOCATIONs]
ADD CONSTRAINT [PK_LOCATIONs]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'ORDER_DETAILS'
ALTER TABLE [dbo].[ORDER_DETAILS]
ADD CONSTRAINT [PK_ORDER_DETAILS]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'SKUs'
ALTER TABLE [dbo].[SKUs]
ADD CONSTRAINT [PK_SKUs]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'INVENTORies'
ALTER TABLE [dbo].[INVENTORies]
ADD CONSTRAINT [PK_INVENTORies]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'LAST_DEMAND_DATA'
ALTER TABLE [dbo].[LAST_DEMAND_DATA]
ADD CONSTRAINT [PK_LAST_DEMAND_DATA]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [LOCATION_LINK_ID] in table 'DEMANDs'
ALTER TABLE [dbo].[DEMANDs]
ADD CONSTRAINT [FK_DEMAND_LOCATION_DEMAND]
    FOREIGN KEY ([LOCATION_LINK_ID])
    REFERENCES [dbo].[LOCATIONs]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_DEMAND_LOCATION_DEMAND'
CREATE INDEX [IX_FK_DEMAND_LOCATION_DEMAND]
ON [dbo].[DEMANDs]
    ([LOCATION_LINK_ID]);
GO

-- Creating foreign key on [ORDER_DETAILS_LINK_ID] in table 'DEMANDs'
ALTER TABLE [dbo].[DEMANDs]
ADD CONSTRAINT [FK_DEMAND_ORDER_DETAILS_DEMAND]
    FOREIGN KEY ([ORDER_DETAILS_LINK_ID])
    REFERENCES [dbo].[ORDER_DETAILS]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_DEMAND_ORDER_DETAILS_DEMAND'
CREATE INDEX [IX_FK_DEMAND_ORDER_DETAILS_DEMAND]
ON [dbo].[DEMANDs]
    ([ORDER_DETAILS_LINK_ID]);
GO

-- Creating foreign key on [SKU_LINK_ID] in table 'DEMANDs'
ALTER TABLE [dbo].[DEMANDs]
ADD CONSTRAINT [FK_DEMAND_SKU_DEMAND]
    FOREIGN KEY ([SKU_LINK_ID])
    REFERENCES [dbo].[SKUs]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_DEMAND_SKU_DEMAND'
CREATE INDEX [IX_FK_DEMAND_SKU_DEMAND]
ON [dbo].[DEMANDs]
    ([SKU_LINK_ID]);
GO

-- Creating foreign key on [LOCATION_LINK_ID] in table 'INVENTORies'
ALTER TABLE [dbo].[INVENTORies]
ADD CONSTRAINT [FK_INVENTORY_LOCATION_INVENTORY]
    FOREIGN KEY ([LOCATION_LINK_ID])
    REFERENCES [dbo].[LOCATIONs]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_INVENTORY_LOCATION_INVENTORY'
CREATE INDEX [IX_FK_INVENTORY_LOCATION_INVENTORY]
ON [dbo].[INVENTORies]
    ([LOCATION_LINK_ID]);
GO

-- Creating foreign key on [SKU_LINK_ID] in table 'INVENTORies'
ALTER TABLE [dbo].[INVENTORies]
ADD CONSTRAINT [FK_INVENTORY_SKU_INVENTORY]
    FOREIGN KEY ([SKU_LINK_ID])
    REFERENCES [dbo].[SKUs]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_INVENTORY_SKU_INVENTORY'
CREATE INDEX [IX_FK_INVENTORY_SKU_INVENTORY]
ON [dbo].[INVENTORies]
    ([SKU_LINK_ID]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------