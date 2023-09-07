# Bugs - Need Help
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
    setting SpeedRatio to SpeedRatio before or after Play() doesn't help
## TreeViewCategories
  - TreeViewSearch XAML error
    (Cannot save value from target back to source. KeyNotFoundException: The given key '1' was not present in the dictionary.)
## MediaViewer
  - zamrznuti pri mazani videa, kdyz se zrovna prehrava (nevim co s tim)

# Bugs
##
  - disky identifikovat podle ID a ne podle pismena (cmd dir command shows volume serial number)
    - or store drive related data in \Temp\PictureManagerCache
## WorkTask
  - unexpected error, task was canceled. problem when returning false from catch
## MediaItems
  - D:\!test\364__32_original.jpg nejde ulozit metadata, nebo crop (resave without metadata in xnview fix the file)
  - error (there is too much metadata to be written to the bitmap) (20210902_083901_Martin.jpg)
## VideoPlayer
  - Display aspect ratio different from video width and height ratio
    use DB width and height for aspect ratio and add way to change it
## Video Clips
  - kdyz klip konci az ke konci videa, tak se nekolikrat opakuje a pak zamrzne prehravac 
    a je to na restart appky (do OnMediaEnded se to nedostane)
  - ulozeni v klipech nezmeni modifed v main toolbaru
## TreeViewCategories
  - Folder reload after folder move => reload is maybe to soon
  - ScrollTo in TreeViewSearch doesn't work when People category is collapsed just for the first time
  - click on rating stars doesn't set metadata
## FolderKeywords
  - do not show expand buttons in TreeView when there is nothing to expand
## PeopleM
  - remove deleted people from selection. MergePeople, ...
## PeopleV
  - reload when opened and person or group is created/renamed/moved in the tree
## Person Detail
  - TopSegments can get cleared when segment from not mounted drive is used and People are modified.
  - set top segment from selected person after PeopleMerge
## MH.UI.WPF
  # Custom Window
   - minimize window will break full screen mode
## CollectionView
  - segments grouped by keywords,
    clear grouping on keyword with sub keywords,
    set new keyword on segments in cleared grouping,
    clear grouping is restored

# Update
##
  - zrusit tabstop tam kde to nema smysl, jako treba status bar, clips slide panel, ...
  - Rethink hierarchical keywords moving
  - check how many RAM consumes referencing objects like Folder have list of MediaItems or MediaItem have list of keywords and people, ...
  - join FileOperationDialogM and FileOperationCollisionDialogM
  - change all Tree View Categories like Video Clips
  - Log button
  - make thumbnail images sharper (size is 2.7x bigger)
  - change Tree.GetThisAndParentRecursive using yield like in FoldersDataAdapter
  - show media items with selected people
  - replace Equals with ReferenceEquals where possible
  - make selection border on base class level in CategoryView and remove it from segment, person, mediaItem
## DB
  - remove direct access to Helper.IsModified
## TreeViewCategories
  - MarkUsedKeywordsAndPeople add Keywords from Segments
  - select/mark searched item after selecting item in search result
  - Items can be null, to save memory
  - Folder move: rethink selecting target folder after move
  - multilevel groups (so make base class for folders, folderKeywords, keywords, geonames, category group, ...)
## VideoClips:
  - recreate video thumbnails button
  - use TreeItemSelectedEvent instead SelectedItemChanged XAML
## XAML
  - remove "x:Static pm:App.Ui, x:Static pm:App.Core" from Views and add properties to ViewModels?
  - replace behaviors with InputBindings where possible
  - change converters using MarkupExtension like in RatingConverter
  - use Grid instead of StackPanel when is used many times like in TreeItemV
  - use BaseMarkupExtensionConverter for converters
  - use SharedResourceDictionary with WeekReference stackowerflow.com/questions/6857355/...
## MH.UI.WPF
  - use button foreground and background for icon in IconButton and IconToggleButton
  - add background behind text in IconTextButton
  - make ExpandCollapseToggleStyle and TreeViewItemFocusVisual as resource for CatTreeView and TreeWrapView
  - merge CatTreeView and TreeWrapView templates
  - use RelativeSource AncestorType= without x:Type
  # Style
   - Label - remove default padding?
   - MH.Styles.IconShadow
