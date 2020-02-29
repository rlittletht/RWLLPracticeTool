USE [DB0902]
GO

/****** Object:  StoredProcedure [dbo].[usp_ReservationReport]    Script Date: 2/23/2020 10:29:48 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROC [dbo].[usp_ReservationReport] as
SET NOCOUNT ON
PRINT '     Team Reservations by Location'
PRINT '     Sorted by # reserved (highest to lowest)'
PRINT ''
select count(*) as Count,
convert(varchar(20),reserved) as Team,
convert(varchar(20),venue) as Location from rwllpractice
where reserved <> 'available'
group by reserved, venue
order by count(*) desc

PRINT ''
PRINT '     Team Reservations by Location'
PRINT '     Sorted by Team'
select count(*) as Count,
convert(varchar(20),reserved) as Team,
convert(varchar(20),venue) as Location from rwllpractice
where reserved <> 'available'
group by reserved, venue
order by reserved

PRINT ''
PRINT '     Reservations made at Hartman Park'
PRINT ''
select count(*) as Count,
convert(varchar(20),reserved) as Team,
convert(varchar(20),field) as Field from rwllpractice
where venue like '%Hartman%'
group by reserved, field
order by count(*) desc

GO

