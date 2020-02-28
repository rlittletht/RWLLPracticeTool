USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_DisplaySlotsEx]    Script Date: 2/18/2014 9:40:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* Params: */
/* @TeamName - the name of the team (as stored in table [rwllteams] 
   @ShowSlots - 1: Show all slots reserved by TeamName
   				2: Show the slots that can be reserved by TeamName 
   @ShowDate - the date to show slots for
   @VenueName - the field to show info for
   @Sort	  - how should this be sorted? ("StartTime" | ... ) 
   @WindowStart- this is the UTC start of the window for considering
				the number of events reserved in a time period

   NOTE for UTC: ShowDate is the the first minute of the 24 hour
   period we want to consider. So if you want 2/2/2020 in PST (UTC-8), 
   then you want to pass in 2/2/2020 08:00:00 UTC+0. This way we capture
   (2/2/2020 08:00:00, 2/3/2020 08:00:00]. 
*/

ALTER PROC [dbo].[usp_DisplaySlotsEx] (@TeamName  VARCHAR(50), 
                                       @ShowSlots TINYINT, 
                                       @ShowDate  VARCHAR(32), 
                                       @VenueName NVARCHAR(255) = '', 
									   @WindowStart DateTime2,
                                       @Sort      VARCHAR(20) = 'StartTime') 
AS 

SET nocount ON 

DECLARE @Division    VARCHAR(10), 
        @FieldStatus INT, 
        @CageStatus  INT, 
        @SQL         VARCHAR(2000), 
		@SQLSuffix   VARCHAR(2000),
		@SQLPrefixDefaultDisabled VARCHAR(50),
		@SQLPrefixDefaultEnabled VARCHAR(50),
		@TeamNameSafe VARCHAR(75)


