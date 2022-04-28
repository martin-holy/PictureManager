# Bugs
##
  - disky identifikovat podle ID a ne podle pismena
    - or store drive related data in \Temp\PictureManagerCache
  - mhc:IconToggleButton need VerticalAlignment="Center" even if parent have this property set
## WorkTask
  - unexpected error, task was canceled. problem when returning false from catch
## MediaItems
  - D:\!test\364__32_original.jpg nejde ulozit metadata, nebo crop
  - error (there is too much metadata to be written to the bitmap) (20210902_083901_Martin.jpg)
  - kdyz ma obrazek metadata jen v DB, tak se rotace neaplikuje na vytvoreni thumbnail
  - ReadMetadata and ThumbInfoBox: only person is visible in info box even if keyword is on image too when loading new images
  - ErrorDialog in CopyMove (MediaItemsVM) doesn't open
## MediaViewer
  - zamrznuti pri mazani videa, kdyz se zrovna prehrava (nevim co s tim)
  - 15% CPU usage on media item delete from full screen
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
## Video Clips
  - kdyz klip konci az ke konci videa, tak se nekolikrat opakuje a pak zamrzne prehravac 
    a je to na restart appky (do OnMediaEnded se to nedostane)
  - ulozeni v klipech nezmeni modifed v main toolbaru
  - RadioButtons default value and IsChecked binding
## TreeViewCategories
  - TreeViewSearch XAML error (need help)
    (Cannot save value from target back to source. KeyNotFoundException: The given key '1' was not present in the dictionary.)
  - Folder reload after folder move => reload is maybe to soon
  - newly created folder is expanded
## FolderKeywords
  - do not show expand buttons in TreeView when there is nothing to expand
## Segments
  - SegmentControl ToolTip: kdyz je person name delsi nez image => segment rect je mimo (nevim co s tim)
    mozna to samy jak jsem pouzil asi na PersonEditControl ale se sloupcema. udelat 3 sloupce (*, auto, *)
  - media item thumbnail in tool tip is enlarged by windows display scaling
  - multi select: find ItemsGroup in WrappedItems above VirtualizingWrapPanelRow with selected segment to get ItemsGroup.Items
## PeopleV
  - reload when opened and person or group is created/renamed/moved in the tree
  - reload when keyword is toggled on person


# Update
##
  - pri overovani existence souboru pri nacitani, overovat pred FileInfo a zaroven doplnovat MediaItem o file size, ... do nullable props
  - zrusit tabstop tam kde to nema smysl, jako treba status bar, clips slide panel, ...
    nebo nastavit FocusVisualStyle na null
  - check strings Equals and set StringComparison Ordinal for strings from code and CurrentCulture for string for/from user
  - GetPerceptualHash a GetAvgHash ma podobnej kod, mozna bych to pouzil i jinde az po CopyPixels
  - static delegates
  - remove Folder Browser
  - Rethink hierarchical keywords moving
  - similar code in Imaging.GetThumbSize and ZoomImageBoxV.GetFitScale
  - comment out MahApps styles that I don't use for app start speed up
## DB
  - remove direct access to Helper.IsModified
## TreeViewCategories
  - MarkUsedKeywordsAndPeople add People and Keywords from Segments
  - select/mark searched item after selecting item in search result
## VideoClips:
  - recreate video thumbnails button
## Folders
  - remove IsFolderKeyword from Folder, store info in separate db file
## Segments
  - remove DisplayKeywords from Person and use Keywords.GetAllKeywords
## XAML
  - remove "x:Static pm:App.Ui, x:Static pm:App.Core" from Views and add properties to ViewModels?


# New
##
  - copy app settings from older app version
  - statickou tridu Prefs v App, ktera bude odkazovat na Settings.Default, takze se bude psat App.Prefs.FfmpegPath
  - Log to file
## Folders
  - Folders a Folder GetByPath udelat univerzalni pro CatTreeView
## MediaItems
  - pridat k rotate taky flip H a V
  - kdyz se nepodari ulozit metadata do obrazku, tak zaznamenat k MediaItemu, ze jsou metadata pouze v DB a obrazek nijak neupravovat
## CatTreeView
  - TODO GeoNames, user name dat do prop na GeoNames, user name checkovat OnBeforeItemCreate, menuitem GeoNameNewCommand udelat obecne
  - ItemCreate na People a Keywords je skoro stejny
  - IListBoxItem as ancestor of ITreeItem
  - filter na delku videa
  - nova kategorie "keywords groups" kde budou kombinace keywords, people, ... ktery se vyskytujou na jednom MediaItem
## Video Clips
  - akce na (ukoncit clip -> presun na dalsi snimek -> zacit novy clip)
  - lock na volume a speed, aby se ignorovalo to co je u clipu
  - import/export
  - metadata (keywords, people, rating, comment) on file and/or Clip
  - export to mp4 (FFMPEG)
## MediaViewer
  - po 3 vterinach taky skryt status bar a nastavit top bar na opacity 0
  - na prehravani panoramat zmensit obrazek na vysku obrazovky <= je to nekvalitni :(
  - zmensovat hodne velky obrazky pri prohlizeni <= je to nekvalitni :(
  - prochazet ve fullscreenu selected, abych si moch oznacit treba dva a prepinat se mezi nima (pozor, delete je za selected)
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
  - changes counter: if count is > 999 format as 1,x
    - ToolTip with number of changes of each table
  - keep list of modified, make MediaItems implementation universal
## VirtualizingWrapPanel
  - find item for scroll to with combination group|items
  - filter or group by GroupItems
    - list of GroupItems (folder, date, keyword, person, rating, geoName, ...) with link to items
    - not all GroupItems have to be active from the first load
  - group is expander
  - try to not scroll to top on reload
## Segments
  - keywords:
    - sort like in the tree
    - disable/enable available
  - count of segments on person
  - put somewhere icon with count of selected segments and show them in ToolTip or dialog or ...


Domain WPF references:
 - PresentationCore
 - PresentationFramework
 - System.Xaml
 - WindowsBase

#Shortcuts:
- ALT+S => Split video clip (create new video clip or finish selected without TimeEnd)
