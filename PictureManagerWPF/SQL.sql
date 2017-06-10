--                                        promazani duplicit

--select Path from Directories group by Path having count(Id) > 1
--delete from Directories where Path in (select Path from Directories group by Path having count(Id) > 1)



-- smazani nepouzitych Keywords
select * from Keywords where Name not like "%/%" and Id not in (select KeywordId from MediaItemKeyword)
delete from Keywords where Name not like "%/%" and Id not in (select KeywordId from MediaItemKeyword)

-- smazani vazby MediaItemKeyword, kdyz neexistuje MediaItem
select * from MediaItemKeyword where Id in (select MIK.Id from MediaItemKeyword as MIK where not exists (select MI.Id from MediaItems as MI where MI.Id = MIK.MediaItemId))
delete from MediaItemKeyword where Id in (select MIK.Id from MediaItemKeyword as MIK where not exists (select MI.Id from MediaItems as MI where MI.Id = MIK.MediaItemId))

-- smazani MediaItemKeyword, kdyz neexistuje Keyword
select * from MediaItemKeyword where Id in (select MIK.Id from MediaItemKeyword as MIK where not exists (select K.Id from Keywords as K where K.Id = MIK.KeywordId))
delete from MediaItemKeyword where Id in (select MIK.Id from MediaItemKeyword as MIK where not exists (select K.Id from Keywords as K where K.Id = MIK.KeywordId))

-- smazani vazby MediaItemPerson, kdyz neexistuje MediaItem
select * from MediaItemPerson where Id in (select MIP.Id from MediaItemPerson as MIP where not exists (select MI.Id from MediaItems as MI where MI.Id = MIP.MediaItemId))
delete from MediaItemPerson where Id in (select MIP.Id from MediaItemPerson as MIP where not exists (select MI.Id from MediaItems as MI where MI.Id = MIP.MediaItemId))

-- smazani vazby MediaItemPerson, kdyz neexistuje Person
select * from MediaItemPerson where Id in (select MIP.Id from MediaItemPerson as MIP where not exists (select P.Id from People as P where P.Id = MIP.PersonId))
delete from MediaItemPerson where Id in (select MIP.Id from MediaItemPerson as MIP where not exists (select P.Id from People as P where P.Id = MIP.PersonId))

-- smazani slozek bez MediaItems
select * from Directories where Id not in (select distinct DirectoryId from MediaItems)
delete from Directories where Id not in (select distinct DirectoryId from MediaItems)