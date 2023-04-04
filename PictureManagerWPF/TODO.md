# Bugs
##
  - disky identifikovat podle ID a ne podle pismena (cmd dir command shows volume serial number)
    - or store drive related data in \Temp\PictureManagerCache
  - mhc:IconToggleButton need VerticalAlignment="Center" even if parent have this property set
  - reload media items count in title bar on viewer change
## WorkTask
  - unexpected error, task was canceled. problem when returning false from catch
## MediaItems
  - D:\!test\364__32_original.jpg nejde ulozit metadata, nebo crop (resave without metadata in xnview fix the file)
  - error (there is too much metadata to be written to the bitmap) (20210902_083901_Martin.jpg)
  - kdyz ma obrazek metadata jen v DB, tak se rotace neaplikuje na vytvoreni thumbnail
  - ErrorDialog in CopyMove (MediaItemsVM) doesn't open
  - Compare: after deleted remove items from list
  - Move Dialog removes first "_" from output folder and from file name
## MediaViewer
  - zamrznuti pri mazani videa, kdyz se zrovna prehrava (nevim co s tim)
  - 15% CPU usage on media item delete from full screen
  - mouse cursor doesn't hide when mouse doesn't move for more than 3 seconds
  - can't view 20170313_140708_Martin.jpg
  - can't add keyword when opened from video thumb
    video also doesn't have file size
  - jump to wrong file after delete when items where shuffled
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
  - Display aspect ratio different from video width and height ratio
    use DB width and height for aspect ratio and add way to change it
## Video Clips
  - kdyz klip konci az ke konci videa, tak se nekolikrat opakuje a pak zamrzne prehravac 
    a je to na restart appky (do OnMediaEnded se to nedostane)
  - ulozeni v klipech nezmeni modifed v main toolbaru
  - RadioButtons default value and IsChecked binding
## TreeViewCategories
  - TreeViewSearch XAML error (need help)
    (Cannot save value from target back to source. KeyNotFoundException: The given key '1' was not present in the dictionary.)
  - Folder reload after folder move => reload is maybe to soon
  - ScrollTo in TreeViewSearch doesn't work when People category is collapsed just for the first time
  - can't delete folder from file system
## FolderKeywords
  - do not show expand buttons in TreeView when there is nothing to expand
## PeopleV
  - reload when opened and person or group is created/renamed/moved in the tree
  - reload when keyword is toggled on person
## Person
  - TopSegments can get cleared when segment from not mounted drive is used and People are modified.
## ToolsTabs
  - reloading of Segments and ThumbnailsGrids doesn't work on IsPinned changed in full screen
    VirtualizingWrapPanel doesn't have the correct width when IsFullScreen is changing
## StackPanel
  - newly created video from images doesn't have file size when opened from thumbnail
## ThumbnailsGrid
  - reload or close when folder is deleted in TreeViewCategories
## Person Detail
  - remove segments after changing person
## Segment matching
  - wrap panel forgets scroll to index after opening segment source. changing segment person after scrolls to top
  - segment with person doesn't show number of selected segments
## SegmentRect
  - rect doesn't stay on the edge of an image when editing
  - moving beyond the edge of an image should stop changing x,y,c
  - rect is cropped when zoomed in
## Segment
  - wrong size calc with windowsDisplayScale
## SlidePanelsGrid
  - panel is opening by its self when for example segment is drag & drop in person detail in fullscreen
## Style
  - input dialog text background and foreground


# Update
##
  - pri overovani existence souboru pri nacitani, overovat pred FileInfo a zaroven doplnovat MediaItem o file size, ... do nullable props
  - zrusit tabstop tam kde to nema smysl, jako treba status bar, clips slide panel, ...
    nebo nastavit FocusVisualStyle na null
  - check strings Equals and set StringComparison Ordinal for strings from code and CurrentCulture for string for/from user
  - GetPerceptualHash a GetAvgHash ma podobnej kod, mozna bych to pouzil i jinde az po CopyPixels
  - static delegates
  - Rethink hierarchical keywords moving
  - similar code in Imaging.GetThumbSize and ZoomImageBoxV.GetFitScale
  - comment out MahApps styles that I don't use for app start speed up
  - check how many RAM consumes referencing objects like Folder have list of MediaItems or MediaItem have list of keywords and people, ...
  - check if mhu:SetterAction can be replaced by b:ChangePropertyAction
  - join FileOperationDialogM and FileOperationCollisionDialogM
## DB
  - remove direct access to Helper.IsModified
## TreeViewCategories
  - MarkUsedKeywordsAndPeople add Keywords from Segments
  - select/mark searched item after selecting item in search result
  - Items can be null, to save memory
  - Folder move: rethink selecting target folder after move
## VideoClips:
  - recreate video thumbnails button
## Folders
  - remove IsFolderKeyword from Folder, store info in separate db file
## Segments
  - remove DisplayKeywords from Person and use Keywords.GetAllKeywords
  - rename Confirmed Segments to Persons (People) and use PersonThumbnailV instead of SegmentV
    - how to deal with selection. it is use full to select confirmed segment and other segment
  - check if segments are sorted by mediaItem fileName and not just by id
  - remove add to drawer button from drawer
  - change icon on add to drawer
## PersonV
  - add button "set as unknown" for selected segments
  - count of segments on group with keywords
  - when loading media items for person => open first media item when mediaViewer is visible
## XAML
  - remove "x:Static pm:App.Ui, x:Static pm:App.Core" from Views and add properties to ViewModels?
  - replace behaviors with InputBindings where possible
  - move resources up to DataTemplate or ResourceDictionary
  - where to declare ResourceConverter because it is used in MH.UI and PM
