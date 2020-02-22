USE [DB0902]
GO
/****** Object:  StoredProcedure [dbo].[usp_UpdateSlots]    Script Date: 03/20/2013 01:59:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




ALTER proc [dbo].[usp_UpdateSlots]
(@TranType varchar(3),
@Team  varchar(50),
@SlotNo  varchar(50)

) as
set nocount on
DECLARE @FieldType nvarchar(50),
@Division varchar(10),
@reservedInThisDay int,
@fieldPerDay int,
@cagesPerDay int,
@ReleasedFieldsToday int,
@ReleasedCagesToday int,
@ReleasedFieldsDate smalldatetime,
@ReleasedCagesDate smalldatetime

SELECT  @ReleasedFieldsToday = ReleasedFieldsToday,
@ReleasedCagesToday = ReleasedCagesToday,
@ReleasedFieldsDate = ReleasedFieldsDate,
@ReleasedCagesDate = ReleasedCagesDate
FROM rwllteams WHERE teamname = @team

SELECT @Division = division from rwllTeams where TeamName = @team
-- For fields
DECLARE @FieldsReleaseCount int
SELECT  @FieldsReleaseCount =  FieldsReleaseCount FROM rwllteams WHERE teamname = @team
SELECT  @fieldPerDay = FieldReservationsPerDay from rwlldivisions where DivisionName = @Division
-- For Cages
DECLARE @CagesReleaseCount int
SELECT  @CagesReleaseCount =  CagesReleaseCount FROM rwllteams WHERE teamname = @team
SELECT  @cagesPerDay = CageReservationsPerDay from rwlldivisions where DivisionName = @Division

If @TranType = 'Rel'
BEGIN

DECLARE @CurrentDate nvarchar(255) = convert(varchar, DateAdd(hh, -8, getdate()), 101)
DECLARE @ReservedFieldDate nvarchar(255)
DECLARE @DateOfReservation nvarchar(255)

IF (@ReleasedFieldsDate <> @CurrentDate)
BEGIN
UPDATE rwllteams SET FieldsReleaseCount = FieldsReleaseCount + @ReleasedFieldsToday WHERE teamname = @team
UPDATE rwllteams SET ReleasedFieldsDate = convert(varchar, DateAdd(hh, -8, getdate()), 101),ReleasedFieldsToday = 0  WHERE teamname = @team
END
IF (@ReleasedCagesDate <> @CurrentDate)
BEGIN
UPDATE rwllteams SET CagesReleaseCount = CagesReleaseCount + @ReleasedCagesToday WHERE teamname = @team
UPDATE rwllteams SET ReleasedCagesDate = convert(varchar, DateAdd(hh, -8, getdate()), 101),ReleasedCagesToday = 0  WHERE teamname = @team
END

SELECT  @FieldType = [Type], @ReservedFieldDate = convert(varchar,[Date], 101), @DateOfReservation = convert(varchar,ReserveDatetime, 101) FROM rwllpractice WHERE SlotNo = @SlotNo and Reserved <> 'Available'

IF @ReservedFieldDate > @CurrentDate
BEGIN
UPDATE rwllpractice SET Reserved = 'Available', ReserveDatetime = null, ReleaseDatetime = convert(varchar, DateAdd(hh, -8, getdate()), 101),ReleaseTeam = @team  WHERE SlotNo = @SlotNo and Reserved <> 'Available'

IF @FieldType = 'Field'
BEGIN
IF(@ReleasedFieldsToday = @fieldPerDay)
BEGIN
UPDATE rwllteams SET FieldsReleaseCount = FieldsReleaseCount + 1 WHERE teamname = @team
END
ELSE
BEGIN
UPDATE rwllteams SET ReleasedFieldsToday = ReleasedFieldsToday + 1 WHERE teamname = @team
END
END
ELSE
BEGIN
IF(@ReleasedCagesToday = @cagesPerDay)
BEGIN
UPDATE rwllteams SET CagesReleaseCount = CagesReleaseCount + 1 WHERE teamname = @team
END
ELSE
BEGIN
UPDATE rwllteams SET ReleasedCagesToday = ReleasedCagesToday + 1 WHERE teamname = @team
END
END

select 0
END
END
ELSE
BEGIN
UPDATE rwllpractice
SET Reserved = @Team, ReserveDatetime = DateAdd(hh, -8, getdate())
WHERE SlotNo = @SlotNo

SELECT @FieldType = [Type] FROM rwllpractice WHERE SlotNo = @SlotNo
-- Reserved in selected day
SELECT @reservedInThisDay = Count(*) FROM rwllpractice Where Reserved = @team and convert(varchar, ReserveDatetime, 101)  = convert(varchar, DateAdd(hh, -8, getdate()), 101) and [Type] = @FieldType


IF @FieldType = 'Field'
BEGIN
IF @FieldsReleaseCount > 0 AND (@fieldPerDay <= @reservedInThisDay)
BEGIN
UPDATE rwllteams SET FieldsReleaseCount = FieldsReleaseCount - 1 WHERE teamname = @team
END
ELSE
BEGIN
IF @ReleasedFieldsToday > 0
BEGIN
UPDATE rwllteams SET ReleasedFieldsToday = ReleasedFieldsToday - 1 WHERE teamname = @team
END
END
END
ELSE
BEGIN
IF @CagesReleaseCount > 0 AND (@cagesPerDay <= @reservedInThisDay)
BEGIN
UPDATE rwllteams SET CagesReleaseCount = CagesReleaseCount - 1 WHERE teamname = @team
END
ELSE
BEGIN
IF @ReleasedCagesToday > 0
BEGIN
UPDATE rwllteams SET ReleasedCagesToday = ReleasedCagesToday - 1 WHERE teamname = @team
END
END
END


select 0
END


--IF Exists (select * from rwllpractice
--where Venue = @Venue and Field = @Field and Date = @Date and StartTime = @StartTime and Reserved <> 'Available' and Reserved <> 'HOLD')
--Begin
--Select -1
--GoTo EndProc
--End

--CheckAdmin:

--IF @Team in ('Administrator','Baseball Scheduler','Softball Scheduler')
--or @TranType = 'ResAdmin'
--Begin
--GoTo AdminBypass
--End

--CheckDailyLimit:


--declare @startDate varchar(50)
--declare @endDate varchar(50)
--declare @dt datetime
--select @dt = getdate()

--IF @dt between convert(varchar, getdate(), 101)+' 00:00:00 AM' AND convert(varchar, getdate(), 101)+' 11:00:00 AM'
--begin

--SET  @startDate =  convert(varchar, getdate()-1, 101)+' 11:00:00 AM'
--SET  @endDate = convert(varchar, getdate(), 101)+' 11:00:00 AM'

--end
--else
--begin
--SET @startDate = convert(varchar, getdate(), 101)+' 11:00:00 AM'
--SET @endDate = convert(varchar, getdate()+1, 101)+' 11:00:00 AM'
--end

--SET @Count = 0
--Select @Count = count(*) from rwllpractice
--where Reserved = @Team
--and ReserveDatetime between @startDate and @endDate
----and datediff(hh,ReserveDatetime,getdate()) < 24

--SELECT @Count = @Count - ReleaseCount
--FROM rwllTeams where TeamName = @Team

--If @Count >= 2
--Begin
--Select -2
--GoTo EndProc
--End

--CheckPremiumLimit:
--------------------------------------------
-- team can only reserve one h5/h5a/h5b/h6/h6a/h6b per week
-- Must find out if they already have a reservation
-- for that week
-- SB Jrs, and BB/SB Majors, BB/SB Coast get 2/week


--select @week = week from rwllpractice where date = @Date

--SET @Count = 0
--Select @Count = count(*) from rwllpractice
--where Reserved = @Team
--and Field in ('H5','H6' )
--and week = @week


--IF @Division in ('A','B','G','H') and @Count > 1
--and @Field in ('H5','H6' )
--Begin
--Select -4
--GoTo EndProc
--END
--IF @Division not in ('A','B','G','H') and @Count > 0
--and @Field in ('H5','H6' )
--Begin
--Select -4
--GoTo EndProc
--END

--AdminBypass:

--Update rwllpractice
--set Reserved = @Team, ReserveDatetime = getdate()
--where Venue = @Venue and Field = @Field and Date = @Date and StartTime = @StartTime and ( Reserved = 'Available' or Reserved = 'HOLD' )
--If @@Error <> 0
--Select -3
--else
--Select 0

--- deduct the new reservation from ReleaseCount
--IF EXISTS (Select * from rwllTeams where ReleaseCount > 0 and TeamName = @Team)
--BEGIN
--Update rwllteams
--set ReleaseCount = ReleaseCount -1
--where teamname = @team
---- don't penalize team for re-acquiring a released slot
---- they still get two new slots / day
--Update rwllpractice
--set ReserveDatetime = dateadd(dd, -2, getdate() )
--where Venue = @Venue and Field = @Field and Date = @Date and StartTime = @StartTime
--END


EndProc:

