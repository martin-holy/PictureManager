*** Picture Manager - Common ***
[N] - New
[U] - Update
[B] - Bug fix

2.33.0:
	- [N] MenuFactory: Keyword menu: move item to group

2.32.0:
	- [B] ImageResizeDialog: not preserve folders was ingnored
	- [B] ImageResizeDialog: resizing only one image
	- [C] AboutDialog: opening url resolved on UI platform
	- [C] BindingU update
	- [C] dependency versions
	- [C] MediaItemVM: MainWindow.IsInViewMode
	- [C] PersonDetailVM: menu moved to MenuFactory
	- [C] SegmentRect refactoring
	- [C] SegmentRectS: segment DeleteCache
	- [C] SegmentsViewsVM: method rename
	- [N] CoreVM: ExportSegmentsToCommand
	- [N] CoreVM: ResizeImages selected/to forlder/in folder commands
	- [N] ImageS: BuildXmp
	- [N] ImageS: GetPeopleSegmentsKeywords
	- [N] MainMenu - Segments: Set selected as same person
	- [N] MainMenu: Add MediaItems view tab
	- [N] MainMenuVM: Delete selected segments
	- [N] MainMenuVM: HideMenuItems method
	- [N] MainMenuVM: SegmentsDrawer commands
	- [N] MenuFactory: Folder: resize images in/to folder
	- [N] MenuFactory: FolderMenu: ExportSegmentsToCommand
	- [N] MenuFactory: PersonDetail
	- [N] MenuFactory: SegmentsDrawerMenu
	- [N] PersonCategoryGroupMenu: GroupMoveInItemsCommand
	- [N] PersonDetailVM: AddSelectedToPersonsTopSegmentsCommand and RemoveSelectedFromPersonsTopSegmentsCommand in menu
	- [N] PersonDetailVM: Menu
	- [N] PersonS: AddToTopSegments and RemoveFromTopSegments methods
	- [N] PersonTreeCategory: Move items to group
	- [N] Res: IconDrawerRemove
	- [N] Res: IconSegmentAdd and IconSegmentRemove
	- [N] Res: IconSegmentDelete
	- [N] Res: IconSegmentEdit and IconSegmentPerson
	- [N] Res: IconSegmentNew
	- [N] Res: IconTabPlus
	- [N] SegmentRectS: GetBy method
	- [N] SegmentRectS: RemoveIfContains
	- [N] SegmentRectVM: CanCreateNew prop
	- [N] SegmentRectVM: IsEditEnabled prop
	- [N] SegmentS: DeleteCache metohd
	- [N] SegmentsDrawerVM: RemoveSelectedCommand
	- [N] SegmentVM: AddSelectedToPersonsTopSegmentsCommand and RemoveSelectedFromPersonsTopSegmentsCommand
	- [N] SegmentVM: DeleteSelectedCommand
	- [N] ToolsTabsVM: ItemMenuFactory
	- [U] ExportSegmentsDialog: autoRun ctor with destDir
	- [U] ImageResizeDialog: ctor with destDir
	- [U] MainMenuVM: ResizeImages -> ResizeSelectedImages
	- [U] MediaItemsViewsVM: AddViewCommand icon
	- [U] MenuFactory: RatingTreeMenu
	- [U] PersonVM: Sizes for views
	- [U] SegmentR: check cache path before delete
	- [U] SegmentRectS: public EditLimit
	- [U] SegmentRectS: SetCurrent returns bool
	- [U] SegmentVM: AddEmptyViewCommand
	- [U] SegmentVM: icons for add/remove selected segments to/from top segments
	- [U] set MainWindow.IsInViewMode to false onMainTabsTabActivated
	- [U] Update all views with person when is moved to different group