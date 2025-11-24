-- SQL Script to add customer fields to Orders table
-- Run this script on your SQL Server database
-- Database name: AsentyxPOS (or your actual database name)

USE [AsentyxPOS]
GO

-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'CustomerFullName')
BEGIN
    ALTER TABLE [dbo].[Orders]
    ADD CustomerFullName NVARCHAR(255) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'CustomerPhone')
BEGIN
    ALTER TABLE [dbo].[Orders]
    ADD CustomerPhone NVARCHAR(20) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'CustomerAddress')
BEGIN
    ALTER TABLE [dbo].[Orders]
    ADD CustomerAddress NVARCHAR(500) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'CustomerEmail')
BEGIN
    ALTER TABLE [dbo].[Orders]
    ADD CustomerEmail NVARCHAR(255) NULL
END
GO

PRINT 'Customer fields added successfully to Orders table'
GO

