# Picture Manager

version always 1.0 :)

_Usable but not tested by others yet!_

Picture Manager is a simple image and video viewer with the ability of editting keywords like rating, people, hierarchical keywords, comments and geo names.

## Browser

### 1. Side bar

### 1.1. Folders

- ### Favorite Folders
  Favorite Folders are shortcuts to Folders. They can be added by `right-click` on the Folder choosing **Add to Favorites** or removed by `right-click` on the favorite folder choosing **Remove from Favorites**. Folder tree is expanded to the chosen Favorite Folder by `left-click` on the favorite folder.
- ### Folders
  Folders are main entry points for browsing media items. Media items are imported and thumbnails are created after `left-click` on a folder. Media items import and thumbnail creation is done only once. Each next load is much faster. Media items can be also loaded from subdirectories by `Shift + left-click` on the folder.
  A folder can be moved by `drag-and-drop` or copied with `Ctrl + drag-and-drop`. Folders can also be renamed or deleted from `right-click` menu on the folder.

### 1.2. Keywords

Keywords are additional data for media items. Media items can be filtered or loaded in this category only if they have been imported through `left-click` on the Folder.
  
 Media items can be loaded by choosing **Load by this** from the `right-click` menu on _People_, _Keywords_ and _GeoNames_ or the `left-click` on the _Folder Keywords_. Loading can also be applied to subdirectories with the `Shift + left-click`.
  
 The filter is applied to the previously loaded media items and can by edited by:

- `left-click` for one or more selected keywords on loaded media items
- `Ctrl + left-click` for having all selected keywords on loaded media items
- `Alt + left-click` for excluding media items with selected keywords

  The Keywords on media items can be edited by entering the edit mode with `Ctrl + E`. In this mode the Keywords will be added or removed from selected media items by `left-click` on _Rating_, _Person_, _Keyword_ or _GeoName_. The edit can be saved with `Ctrl + S` or canceled with `Ctrl + Q`.

  The Comments on media items can be edited with `Ctrl + K` without the need of entering the edit mode.

  1. Ratings

  2. Sizes
     Sizes show a range of MegaPixels of the loaded media items. It can be used to filter the loaded media items by adjusting this range.
  3. People
     This category can contain a group of people or only a person.
     A group can be created by choosing **New Group** from the `right-click` menu on the category. It can also be renamed or deleted from `right-click` menu on the group. When a group is deleted, all people in the group are moved to the root of this category.
     A person can be created by choosing **New** from the `right-click` menu on the category or the group. It can be renamed or deleted from the `right-click` menu on the person. When a person is deleted, it is just deleted from the internal database and not from the actual media items!
     People can be moved between the groups by `drag-and-drop`.
  4. Folder Keywords
     _Folder Keywords_ are something like _Folders_ but one _Folder Keyword_ can be linked to multiple folders. The contents of the folders will be merged under one _Folder Keyword_.
     For example: When two folders (`D:\Pictures\subfolder\subfolder\` and `P:\subfolder\`) are set as _Folder Keywords_ from the `right-click` menu on the folder, the contents of these folders will be merged under the _Folder Keywords_ category.
  5. Keywords
     This category can contain a group of keywords or only a keyword.
     A group can be created by choosing **New Group** from the `right-click` menu on the category. It can also be renamed or deleted from the `right-click` menu on the group. When a group is deleted, all keywords in the group are moved to the root of this category.
     A keyword can be created by choosing **New** from the `right-click` menu on the category, a group or another keyword and renamed or deleted from the `right-click` menu on the keyword. When a keyword is deleted, it is just deleted from the internal database and not from the actual media items!
     Keywords can be moved between groups by `drag-and-drop`. Sort order of the keywords can be changed by `drag-and-drop`.
  6. GeoNames
     _GeoNames_ are added from http://geonames.org by selecting the media items with GPS coordinates and `left-click` on **Tools/GeoNames** or by `right-click` on _GeoNames_ category and entering GPS coordinates in this format **N36.75847,W3.84609**.
  7. Filters
  8. Viewers
     A viewer is something like a workspace with included and excluded folders. The folders can be added from the `right-click` menu on a viewer or removed from the `right-click` menu on a folder.
     When no viewer is present, the application works with all available folders.

### 2. Thumbnails

    The size of thumbnails can be set in **Tools/Settings**. Thumbnails can be created again when the size is changed through **Media Items/Rebuild Thumbnails**. Thumbnails can be zoomed in or out with `Ctrl + mouse scroll` without changing the size in the settings.
    The selection can be made by `left-click` or with the combination `Shift or Ctrl + left-click` or selecting all with `Ctrl + A`.

## 3. Viewer

Viewer can be opened from the browser by `double-click` on the thumbnail and closed by `double-click` or `Esc`. The navigation between media items is done by `left and right arrow key` or with `mouse-scroll`. An image can be zoomed to 100% by holding `left-mouse-button` or changed to any zoom with `Ctrl + mouse-scroll`. An image can also be moved around if it is bigger than the screen by holding `left-mouse-button`.
The presentation can be turned on/off with `Ctrl + P` with a 3-second delay.