## WPF Controls
  - ZoomAndPanImage update to MVVM
  - VideoPlayer update to MVVM
  - use button foreground and background for icon in IconButton and IconToggleButton
  - create dialog button style in MH.UI
  - add background behind text in IconTextButton
## Keywords
  - use only one method to get keywords in which will be filtering by current viewer
## Compress dialog
  - smaller font size for file size
## MediaItemM
  - remove IsNew prop


# New
##
  - copy app settings from older app version
  - statickou tridu Prefs v App, ktera bude odkazovat na Settings.Default, takze se bude psat App.Prefs.FfmpegPath
  - Log to file
  - change Dictionaries for object with implementation IEquatable and replace them with HashSets
  - Virtual images from video:
    - media type: VideoImage
    - props: timestamp of image and media item (video) id
    - can have keywords, people, comment, ... 
    - has own thumbnail icon
    - opening from thumbnail will show paused video 
      - so I need another video player mode to scroll thru VideoImages and than to next media item or MediaViewer will have mix of images, videos and videoImages?
## Folders
  - Folders a Folder GetByPath udelat univerzalni pro CatTreeView
  - GeoNames for Folders
## MediaItems
  - pridat k rotate taky flip H a V
  - kdyz se nepodari ulozit metadata do obrazku, tak zaznamenat k MediaItemu, ze jsou metadata pouze v DB a obrazek nijak neupravovat
## CatTreeView
  - TODO GeoNames, user name dat do prop na GeoNames, user name checkovat OnBeforeItemCreate, menuitem GeoNameNewCommand udelat obecne
  - filter na delku videa
  - nova kategorie "keywords groups" kde budou kombinace keywords, people, ... ktery se vyskytujou na jednom MediaItem
  - LeftMouseButtonClick on Person or Keyword opens menu with options (load, filter, set to person, set to segment, set to media item)
  - show small segment next to person icon or show just small segment
## Video Clips
  - akce na (ukoncit clip -> presun na dalsi snimek -> zacit novy clip)
  - lock na volume a speed, aby se ignorovalo to co je u clipu
  - import/export
  - metadata (keywords, people, rating, comment) on file and/or Clip
  - export to mp4 (FFMPEG)
  - zoom and pan
## MediaViewer
  - po 3 vterinach taky skryt status bar a nastavit top bar na opacity 0
  - na prehravani panoramat zmensit obrazek na vysku obrazovky <= je to nekvalitni :(
  - zmensovat hodne velky obrazky pri prohlizeni <= je to nekvalitni :(
  - prochazet ve fullscreenu selected, abych si moch oznacit treba dva a prepinat se mezi nima (pozor, delete je za selected)
  - func to zoom to segment
## ThumbnailsGrid
  - Auto groups from People and Keywords
  - Show Folders with images from 4 sub folders, number of files (info only from cache with refresh on expand)
  - vymyslet to tak, aby zustali thumbs selected i kdyz se prepne do viewera a listuje se v nem
    pozor!! kdyz bych ve vieweru editoval/mazal/..., tak bych to aplikoval na selected!!!
  - v nahledech na videa neustale prehravat video vytvoreny ze screenshotu toho videa ve velikosti nahledu, takze to snad 
    nebude narocny a bude se to moc prehravat porad a kdyz se na nahled najede mysi tak se pude prehravat puvodni video
  - umoznit zmenit nahled u videa
## Viewers
  - Rename Viewers to Workspaces
## DB
  - save in user profile
  - new table for MediaItems hashes for comparison
  - ToolTip or dialog with number of changes of each table
  - keep list of modified, make MediaItems implementation universal
  - listen in DataAdapter on OnPropertyChanged and on ObservableCollection to log changes for overview
    and automatic setting IsModified
## VirtualizingWrapPanel
  - find item for scroll to with combination group|items
  - filter or group by GroupItems
    - list of GroupItems (folder, date, keyword, person, rating, geoName, ...) with link to items
    - not all GroupItems have to be active from the first load
  - group is expander
  - try to not scroll to top on reload
  - make VWP like Tree where:
    - Root can contain Group or Item
    - Group can contain Group or Item
    - Item can contain Item
## Segments
  - keywords:
    - sort like in the tree
    - disable/enable available
  - count of segments on person
  - put somewhere icon with count of selected segments and show them in ToolTip or dialog or ...
  - add groups to SegmentDrawer
  - open from SegmentDrawer will open media item and load all other segments from SegmentDrawer
  - tagging keyword on segment will show options to set it to segment or person
  - filter for segment resolution


#Shortcuts:
- ALT+S => Split video clip (create new video clip or finish selected without TimeEnd)


## DB Rewrite
  - store drive related data in drive:\Temp\PictureManagerCache\db
    (FavoriteFolders, Folders, MediaItems, Segments, VideoClips, VideoClipsGroups)
  - test before folder delete from cache if is not the db folder
  - if drive is not ready any more or if is read only => store data to default db location
  - how to load data:
    - look at X:\Temp\PictureManagerCache\db\Folders.csv
    - if not found
    - look at db\Folders.X.csv
    - if not found
    - look at db\Folders.csv
  - save needs to be done with walking the tree so that sort order is preserved
  - so the first start should load from db\Folders.csv
    and than save to X:\Temp\PictureManagerCache\db\Folders.csv or db\Folders.X.csv
    so when data was loaded from db\Folders.X.csv 
      and than there was successful save to X:\Temp\PictureManagerCache\db\Folders.csv
      db\Folders.X.csv should be deleted
  - use list of drives for load on one location