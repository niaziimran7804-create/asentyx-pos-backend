-- SQL Script to add Invoice and ShopConfiguration tables
-- Run this script on your SQL Server database
-- Database name: AsentyxPOS (or your actual database name)

USE [AsentyxPOS]
GO

-- Create ShopConfigurations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ShopConfigurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ShopConfigurations](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ShopName] [nvarchar](255) NOT NULL,
        [ShopAddress] [nvarchar](500) NULL,
        [ShopPhone] [nvarchar](20) NULL,
        [ShopEmail] [nvarchar](255) NULL,
        [ShopWebsite] [nvarchar](100) NULL,
        [TaxId] [nvarchar](100) NULL,
        [FooterMessage] [nvarchar](500) NULL,
        [HeaderMessage] [nvarchar](500) NULL,
        [Logo] [varbinary](max) NULL,
        [UpdatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_ShopConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ShopConfigurations_Id] ON [dbo].[ShopConfigurations]([Id] ASC)
    
    -- Insert default shop configuration
    INSERT INTO [dbo].[ShopConfigurations] 
    ([ShopName], [ShopAddress], [ShopPhone], [ShopEmail], [FooterMessage], [UpdatedAt])
    VALUES 
    ('POS System', '123 Main Street', '+1 234 567 8900', 'info@possystem.com', 'Thank you for your business!', GETUTCDATE())
    
    PRINT 'ShopConfigurations table created successfully'
END
ELSE
BEGIN
    PRINT 'ShopConfigurations table already exists'
END
GO

-- Create Invoices table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Invoices](
        [InvoiceId] [int] IDENTITY(1,1) NOT NULL,
        [OrderId] [int] NOT NULL,
        [InvoiceNumber] [nvarchar](50) NOT NULL,
        [InvoiceDate] [datetime2](7) NOT NULL,
        [DueDate] [datetime2](7) NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED ([InvoiceId] ASC)
    )
    
    CREATE NONCLUSTERED INDEX [IX_Invoices_OrderId] ON [dbo].[Invoices]([OrderId] ASC)
    
    ALTER TABLE [dbo].[Invoices] WITH CHECK ADD CONSTRAINT [FK_Invoices_Orders_OrderId] 
    FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders] ([OrderId])
    
    ALTER TABLE [dbo].[Invoices] CHECK CONSTRAINT [FK_Invoices_Orders_OrderId]
    
    PRINT 'Invoices table created successfully'
END
ELSE
BEGIN
    PRINT 'Invoices table already exists'
END
GO

PRINT 'Invoice and ShopConfiguration tables setup completed'
GO

