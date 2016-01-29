using System;
using System.Runtime.InteropServices;

namespace PictureManager.ShellStuff.Interfaces {
  /// <summary>
  /// 
  /// </summary>
  [ComImport()]
  [GuidAttribute("000214E6-0000-0000-C000-000000000046")]
  [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IShellFolder {
    /// <summary>
    /// Parses the display name.
    /// </summary>
    /// <param name="hwndOwner">The HWND owner.</param>
    /// <param name="pbcReserved">The PBC reserved.</param>
    /// <param name="lpszDisplayName">Display name of the LPSZ.</param>
    /// <param name="pchEaten">The PCH eaten.</param>
    /// <param name="ppidl">The ppidl.</param>
    /// <param name="pdwAttributes">The PDW attributes.</param>
    /// <returns></returns>
    [PreserveSig]
    int ParseDisplayName(IntPtr hwndOwner,
                         IntPtr pbcReserved,
                         [MarshalAs(UnmanagedType.LPWStr)] string lpszDisplayName,
                         out int pchEaten, //A pointer to a ULONG value that receives the number of characters of the display name that was parsed. 
                         out IntPtr ppidl,//When this method returns, contains a pointer to the pointer to an item identifier list (PIDL) for the object.
                         out int pdwAttributes);//The value used to query for file attributes. If not used, it should be set to NULL. 

    /// <summary>
    /// Enums the objects.
    /// </summary>
    /// <param name="hwndOwner">The HWND owner.</param>
    /// <param name="grfFlags">The GRF flags.</param>
    /// <param name="ppenumIDList">The ppenum ID list.</param>
    /// <returns></returns>
    [PreserveSig]
    int EnumObjects(IntPtr hwndOwner,
                    [MarshalAs(UnmanagedType.U4)] ESHCONTF grfFlags,
                    ref IEnumIDList ppenumIDList);

    /// <summary>
    /// Binds to object.
    /// </summary>
    /// <param name="pidl">The pidl.</param>
    /// <param name="pbcReserved">The PBC reserved.</param>
    /// <param name="riid">The riid.</param>
    /// <param name="ppvOut">The PPV out.</param>
    /// <returns></returns>
    [PreserveSig]
    int BindToObject(IntPtr pidl,
                     IntPtr pbcReserved,
                     ref Guid riid,
                     ref IShellFolder ppvOut);

    /// <summary>
    /// Binds to storage.
    /// </summary>
    /// <param name="pidl">The pidl.</param>
    /// <param name="pbcReserved">The PBC reserved.</param>
    /// <param name="riid">The riid.</param>
    /// <param name="ppvObj">The PPV obj.</param>
    /// <returns></returns>
    [PreserveSig]
    int BindToStorage(IntPtr pidl,
                      IntPtr pbcReserved,
                      ref Guid riid,
                      IntPtr ppvObj);

    /// <summary>
    /// Compares the Ids.
    /// </summary>
    /// <param name="lParam">The l param.</param>
    /// <param name="pidl1">The pidl1.</param>
    /// <param name="pidl2">The pidl2.</param>
    /// <returns></returns>
    [PreserveSig]
    int CompareIDs(IntPtr lParam,
                   IntPtr pidl1, IntPtr pidl2);

    /// <summary>
    /// Creates the view object.
    /// </summary>
    /// <param name="hwndOwner">The HWND owner.</param>
    /// <param name="riid">The riid.</param>
    /// <param name="ppvOut">The PPV out.</param>
    /// <returns></returns>
    [PreserveSig]
    int CreateViewObject(IntPtr hwndOwner,
                         ref Guid riid,
                         IntPtr ppvOut);

    /// <summary>
    /// Gets the attributes of.
    /// </summary>
    /// <param name="cidl">The cidl.</param>
    /// <param name="apidl">The apidl.</param>
    /// <param name="rgfInOut">The RGF in out.</param>
    /// <returns></returns>
    [PreserveSig]
    int GetAttributesOf(int cidl,
                        IntPtr apidl,
                        [MarshalAs(UnmanagedType.U4)] ref ESFGAO rgfInOut);

    /// <summary>
    /// Gets the UI object of.
    /// </summary>
    /// <param name="hwndOwner">The HWND owner.</param>
    /// <param name="cidl">The cidl.</param>
    /// <param name="apidl">The apidl.</param>
    /// <param name="riid">The riid.</param>
    /// <param name="rgfReserved">The RGF reserved.</param>
    /// <param name="ppvOut">The PPV out.</param>
    /// <returns></returns>
    [PreserveSig]
    int GetUIObjectOf(IntPtr hwndOwner,//A handle to the owner window that the client should specify if it displays a dialog box or message box.
                      int cidl,//The number of file objects or subfolders specified in the apidl parameter.
                      ref IntPtr apidl,//The address of an array of pointers to ITEMIDLIST structures
                      ref Guid riid,//The identifier of the Component Object Model (COM) interface object to return.
                      out int rgfReserved,// reserved
                      ref IUnknown ppvOut);//A pointer to the requested interface. If an error occurs, a NULL pointer is returned in this address.

    /// <summary>
    /// Gets the display name of.
    /// </summary>
    /// <param name="pidl">The pidl.</param>
    /// <param name="uFlags">The u flags.</param>
    /// <param name="lpName">Name of the lp.</param>
    /// <returns></returns>
    [PreserveSig]
    int GetDisplayNameOf(IntPtr pidl,
                         [MarshalAs(UnmanagedType.U4)] ESHGDN uFlags,
                         ref STRRET_CSTR lpName);

    /// <summary>
    /// Sets the name of.
    /// </summary>
    /// <param name="hwndOwner">The HWND owner.</param>
    /// <param name="pidl">The pidl.</param>
    /// <param name="lpszName">Name of the LPSZ.</param>
    /// <param name="uFlags">The u flags.</param>
    /// <param name="ppidlOut">The ppidl out.</param>
    /// <returns></returns>
    [PreserveSig]
    int SetNameOf(IntPtr hwndOwner,
                  IntPtr pidl,
                  [MarshalAs(UnmanagedType.LPWStr)] string lpszName,
                  [MarshalAs(UnmanagedType.U4)] ESHCONTF uFlags,
                  ref IntPtr ppidlOut);
  };
}