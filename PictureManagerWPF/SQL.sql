--                                        promazani duplicit

--select Path from Directories group by Path having count(Id) > 1

--select * from MediaItems as MI where not exists (select D.Id from Directories as D where D.Id = MI.DirectoryId)

--select * from MediaItemKeyword as MIK where not exists (select MI.Id from MediaItems as MI where MI.Id = MIK.MediaItemId)

--select * from MediaItemPerson as MIP where not exists (select MI.Id from MediaItems as MI where MI.Id = MIP.MediaItemId)


--delete from Directories where Path in (select Path from Directories group by Path having count(Id) > 1)

--delete from MediaItems where Id in (select MI.Id from MediaItems as MI where not exists (select D.Id from Directories as D where D.Id = MI.DirectoryId))

--delete from MediaItemKeyword where Id in (select MIK.Id from MediaItemKeyword as MIK where not exists (select MI.Id from MediaItems as MI where MI.Id = MIK.MediaItemId))

--delete from MediaItemPerson where Id in (select MIP.Id from MediaItemPerson as MIP where not exists (select MI.Id from MediaItems as MI where MI.Id = MIP.MediaItemId))


-- smazani nepouzitych Keywords
--delete from Keywords where Name not like "%/%" and Id not in (select KeywordId from MediaItemKeyword)