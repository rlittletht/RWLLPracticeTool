USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[usp_PopulateFieldList]    Script Date: 2/23/2020 10:31:47 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE proc [dbo].[usp_PopulateFieldList]
AS
BEGIN
SELECT DISTINCT Field
From rwllpractice
ORDER BY Field
END

GO

