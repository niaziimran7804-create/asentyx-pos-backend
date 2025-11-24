-- SQL Script to create OrderHistory table
-- Run this script on your SQL Server database

USE [AsentyxPOS]
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderHistories')
BEGIN
    CREATE TABLE [dbo].[OrderHistories] (
        [OrderHistoryId] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [PreviousStatus] NVARCHAR(50) NULL,
        [NewStatus] NVARCHAR(50) NULL,
        [PreviousOrderStatus] NVARCHAR(50) NULL,
        [NewOrderStatus] NVARCHAR(50) NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [Notes] NVARCHAR(500) NULL,
        [ChangedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_OrderHistories] PRIMARY KEY ([OrderHistoryId]),
        CONSTRAINT [FK_OrderHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_OrderHistories_OrderId] ON [dbo].[OrderHistories] ([OrderId]);
    CREATE INDEX [IX_OrderHistories_UserId] ON [dbo].[OrderHistories] ([UserId]);
    CREATE INDEX [IX_OrderHistories_ChangedDate] ON [dbo].[OrderHistories] ([ChangedDate]);
END
GO

PRINT 'OrderHistories table created successfully.'
GO

