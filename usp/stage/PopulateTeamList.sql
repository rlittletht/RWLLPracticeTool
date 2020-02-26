USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_PopulateTeamList]    Script Date: 2/23/2020 10:29:28 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROC [dbo].[usp_PopulateTeamList]
AS
SELECT  TeamName
FROM rwllteams
ORDER BY Division, TeamName
GO

