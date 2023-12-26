                                *** BUGS ***
        # MH.UI.WPF
## Custom Window
  - minimize window will break full screen mode

        # Picture Manager - Need Help
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
    setting SpeedRatio to SpeedRatio before or after Play() doesn't help
## MediaViewer
  - app freeze on video delete when is playing

        # Picture Manager
  - read only drives and creating thumbnails
## MediaItems
  - D:\!test\364__32_original.jpg save metadata or crop doesn't work (re-save without metadata in XnView fixed the file)
  - error (there is too much metadata to be written to the bitmap) (20210902_083901_Martin.jpg)
  - Reload metadata on video
## VideoPlayer
  - Display aspect ratio different from video width and height ratio
    use DB width and height for aspect ratio and add way to change it
## Person Detail
  - TopSegments can get cleared when segment from not mounted drive is used and People are modified.
## MainTabs
  - creating new tab resets scroll bars on hidden tabs. probably on LayoutUpdate when ItemsSource is updated


                                  *** UPDATE ***
        # MH.UI
## Tabs
  - allow moving tab to left|middle|right panel
## CategoryViewGroup
  - GroupedBy instance for empty group with icon from View
## CollectionView
  - don't show expanded root if source is empty
## PopupSlider
  - close on mouse up

        # MH.Utils
## Resources
  - use constant strings for icons
## Dialog
  - add CanExecute for buttons like Yes/OK

        # MH.UI.WPF
## IconButton
  - use button foreground and background for icon in IconButton and IconToggleButton
  - add background behind text in IconTextButton
## Style
  - Label - remove default padding?
  - MH.Styles.IconShadow

        # Picture Manager
##
  - Rethink hierarchical keywords moving
  - join FileOperationDialogM and FileOperationCollisionDialogM
  - Log button
  - show media items with selected people
  - replace Equals with ReferenceEquals where possible
  - remove MediaItemFilterSizeM
## TreeViewCategories
  - select/mark searched item after selecting item in search result
  - multilevel groups (so make base class for folders, folderKeywords, keywords, geonames, category group, ...)
  - close search on mouse leave
## Keywords
  - use only one method to get keywords in which will be filtering by current viewer
## Segments
  - with segment creation create unknown person (#1234) as well so that every segment has person
## ToggleDialog
  - show selected items on dialog
## SegmentsDataAdapter
  - move generic methods ItemCreate, ItemsDelete, ItemDelete to DataAdapter
## MediaItems
  - save to file only metadata visible for viewer
  - merge ReloadMetadataInFolderCommand and ReloadMetadataCommand
## MediaItemsView
  - change SelectAndScrollToCurrentMediaItem to scroll to exactly to media item
    only if is not in the view otherwise scroll to top item



##
 - reset sort order button of VideoMediaItems to delete relation in DB
 - tool tip for VideoMediaItems with all info
 - comment and delete on VideoItem
 - rename DataAdapter classes to DA



                                    *** NEW ***
##
  - Log to file
  - Virtual images from video:
    - media type: VideoImage
    - props: time stamp of image and media item (video) id
    - can have keywords, people, comment, ... 
    - has own thumbnail icon
    - opening from thumbnail will show paused video 
      - so I need another video player mode to scroll trough VideoImages and than to next media item or MediaViewer will have mix of images, videos and videoImages?
  - slider with drag point for adjusting slider drag change value where:
    if the point is more away from the slider => change value is smaller
    and if is closer => change value is bigger
    (or zooming time line)
## one source for MediaItemsView and MediaViewer
  - collection of MediaItems with filtering used in MediaItemsView and MediaViewer.
    so I can choice how to display filtered data: in MediaItemsView or MediaViewer
    and choice show selected or all
    ...
## Folders
  - GeoNames for Folders
## MediaItems
  - add flip H a V
## CatTreeView
  - filter for video length
  - LeftMouseButtonClick on Person or Keyword opens menu with options (load, filter, set to person, set to segment, set to media item)
  - show small segment next to person icon or show just small segment
## Video Clips
  - akce na (ukoncit clip -> presun na dalsi snimek -> zacit novy clip)
  - lock na volume a speed, aby se ignorovalo to co je u clipu
  - import/export
  - metadata (keywords, people, rating, comment) on file and/or Clip
  - export to mp4 (FFMPEG)
## MediaViewer
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

                                    *** DISCARDED ***
  - make thumbnail images sharper (size is 2.7x bigger)
## MediaViewer
  - na prehravani panoramat zmensit obrazek na vysku obrazovky <= je to nekvalitni :(
  - zmensovat hodne velky obrazky pri prohlizeni <= je to nekvalitni :(


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