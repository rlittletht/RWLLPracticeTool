USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_ReservedByDate]    Script Date: 2/23/2020 10:30:05 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER proc [dbo].[usp_ReservedByDate]
(
@TeamName varchar(50),
@Date varchar(10),
@FieldType varchar(10)
) as
Set nocount on

DECLARE @Division varchar(10),
@SQL varchar(2000),
@fieldPerDay int,
@fieldPerWeek int,
@reservedInThisWeek int,
@reservedInThisDay int,
@currentWeekNumber int,
@teamFieldsReleaseCount int = 0,
@teamCagesReleaseCount int = 0,
@ReleasedFieldsToday int,
@ReleasedCagesToday int,
@skipPerDayRule int = 0 -- 1 - skip rile, 0 - use rule

SELECT @Division = division from rwllTeams where TeamName = @TeamName

IF @FieldType = 'Field'
BEGIN
SELECT @teamFieldsReleaseCount =  FieldsReleaseCount, @ReleasedFieldsToday = ReleasedFieldsToday FROM rwllteams WHERE teamname = @TeamName
SELECT @fieldPerDay = FieldReservationsPerDay from rwlldivisions where DivisionName = @Division
SELECT @fieldPerWeek = FieldReservationsPerWeek from rwlldivisions where DivisionName = @Division
--SET @fieldPerDay = @fieldPerDay +@teamFieldsReleaseCount
--SET @fieldPerWeek = @fieldPerWeek +@teamFieldsReleaseCount
END
ELSE
BEGIN
SELECT @teamCagesReleaseCount =  CagesReleaseCount,@ReleasedCagesToday = ReleasedCagesToday FROM rwllteams WHERE teamname = @TeamName
SELECT @fieldPerDay = CageReservationsPerDay from rwlldivisions where DivisionName = @Division
SELECT @fieldPerWeek = CageReservationsPerWeek from rwlldivisions where DivisionName = @Division
--SET @fieldPerDay = @fieldPerDay +@teamCagesReleaseCount
--SET @fieldPerWeek = @fieldPerWeek +@teamCagesReleaseCount
END

IF (@teamFieldsReleaseCount > 0) OR (@teamCagesReleaseCount > 0) OR (@ReleasedFieldsToday > 0) OR (@ReleasedCagesToday > 0)
BEGIN
SET @skipPerDayRule = 1
END

-- Reserved in selected day
SELECT @reservedInThisDay = Count(*) FROM rwllpractice
Where Reserved = @TeamName and convert(varchar, ReserveDatetime, 101)  = convert(varchar, DateAdd(hh, -8, getdate()), 101) and [Type] = @FieldType

--Current week number
SELECT TOP(1)@currentWeekNumber = [Week] FROM rwllpractice
WHERE [Date] = @Date

-- Reserved in selected week
SELECT @reservedInThisWeek = COUNT(*) FROM rwllpractice
WHERE Reserved = @TeamName and [Week]  = @currentWeekNumber and [Type] = @FieldType

IF @reservedInThisWeek < @fieldPerWeek
BEGIN
IF (@skipPerDayRule = 1) OR (@reservedInThisDay < @fieldPerDay)
BEGIN
RETURN 0
END
ELSE
BEGIN
RETURN 1
END
END
ELSE
BEGIN
RETURN 1
END

GO

