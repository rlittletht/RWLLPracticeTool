USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_DisplaySlots]    Script Date: 2/9/2020 2:44:31 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE proc [dbo].[usp_DisplaySlots]
(@TeamName  varchar(50),
@ShowSlots tinyint,
@ShowDate varchar(10),
@VenueName nvarchar(255) = '',
@Sort  varchar(20) = 'StartTime'
) as
Set nocount on
DECLARE @Division varchar(10),
@FieldStatus int,
@CageStatus int,
@SQL varchar(2000)

SELECT @SQL = 'SELECT 0 as IsEnabled, SlotNo,Week,Status,Venue,Field,convert(varchar(12),Date,111)as Date,Weekday,StartTime,
EndTime,Hours,Reserved,Divisions,ReserveDatetime from rwllpractice '
IF @TeamName not in ( 'Administrator','Tonya Henry', 'ShowAll', '-- Select Team --', '-- Admin --', '-- Baseball --', '-- Softball --' )
BEGIN
IF @ShowSlots = 1
BEGIN
SELECT @SQL = @SQL + ' where Reserved = ''' + @TeamName + ''''
SELECT @SQL = @SQL + ' order by ' + @Sort
--  SELECT @SQL
EXEC (@SQL)
END
IF @ShowSlots = 2
BEGIN
SELECT  @Division = division from rwllTeams where TeamName = @TeamName
--EXEC @FieldStatus =  usp_ReservedByDate @TeamName,@ShowDate, 'Field'
--EXEC @CageStatus  =  usp_ReservedByDate @TeamName,@ShowDate, 'Cage'
IF @VenueName = ''
BEGIN
SELECT [dbo].ufn_ReservationEnable(@TeamName, [Week], 'Field', [Field]) as IsEnabled, SlotNo,[Week],[Status],Venue,Field,convert(varchar(12),Date,111)as Date,[Weekday],StartTime, EndTime,[Hours],Reserved,Divisions,ReserveDatetime from rwllpractice
WHERE Reserved = 'Available' AND [Date] = @ShowDate AND [Type] = 'Field' and charindex(@Division, Divisions) <> 0
UNION
SELECT [dbo].ufn_ReservationEnable(@TeamName, [Week], 'Cage', [Field]) as IsEnabled, SlotNo,[Week],[Status],Venue,Field,convert(varchar(12),Date,111)as Date,[Weekday],StartTime, EndTime,[Hours],Reserved,Divisions,ReserveDatetime from rwllpractice
WHERE Reserved = 'Available' AND [Date] = @ShowDate AND [Type] = 'Cage' and charindex(@Division, Divisions) <> 0
END
ELSE
BEGIN
SELECT [dbo].ufn_ReservationEnable(@TeamName, [Week], 'Field', [Field]) as IsEnabled, SlotNo,[Week],[Status],Venue,Field,convert(varchar(12),Date,111)as Date,[Weekday],StartTime, EndTime,[Hours],Reserved,Divisions,ReserveDatetime from rwllpractice
WHERE Reserved = 'Available' AND [Field] = @VenueName AND [Type] = 'Field' and charindex(@Division, Divisions) <> 0
UNION
SELECT [dbo].ufn_ReservationEnable(@TeamName, [Week], 'Cage', [Field]) as IsEnabled, SlotNo,[Week],[Status],Venue,Field,convert(varchar(12),Date,111)as Date,[Weekday],StartTime, EndTime,[Hours],Reserved,Divisions,ReserveDatetime from rwllpractice
WHERE Reserved = 'Available' AND [Field] = @VenueName AND [Type] = 'Cage' and charindex(@Division, Divisions) <> 0
END
END
END
ELSE
BEGIN
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

GO

