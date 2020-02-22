USE [DB0902]
GO

/****** Object:  UserDefinedFunction [dbo].[ufn_ReservationEnable]    Script Date: 2/9/2020 2:45:33 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[ufn_ReservationEnable]
	(
	@TeamName varchar(50),
	@Week float,
	@FieldType varchar(10),
	@Field nvarchar(255)
	)
RETURNS INT AS
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
			@Result int

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
		Where 
			   Reserved = @TeamName 
			   	and convert(varchar, ReserveDatetime, 101) = convert(varchar, getdate(), 101) and [Type] = @FieldType

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
			ELSE
				BEGIN
					Set @Result = 1
				END
		END
	ELSE
		BEGIN
			Set @Result = 1
		END
	RETURN @Result
END

GO

