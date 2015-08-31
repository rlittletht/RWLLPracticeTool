USE [DB0902]
GO
/****** Object:  UserDefinedFunction [dbo].[ufn_ReservationEnableEx]    Script Date: 03/24/2013 23:36:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER FUNCTION [dbo].[ufn_ReservationEnableEx]
	(
	@TeamName varchar(50),
	@Week float,
	@FieldType varchar(10),
	@Field nvarchar(255)
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
			@teamFieldsReleaseCount int = 0,
			@teamCagesReleaseCount int = 0,
			@ReleasedFieldsToday int,
			@ReleasedCagesToday int,
			@skipPerDayRule int = 0, -- 1 - skip rile, 0 - use rule
			@reservedInThisWeek_H5H6 int,
			@Result int,
			@ExtResult VARCHAR(50)

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

	SET @ExtResult = ''

	IF (@teamFieldsReleaseCount > 0) 
		BEGIN
			SET @ExtResult = @ExtResult + '(teamFieldsReleaseCount>0)'
			SET @skipPerDayRule = 1
		END 
	IF (@teamCagesReleaseCount > 0) 
		BEGIN
			SET @ExtResult = @ExtResult + '(teamCagesReleaseCount>0)'
			SET @skipPerDayRule = 1
		END 

	IF (@ReleasedFieldsToday > 0) 
		BEGIN
			SET @ExtResult = @ExtResult + '(ReleaseFieldsToday>0)'
			SET @skipPerDayRule = 1
		END 

	IF (@ReleasedCagesToday > 0) 
		BEGIN
			SET @ExtResult = @ExtResult + '(ReleaseCagesToday>0)'
			SET @skipPerDayRule = 1
		END

	-- Reserved in selected day
	SELECT @reservedInThisDay = Count(*) FROM rwllpractice
		Where 
			   Reserved = @TeamName 
			   	and convert(varchar, ReserveDatetime, 101) = convert(varchar, dateadd(hh,-8,getdate()), 101) and [Type] = @FieldType

	----Current week number
	--SELECT TOP(1)@currentWeekNumber = [Week] FROM rwllpractice
	--WHERE [Date] = @Date

	-- Reserved in selected week
	SELECT @reservedInThisWeek = COUNT(*) FROM rwllpractice
		WHERE Reserved = @TeamName and [Week]  = @Week and [Type] = @FieldType

	IF @reservedInThisWeek < @fieldPerWeek
		BEGIN
			IF (@skipPerDayRule = 1) OR (@reservedInThisDay < @fieldPerDay)
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
			ELSE -- skipPerDayRule != 0 and resrvedInThisDay >= fieldPerDay
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

