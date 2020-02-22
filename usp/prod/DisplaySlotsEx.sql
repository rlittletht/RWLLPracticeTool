USE [THETASOFT]
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
*/

ALTER PROC [dbo].[usp_DisplaySlotsEx] (@TeamName  VARCHAR(50), 
                                       @ShowSlots TINYINT, 
                                       @ShowDate  VARCHAR(10), 
                                       @VenueName NVARCHAR(255) = '', 
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

SELECT @SQLSuffix = ' as IsEnabled, SlotNo,Week,Status,Venue,Field,convert(varchar(12),Date,111)as Date,Weekday,StartTime, EndTime,Hours,Reserved,Divisions,ReserveDatetime from rwllpractice ' 

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
						SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Field', [field] ) AS IsEnabled, 
							   slotno, [week], [status], venue, field, CONVERT(VARCHAR(12), date, 111) AS Date, 
							   [weekday], starttime, endtime, [hours], reserved, divisions, reservedatetime 
							FROM   rwllpractice 
							WHERE  reserved = 'Available' 
								AND [date] = @ShowDate 
								AND [type] = 'Field' 
								AND Charindex(@Division, divisions) <> 0 
							UNION 
								SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Cage', [field]) AS IsEnabled, 
									slotno, [week], [status], venue, field, CONVERT(VARCHAR(12), date, 111) AS Date, 
									[weekday], starttime, endtime, [hours], reserved, divisions, reservedatetime 
								FROM   rwllpractice 
								WHERE  reserved = 'Available' 
									AND [date] = @ShowDate 
									AND [type] = 'Cage' 
									AND Charindex(@Division, divisions) <> 0 
					END 
				ELSE 
					BEGIN 
						SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Field', [field] ) AS IsEnabled, 
								slotno, [week], [status], venue, field, CONVERT(VARCHAR(12), date, 111) AS Date, 
								[weekday], starttime, endtime, [hours], reserved, divisions, reservedatetime 
							FROM   rwllpractice 
							WHERE  reserved = 'Available' 
								AND [field] = @VenueName 
								AND [type] = 'Field' 
								AND Charindex(@Division, divisions) <> 0 
							UNION 
								SELECT [dbo].Ufn_reservationenableEx(@TeamName, [week], 'Cage', [field]) AS IsEnabled, 
										slotno, [week], [status], venue, field, CONVERT(VARCHAR(12), date, 111) AS Date, 
										[weekday], starttime, endtime, [hours], reserved, divisions, reservedatetime 
									FROM   rwllpractice 
									WHERE  reserved = 'Available' 
										AND [field] = @VenueName 
										AND [type] = 'Cage' 
										AND Charindex(@Division, divisions) <> 0 
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
								SELECT @SQL = @SQL + ' and Date = ''' + @ShowDate + '''' 
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


