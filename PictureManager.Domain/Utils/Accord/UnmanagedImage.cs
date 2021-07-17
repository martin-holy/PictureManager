// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2012
// contacts@aforgenet.com
//
// Accord Imaging Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

// INFO
// This file contains just part of full code. This part is just what is needed for ExhaustiveTemplateMatching!

using System;
using System.Drawing.Imaging;

namespace Accord.Imaging {
  /// <summary>
  /// Image in unmanaged memory.
  /// </summary>
  /// 
  /// <remarks>
  /// <para>The class represents wrapper of an image in unmanaged memory. Using this class
  /// it is possible as to allocate new image in unmanaged memory, as to just wrap provided
  /// pointer to unmanaged memory, where an image is stored.</para>
  /// 
  /// <para>Usage of unmanaged images is mostly beneficial when it is required to apply <b>multiple</b>
  /// image processing routines to a single image. In such scenario usage of .NET managed images 
  /// usually leads to worse performance, because each routine needs to lock managed image
  /// before image processing is done and then unlock it after image processing is done. Without
  /// these lock/unlock there is no way to get direct access to managed image's data, which means
  /// there is no way to do fast image processing. So, usage of managed images lead to overhead, which
  /// is caused by locks/unlock. Unmanaged images are represented internally using unmanaged memory
  /// buffer. This means that it is not required to do any locks/unlocks in order to get access to image
  /// data (no overhead).</para>
  /// 
  /// <para>Sample usage:</para>
  /// <code>
  /// // sample 1 - wrapping .NET image into unmanaged without
  /// // making extra copy of image in memory
  /// BitmapData imageData = image.LockBits(
  ///     new Rectangle( 0, 0, image.Width, image.Height ),
  ///     ImageLockMode.ReadWrite, image.PixelFormat );
  /// 
  /// try
  /// {
  ///     UnmanagedImage unmanagedImage = new UnmanagedImage( imageData ) );
  ///     // apply several routines to the unmanaged image
  /// }
  /// finally
  /// {
  ///     image.UnlockBits( imageData );
  /// }
  /// 
  /// 
  /// // sample 2 - converting .NET image into unmanaged
  /// UnmanagedImage unmanagedImage = UnmanagedImage.FromManagedImage( image );
  /// // apply several routines to the unmanaged image
  /// ...
  /// // conver to managed image if it is required to display it at some point of time
  /// Bitmap managedImage = unmanagedImage.ToManagedImage( );
  /// </code>
  /// </remarks>
  /// 
  public class UnmanagedImage : IDisposable {
    // pointer to image data in unmanaged memory
    private IntPtr imageData;

    // image size
    private int width, height;

    // image stride (line size)
    private int stride;

    // image pixel format
    private PixelFormat pixelFormat;

    // flag which indicates if the image should be disposed or not
    private bool mustBeDisposed = false;


    /// <summary>
    /// Pointer to image data in unmanaged memory.
    /// </summary>
    public IntPtr ImageData {
      get { return imageData; }
    }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width {
      get { return width; }
    }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height {
      get { return height; }
    }

    /// <summary>
    /// Image stride (line size in bytes).
    /// </summary>
    public int Stride {
      get { return stride; }
    }

    /// <summary>
    /// Image pixel format.
    /// </summary>
    public PixelFormat PixelFormat {
      get { return pixelFormat; }
    }

    /// <summary>
    /// Gets the image size, in bytes.
    /// </summary>
    /// 
    public int Bytes {
      get { return stride * height; }
    }

    /// <summary>
    /// Gets the image size, in pixels.
    /// </summary>
    /// 
    public int Size {
      get { return width * height; }
    }

    /// <summary>
    /// Gets the number of extra bytes after the image width is over. This can be computed
    /// as <see cref="Stride"/> - <see cref="Width"/> * <see cref="PixelSize"/>.
    /// </summary>
    /// 
    public int Offset {
      get { return stride - width * PixelSize; }
    }

    /// <summary>
    /// Gets the size of the pixels in this image, in bytes. For 
    /// example, a 8-bpp grayscale image would have pixel size 1.
    /// </summary>
    /// 
    public int PixelSize {
      get { return System.Drawing.Bitmap.GetPixelFormatSize(pixelFormat) / 8; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
    /// </summary>
    /// 
    /// <param name="imageData">Pointer to image data in unmanaged memory.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Image stride (line size in bytes).</param>
    /// <param name="pixelFormat">Image pixel format.</param>
    /// 
    /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
    /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
    /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
    /// 
    public UnmanagedImage(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat) {
      init(imageData, width, height, stride, pixelFormat);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
    /// </summary>
    /// 
    /// <param name="bitmapData">Locked bitmap data.</param>
    /// 
    /// <remarks><note>Unlike <see cref="FromManagedImage(BitmapData)"/> method, this constructor does not make
    /// copy of managed image. This means that managed image must stay locked for the time of using the instance
    /// of unamanged image.</note></remarks>
    /// 
    public UnmanagedImage(BitmapData bitmapData) {
      init(bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, bitmapData.PixelFormat);
    }

    private void init(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat) {
      this.imageData = imageData;
      this.width = width;
      this.height = height;
      this.stride = stride;
      this.pixelFormat = pixelFormat;
    }

    /// <summary>
    /// Destroys the instance of the <see cref="UnmanagedImage"/> class.
    /// </summary>
    /// 
    ~UnmanagedImage() {
      Dispose(false);
    }

    /// <summary>
    /// Dispose the object.
    /// </summary>
    /// 
    /// <remarks><para>Frees unmanaged resources used by the object. The object becomes unusable
    /// after that.</para>
    /// 
    /// <par><note>The method needs to be called only in the case if unmanaged image was allocated
    /// using <see cref="Create(int, int, PixelFormat)"/> method. In the case if the class instance 
    /// was created using constructor, this method does not free unmanaged memory.</note></par>
    /// </remarks>
    /// 
    public void Dispose() {
      Dispose(true);
      // remove me from the Finalization queue 
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose the object.
    /// </summary>
    /// 
    /// <param name="disposing">Indicates if disposing was initiated manually.</param>
    /// 
    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        // dispose managed resources
      }

      // free image memory if the image was allocated using this class
      if ((mustBeDisposed) && (imageData != IntPtr.Zero)) {
        System.Runtime.InteropServices.Marshal.FreeHGlobal(imageData);
        System.GC.RemoveMemoryPressure(stride * height);
        imageData = IntPtr.Zero;
      }
    }
  }
}
