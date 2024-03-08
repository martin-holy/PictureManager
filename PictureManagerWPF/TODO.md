                                *** BUGS ***
        # MH.UI.WPF
## Custom Window
  - minimize window will break full screen mode
## ErrorDialog
  - resize stretch

        # Picture Manager - Need Help
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
    setting SpeedRatio to SpeedRatio before or after Play() doesn't help
## MediaViewer
  - app freeze on video delete when is playing

        # Picture Manager
  - read only drives and creating thumbnails
## MediaItems
  - D:\!test\364__32_original.jpg save metadata doesn't work (re-save without metadata in XnView fixed the file)
## Video Items (VideoImage and VideoClip)
  - Display aspect ratio different from video width and height ratio
    use DB width and height for aspect ratio and add way to change it
## Person Detail
  - TopSegments can get cleared when segment from not mounted drive is used and People are modified.
## MainTabs
  - creating new tab resets scroll bars on hidden tabs. probably on LayoutUpdate when ItemsSource is updated
## CollectionView
  - wrong sort (it is by text and not number) on update when grouped by date
## SegmentsMatching
  - scroll people down => select segment in segments => select person => people are scrolled to top
    maybe set focus on mouse enter


                                  *** UPDATE ***
        # MH.UI
## CollectionView
  - don't show expanded root if source is empty
  - method Add or Insert => Update(e.Data, false)


        # Picture Manager
##
  - Rethink hierarchical keywords moving
  - Log button
  - CoreUI with interfaces for CoreM and CoreVM
  - replace Contains with ReferenceEquals
## TreeViewCategories
  - select/mark searched item after selecting item in search result
## Keywords
  - use only one method to get keywords in which will be filtering by current viewer
## Segments
  - with segment creation create unknown person (#1234) as well so that every segment has person
  - add button remove selected segments from drawer
## ToggleDialog
  - show selected items on dialog
## MediaItems
  - save to file only metadata visible for viewer
## MediaItemsView
  - use Filter on MediaItem type in LoadByTag GetItems
## ProgressBarAsyncDialog
  - do Init and Start in constructor
## Core
  - use named methods for events
  - split repository and view-model events handling
    so maybe don't do events handling in EntityR but in CoreR


                                    *** NEW ***
##
  - Log to file
  - slider with drag point for adjusting slider drag change value where:
    if the point is more away from the slider => change value is smaller
    and if is closer => change value is bigger
    (or zooming time line)
  - sort MediaItems and Segments by color of Thumb image
## one source for MediaItemsView and MediaViewer
  - collection of MediaItems with filtering used in MediaItemsView and MediaViewer.
    so I can choice how to display filtered data: in MediaItemsView or MediaViewer
    and choice show selected or all
## Folders
  - GeoNames for Folders
## MediaItems
  - add flip H a V
  - filter for video length
## CatTreeView
  - multilevel groups
## Video Items (VideoImage and VideoClip)
  - tool tip for VideoItems with all info
  - reset sort order button of VideoItems to delete relation in DB
  - add changing sort order
  - ignore volume and speed from selected clip ToggleButton
  - show as separate thumbs in MediaItemView
  - import/export
  - export to mp4 (FFMPEG)
## MediaViewer
  - func to zoom to segment
## ThumbnailsGrid
  - Show Folders with images from 4 sub folders, number of files (info only from cache with refresh on expand)
  - TimeLine for thumbnails showing dates and number of day from first date in folder
## Viewers
  - Rename Viewers to Workspaces
## DB
  - save in user profile
  - new table for MediaItems hashes for comparison
  - ToolTip or dialog with number of changes of each table
## Segments
  - keywords:
    - sort like in the tree
    - disable/enable available
  - put somewhere icon with count of selected segments and show them in ToolTip or dialog or ...
  - filter for segment resolution
## TreeView
  - show ScrollToParent on mouse hover. Icon with ToolTip (Parent.Name) for each parent. Maybe shared button for whole TreeView
## GroupByDialog
  - incremental search
## PersonDetail
  - Comment
## MovieManager Plug-in



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