SELECT @TeamNameSafe = REPLACE(@TeamName, '''', '''''')

SELECT @SQLPrefixDefaultDisabled = 'SELECT ' + CHAR(39) + 'default state is disabled' + CHAR(39);
SELECT @SQLPrefixDefaultEnabled = 'SELECT ' + CHAR(39) + CHAR(39);

SELECT @SQLSuffix = ' as IsEnabled, SlotNo,Week,Status,Venue,Field,'
	+'SlotStart, '
	+'SlotLength, '
	+'Reserved,Divisions,'
	+'SlotReservedDatetime '
	+'from rwllpractice ' 

IF @TeamName NOT IN ( 'Administrator', 'Tonya Henry', 'ShowAll', '-- Select Team --', '-- Admin --', '-- Baseball --', '-- Softball --' ) 
	BEGIN 
		IF @ShowSlots = 1 
			BEGIN 
				SELECT @SQL = @SQLPrefixDefaultEnabled + @SQLSuffix

				SELECT @SQL = @SQL + ' where Reserved = ''' + @TeamNameSafe + '''' 
				SELECT @SQL = @SQL + ' order by ' + @Sort 

				--  SELECT @SQL 
				EXEC (@SQL) 
			END 
			
		IF @ShowSlots = 2 
			BEGIN 
				SELECT @Division = division FROM   rwllteams WHERE  teamname = @TeamName 

				--EXEC @FieldStatus =  usp_ReservedByDate @TeamName,@ShowDate, 'Field' 
				--EXEC @CageStatus  =  usp_ReservedByDate @TeamName,@ShowDate, 'Cage' 
				IF @VenueName = '' 
					BEGIN 
						SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Field', [field], @WindowStart) AS IsEnabled, 
							   slotno, [week], [status], venue, field, 
							   SlotStart, 
							   SlotLength, 
							   reserved, divisions, 
							   SlotReservedDateTime,
							   CONCAT(CONVERT(nvarchar(30), SlotStart, 112), ':', Venue, CONVERT(nvarchar(30), SlotStart, 114), ':', Field) AS SortKey
							FROM   rwllpractice 
							WHERE  reserved = 'Available' 
								AND DateDiff(minute, @ShowDate, SlotStart) > 0
								AND DateDiff(minute, @ShowDate, SlotStart) <= (60 * 24)
								AND [type] = 'Field' 
								AND Charindex(@Division, divisions) <> 0 
							UNION 
								SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Cage', [field], @WindowStart) AS IsEnabled, 
										slotno, [week], [status], venue, field, 
										SlotStart, 
										SlotLength, 
										reserved, divisions, 
										SlotReservedDateTime,
										CONCAT(CONVERT(nvarchar(30), SlotStart, 112), ':', Venue, CONVERT(nvarchar(30), SlotStart, 114), ':', Field) AS SortKey
								FROM   rwllpractice 
								WHERE  reserved = 'Available' 
									AND DateDiff(minute, @ShowDate, SlotStart) > 0
									AND DateDiff(minute, @ShowDate, SlotStart) <= (60 * 24)
									AND [type] = 'Cage' 
									AND Charindex(@Division, divisions) <> 0 
								ORDER BY 
									SortKey

					END 
				ELSE 
					BEGIN 
						SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Field', [field], @WindowStart) AS IsEnabled, 
								slotno, [week], [status], venue, field, 
								SlotStart,
								SlotLength, 
								reserved, divisions, 
								SlotReservedDateTime,
							    CONCAT(CONVERT(nvarchar(30), SlotStart, 112), ':', Venue, CONVERT(nvarchar(30), SlotStart, 114), ':', Field) AS SortKey
							FROM   rwllpractice 
							WHERE  reserved = 'Available' 
								AND [field] = @VenueName 
								AND [type] = 'Field' 
								AND Charindex(@Division, divisions) <> 0 
							UNION 
								SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Cage', [field], @WindowStart) AS IsEnabled, 
										slotno, [week], [status], venue, field, 
										SlotStart,
										SlotLength, 
										reserved, divisions, 
										SlotReservedDateTime,
										CONCAT(CONVERT(nvarchar(30), SlotStart, 112), ':', Venue, CONVERT(nvarchar(30), SlotStart, 114), ':', Field) AS SortKey
									FROM   rwllpractice 
									WHERE  reserved = 'Available' 
										AND [field] = @VenueName 
										AND [type] = 'Cage' 
										AND Charindex(@Division, divisions) <> 0 
							ORDER BY
								SortKey
					END 
			END 
	END 
ELSE -- else, just do a generic query since this isn't a real team login
	BEGIN 
		IF @TeamName = 'Administrator'
			BEGIN
				SELECT @SQL = @SQLPrefixDefaultEnabled + @SQLSuffix
			END
		ELSE
			BEGIN
				SELECT @SQL = @SQLPrefixDefaultDisabled + @SQLSuffix
			END
		IF @ShowSlots = 0 
			BEGIN 
				IF @VenueName = '' 
					BEGIN 
						SELECT @SQL = @SQL + ' Where Reserved <> ''Available'' and Reserved <> ''HOLD'' and Date=''' + @ShowDate + '''' 
					END 
				ELSE 
					BEGIN 
						SELECT @SQL = @SQL + ' Where Reserved <> ''Available'' and Reserved <> ''HOLD'' and Field=''' + @VenueName + '''' 
					END 
			END 
		IF @ShowSlots = 1 
			BEGIN 
				SELECT @SQL = @SQL + ' Where Reserved <> ''Available'' ' 
			END 
		IF @ShowSlots = 2 
			BEGIN 
				SELECT @SQL = @SQL + ' Where ( Reserved = ''Available'' or Reserved = ''HOLD'' )' 
				IF @ShowDate <> '06/31/2006' 
					BEGIN 
						IF @VenueName = '' 
							BEGIN 
								SELECT @SQL = @SQL
									+ ' AND DateDiff(minute, ''' + @ShowDate + ''', SlotStart) > 0 '
									+ ' AND DateDiff(minute, ''' + @ShowDate + ''', SlotStart) <= (60 * 24)'

							END 
						ELSE 
							BEGIN 
								SELECT @SQL = @SQL + ' and Field = ''' + @VenueName + '''' 
							END 
					END 
			END 

		SELECT @SQL = @SQL + ' order by ' + @Sort 

		--  SELECT @SQL 
		EXEC (@SQL) 
END  



