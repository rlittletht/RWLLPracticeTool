USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_PopulateFieldList]    Script Date: 2/23/2020 10:29:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER proc [dbo].[usp_PopulateFieldList]
AS
BEGIN
SELECT DISTINCT Field
From rwllpractice
ORDER BY Field
END

GO

