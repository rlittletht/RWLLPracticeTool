USE [DB0902]
GO

/****** Object:  UserDefinedFunction [dbo].[ufn_ReservationEnableEx]    Script Date: 2/9/2020 2:45:42 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/* Approach:

	There are several restrictions on reservations. 
	
	Some are about how many events with the same "Week" value you can 
	hold (i.e. based on rules in the rwlldivisions table...you can only 
	hold 4 reservations	in a particular week for fields).

	Some are about how many reservations you can place within
	a 24 hour period (no matter what the date is for the reservation. This
	prevents people from reserving every single week of the season all
	at once. The 24 hour window is configurable by a parameter passed in

Params:
	@TeamName - the name of hte team as stored in the table [rwllteams]
	@Week - the week group number to consider. this will be used to determine
			how many events during that week they can reserve
	@FieldType - The type of the reservation (FIELD or CAGE (or other in the future)
	@Field - Some fields have specific restrictions as well 
		(only once per 24 hour period or twice a week, for example)
	@WindowStart - this is when the 24 hour window starts (in UTC) for 
		determining	how many events were reserved during a window. this is
		configurable chiefly to allow clients to determine when their
		'midnight' happens, especially with various daylight savings
		times.  this also could allow per-team windows)
*/

ALTER FUNCTION [dbo].[ufn_ReservationEnableEx]
	(
	@TeamName varchar(50),
	@Week float,
	@FieldType varchar(10),
	@Field nvarchar(255),
	@WindowStart DateTime2
	)
RETURNS VARCHAR(50) AS
BEGIN
	DECLARE @Division varchar(10),
			@SQL varchar(2000),
			@fieldPerDay int,
			@fieldPerWeek int,
			@reservedInThisWeek int,
			@reservedInThisDay int,
			@currentWeekNumber int,
			@ReleasedFieldsToday int = 0,
			@ReleasedCagesToday int = 0,
			@reservedInThisWeek_H5H6 int,
			@Result int,
			@ExtResult VARCHAR(50)

	SELECT @Division = division from rwllTeams where TeamName = @TeamName

	IF @FieldType = 'Field'
		BEGIN
			SELECT @ReleasedFieldsToday = Count(*) FROM rwllpractice WHERE ReleaseTeam=@TeamName AND [Type] = 'Field' AND DateDiff(minute, @WindowStart, SlotReleasedDateTime) >= 0 AND DateDiff(minute, @WindowStart, SlotReleasedDateTime) <= 24 * 60
			SELECT @fieldPerDay = FieldReservationsPerDay from rwlldivisions where DivisionName = @Division
			SELECT @fieldPerWeek = FieldReservationsPerWeek from rwlldivisions where DivisionName = @Division

			if (@ReleasedFieldsToday > @fieldPerDay)
				BEGIN
					SET @ReleasedFieldsToday = @fieldPerDay
				END

			if (@ReleasedFieldsToday > 0)
				BEGIN
					SET @fieldPerDay = @fieldPerDay + @ReleasedFieldsToday
				END
		END
	ELSE
		BEGIN
			SELECT @ReleasedCagesToday = Count(*) FROM rwllpractice WHERE ReleaseTeam=@TeamName AND [Type] = 'Cages' AND DateDiff(minute, @WindowStart, SlotReleasedDateTime) >= 0 AND DateDiff(minute, @WindowStart, SlotReleasedDateTime) <= 24 * 60
			SELECT @fieldPerDay = CageReservationsPerDay from rwlldivisions where DivisionName = @Division
			SELECT @fieldPerWeek = CageReservationsPerWeek from rwlldivisions where DivisionName = @Division

			if (@ReleasedCagesToday > @fieldPerDay)
				BEGIN
					SET @ReleasedCagesToday = @fieldPerDay
				END

			if (@ReleasedCagesToday > 0)
				BEGIN
					SET @fieldPerDay = @fieldPerDay + @ReleasedFieldsToday
				END
		END

	SET @ExtResult = ''

	-- Reserved in selected day
	SELECT @reservedInThisDay = Count(*) FROM rwllpractice
		Where 
			   Reserved = @TeamName
			    AND DateDiff(minute, @WindowStart, SlotReservedDateTime) >= 0
				AND DateDiff(minute, @WindowStart, SlotReservedDateTime) < 60 * 24
			   	AND [Type] = @FieldType

	----Current week number
	--SELECT TOP(1)@currentWeekNumber = [Week] FROM rwllpractice
	--WHERE [Date] = @Date

	-- Reserved in selected week
	SELECT @reservedInThisWeek = COUNT(*) FROM rwllpractice
		WHERE Reserved = @TeamName and [Week]  = @Week and [Type] = @FieldType

	IF @reservedInThisWeek < @fieldPerWeek
		BEGIN
			IF (@reservedInThisDay < @fieldPerDay)
				BEGIN
					IF @Field in ('H5', 'H6', 'H5a', 'H5b', 'H6a', 'H6b')
						BEGIN
							-- Reserved fields H5, H6 in selected week
							SELECT @reservedInThisWeek_H5H6 = COUNT(*) FROM rwllpractice
							WHERE Reserved = @TeamName and [Week]  = @Week and [Type] = @FieldType and [Field] in ('H5', 'H6', 'H5a', 'H5b', 'H6a', 'H6b')
							-- If division is Majors BB, Coast BB, Juniors SB, Majors SB, Coast SB, Juniors BB, Seniors BB, Bigs BB
							IF charindex(@Division, 'ABGHILMN') <> 0
								BEGIN
									-- Max reserved for these divisions is 3 per week...
									IF @reservedInThisWeek_H5H6 > 2
										BEGIN
    										Set @ExtResult = '@reservedInThisWeek_H5H6(' + cast(@reservedInThisWeek_H5H6 as varchar(50))+ ') > 2; @Division = ' + @Division
											Set @Result = 1
										END
									ELSE
										BEGIN
											Set @Result = 0
										END
								END
							ELSE
								BEGIN
									IF @Division <> 'X'
										BEGIN
											-- Max reserved for these divisions is 2 per week...
											IF @reservedInThisWeek_H5H6 > 1
												BEGIN
													Set @ExtResult = '@reservedInThisWeek_H5H6(' + cast(@reservedInThisWeek_H5H6 as varchar(50))  + ') > 1; Division = ' + @Division
													Set @Result = 1
												END
											ELSE
												BEGIN
													Set @Result = 0
												END
										END
									ELSE
										BEGIN
											Set @Result = 0
										END
								END
						END
					ELSE
						BEGIN
							Set @Result = 0
						END
				END
			ELSE -- resrvedInThisDay >= fieldPerDay
				BEGIN
    				Set @ExtResult = '@reservedInThisDay(' + cast(@reservedInThisDay as varchar(50)) + ') >= @fieldPerDay' + cast(@fieldPerDay as varchar(50)) + ')'
					Set @Result = 1
				END
		END
	ELSE
		BEGIN
    		set @ExtResult = '@reservedInThisWeek(' + cast(@reservedInThisWeek as varchar(50)) + ') >= @fieldPerWeek(' + cast(@fieldPerWeek as varchar(50)) + ')'
			Set @Result = 1
		END
	RETURN @ExtResult
END

GO

