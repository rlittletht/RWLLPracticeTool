USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[usp_UpdateSlots]    Script Date: 2/23/2020 10:30:22 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/* Update information about a slot -- Reserve or Release it

*/
ALTER proc [dbo].[usp_UpdateSlots] (@TranType varchar(3),
									@Team  varchar(50),
									@SlotNo  varchar(50))
AS

SET nocount ON

DECLARE 
	@FieldType nvarchar(50),
	@Division varchar(10),
	@reservedInThisDay int,
	@fieldPerDay int,
	@cagesPerDay int,
	@ReleasedFieldsToday int,
	@ReleasedCagesToday int,
	@ReleasedFieldsDate smalldatetime,
	@ReleasedCagesDate smalldatetime

-- Get information about the team
SELECT  
	@ReleasedFieldsToday = ReleasedFieldsToday,
	@ReleasedCagesToday = ReleasedCagesToday,
	@ReleasedFieldsDate = ReleasedFieldsDate,
	@ReleasedCagesDate = ReleasedCagesDate
FROM 
	rwllteams 
WHERE 
	teamname = @team

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
		DECLARE @CurrentDate DateTime2 = GetUTCDate()
		DECLARE @ReservedFieldDate DateTime2 = GetUTCDate()
		DECLARE @DateOfReservation nvarchar(255)

		SELECT  @FieldType = [Type], @ReservedFieldDate = [SlotStart], @DateOfReservation = SlotReservedDatetime FROM rwllpractice WHERE SlotNo = @SlotNo and Reserved <> 'Available'

		IF DateDiff(minute, @CurrentDate, @ReservedFieldDate) > 0 -- if the reserved date > current date
			BEGIN
				UPDATE rwllpractice SET Reserved = 'Available', ReserveDatetime = null, SlotReleasedDatetime = @CurrentDate,ReleaseTeam = @team  WHERE SlotNo = @SlotNo and Reserved <> 'Available'

				SELECT 0
			END
	END
ELSE
	BEGIN
		UPDATE 
		   rwllpractice
		SET 
		   Reserved = @Team, SlotReservedDatetime = GetUTCDate()
		WHERE 
		   SlotNo = @SlotNo

		SELECT 0
	END

EndProc:

GO

