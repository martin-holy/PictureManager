                                *** BUGS ***
        # MH.UI.WPF
## TreeViewBase
  - Mouse scroll up when some items are collapsed (ListItem.IsHidden) is not possible. ScrollBar is working
## Custom Window
  - minimize window will break full screen mode
## ErrorDialog
  - resize stretch
## MediaPlayer
  - TimelinePosition changes but video not after changing TimelinePosition with buttons

        # Picture Manager - Need Help
## VideoPlayer
  - if SpeedRatio is > 2, Pause()/Play() will cause ignoring SpeedRatio and plays on SpeedRatio = 1
    setting SpeedRatio to SpeedRatio before or after Play() doesn't help
## MediaViewer
  - app freeze on video delete when is playing

        # Picture Manager
  - read only drives and creating thumbnails
  - add multilevel keyword to person => delete sub keyword => person doesn't have keyword displayed but Load people by keyword works
    after app restart is OK
  - changing selection in Viewer detail works only with mouse. with keyboard selection works only visually
## MediaItems
  - D:\!test\364__32_original.jpg save metadata doesn't work (re-save without metadata in XnView fixed the file)
## Video Items (VideoImage and VideoClip)
  - Display aspect ratio different from video width and height ratio
    use DB width and height for aspect ratio and add way to change it
## MainTabs
  - creating new tab resets scroll bars on hidden tabs. probably on LayoutUpdate when ItemsSource is updated
## CollectionView
  - wrong sort (it is by text and not number) on update when grouped by date
## SegmentsMatching
  - scroll people down => select segment in segments => select person => people are scrolled to top
    maybe set focus on mouse enter
  - can't select multiple segments one by one fast
## PersonDetail
  - create new segment => set person to the new segment => open PersonDetail from StatusBar => new segment is not there (second open is ok)

          # Movie Manager
  - seen date can't be set to already selected date in calendar


                                  *** UPDATE ***

        # MH.UI.WPF
  - RangeSlider - change values by keyboard
  - PopupSlider - close Slider using keyboard
  - add scrollbar for TabPanel
  - ProgressDialog: create host template with ProgressText and ProgressBar

        # MH.UI
## CollectionView
  - don't show expanded root if source is empty
  - expand and scroll to item in GroupByDialog on open
## TabControl
  - Custom tab background color (same color for MediaItems, Segments, MovieManager, ...)

        # Picture Manager
##
  - Rethink hierarchical keywords moving
## TreeViewCategories
  - select/mark searched item after selecting item in search result
## Keywords
  - use only one method to get keywords in which will be filtering by current viewer
## ToggleDialog
  - show selected items on dialog
  - add support for removing GeoName
  - add support for removing Person on Segment
## MediaItems
  - save to file only metadata visible for viewer
## MediaItemsView
  - use Filter on MediaItem type in LoadByTag GetItems
  - open items from group or all with default sort or all sorted by groups or ...?
## Video
  - ThumbPosition for VideoM and VideoItemM as RelationDataAdapter
    + setting this position in VideoDetail
  - keep last video in VideoDetail when leaving MediaItemsView
  - add support for clearing MediaItemThumbSourceConverter ErrorCache and IgnoreCache
## StatusBar
  - limit folder path size
  - add menu with zooms on zoom
## PresentationPanelVM
  - three state button with play/pause/stop and show paused state when video is playing
  - play VideoImages and VideoClips based on settings
## SegmentRect
  - allow mouse events trough
## Search
  - make "plug-ins" for search (keywords, folders, fileNames, dates, people, actors, ...)

        # Movie Managers
## Movies
  - group by genre


                                    *** NEW ***
##
  - Log to file
  - slider with drag point for adjusting slider drag change value where:
    if the point is more away from the slider => change value is smaller
    and if is closer => change value is bigger
    (or zooming time line)
  - Reload Drives (change DB to be able to load Folders, MediaItems and Segments from one drive after initial load was finished)
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
  - zoom to segment
  - highlight segment on mouse hover over person on StatusBar even if it is not visible
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
  - create multiple views of segments like media items views
## GroupByDialog
  - incremental search
## PersonDetail
  - Comment

                                    *** Code ***
##
  - Find generic way to create and open tab from Command
  - OneToManyMultiDataAdapter do it without reference to CoreR
  - don't do CreateDirectory in FolderR.ItemCreate. it can throw exception
  - <ColumnDefinition MinWidth="{DynamicResource ScrollBar.Width}" Width="0"/> can I use this trick somewhere else?
  - ToBoolConverter where Inst is IsNotNull, IsStringEmpty, IsCollectionEmpty, ...
  - IHaveName is applied only to GenreM but the DataTemplate is used for KeywordM as well
  - unused method LinkFolderWithFolderKeyword
  - maybe move all CORE properties that needs to be set from WPF to one class, use ICoreP
  - use private fileds in classes when possible
## SimpleDB
  - better CSV mapping, maybe Dictionary<propName, csvIndex>
  - separate Repository from DB storage (continue with MH.Utils.DB.BaseClasses.Repository)
  - change GetById to return not null and add TryGetById and maybe pass class prop to it
## Components
  - feature that contains only ViewModel and View
  - it can be null and it is created on first use
  - how to create, where hold the instance, dependencies
  - ...

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