## Keywords
  - use only one method to get keywords in which will be filtering by current viewer
## Dialog
  - add CanExecute for buttons like Yes/Ok
## Segments
  - with segment creation create unknown person (#1234) as well so that every segment has person
## ToggleDialog
  - make it static or returning one instance
  - show selected items on dialog
## MediaViewer
  - hide cursor only in presentation mode
## SegmentsMatching
  - from selected people and segments
## PeopleV
  - show keywords groups in GroupByDialog
## SegmentV
  - make selection bigger. remove margin and replace it by transparent border
## Tabs
  - base class for all tabs (TreeViewCategory tabs, tools tabs and main tabs)
    and allow moving tab to left|middle|right panel
## MediaItemsView
  - replace mouse wheel + CTRL for zoom with slider
## SegmentsRects
  - sort by X for correct overlap
## CategoryViewGroup
  - GroupedBy instance for empty group with icon from View 

# New
##
  - Log to file
  - Virtual images from video:
    - media type: VideoImage
    - props: time stamp of image and media item (video) id
    - can have keywords, people, comment, ... 
    - has own thumbnail icon
    - opening from thumbnail will show paused video 
      - so I need another video player mode to scroll trough VideoImages and than to next media item or MediaViewer will have mix of images, videos and videoImages?
## one source for MediaItemsView and MediaViewer
  - collection of MediaItems with filtering used in MediaItemsView and MediaViewer.
    so I can choice how to display filtered data: in MediaItemsView or MediaViewer
    and choice show selected or all
    ...
## Folders
  - GeoNames for Folders
## MediaItems
  - add flip H a V
  - kdyz se nepodari ulozit metadata do obrazku, tak zaznamenat k MediaItemu, ze jsou metadata pouze v DB a obrazek nijak neupravovat
## CatTreeView
  - filter na delku videa
  - LeftMouseButtonClick on Person or Keyword opens menu with options (load, filter, set to person, set to segment, set to media item)
  - show small segment next to person icon or show just small segment
## Video Clips
  - akce na (ukoncit clip -> presun na dalsi snimek -> zacit novy clip)
  - lock na volume a speed, aby se ignorovalo to co je u clipu
  - import/export
  - metadata (keywords, people, rating, comment) on file and/or Clip
  - export to mp4 (FFMPEG)
## MediaViewer
  - na prehravani panoramat zmensit obrazek na vysku obrazovky <= je to nekvalitni :(
  - zmensovat hodne velky obrazky pri prohlizeni <= je to nekvalitni :(
  - prochazet ve fullscreenu selected, abych si moch oznacit treba dva a prepinat se mezi nima (pozor, delete je za selected)
  - func to zoom to segment
## ThumbnailsGrid
  - Show Folders with images from 4 sub folders, number of files (info only from cache with refresh on expand)
  - vymyslet to tak, aby zustali thumbs selected i kdyz se prepne do viewera a listuje se v nem
    pozor!! kdyz bych ve vieweru editoval/mazal/..., tak bych to aplikoval na selected!!!
  - v nahledech na videa neustale prehravat video vytvoreny ze screenshotu toho videa ve velikosti nahledu, takze to snad 
    nebude narocny a bude se to moc prehravat porad a kdyz se na nahled najede mysi tak se bude prehravat puvodni video
  - umoznit zmenit nahled u videa
  - TimeLine for thumbnails showing dates and number of day from first date in folder
## Viewers
  - Rename Viewers to Workspaces
## DB
  - save in user profile
  - new table for MediaItems hashes for comparison
  - ToolTip or dialog with number of changes of each table
  - keep list of modified, make MediaItems implementation universal
  - listen in DataAdapter on OnPropertyChanged and on ObservableCollection to log changes for overview
    and automatic setting IsModified
## Segments
  - keywords:
    - sort like in the tree
    - disable/enable available
  - put somewhere icon with count of selected segments and show them in ToolTip or dialog or ...
  - filter for segment resolution
## TreeView
  - show ScrollToParent on mouse hover. Icon with ToolTip (Parent.Name) for each parent. Maybe shared button for whole TreeView


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