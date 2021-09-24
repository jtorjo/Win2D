using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using common2.util.byte_buffer;

namespace test_ui.uwp
{
    public abstract class read_video_jpeg_base : IDisposable
    {

        protected read_video_jpeg_base() {
        }

        public abstract byte_buffer_temp_holder raw_jpeg_buffer(int idx);


        // always returns the item right BEFORE ms
        // so we never get into the issue of sometimes returning the same item twice, which if playing, could actually look sluggish at times
        protected abstract int ms_to_idx(double ms);

        public async Task<BitmapImage> bitmap_image_at_ms(double ms, double resize_width, double resize_height) {
            return await bitmap_image_at_idx(ms_to_idx(ms), resize_width, resize_height);
        }
        public async Task<BitmapImage> bitmap_image_at_idx(int idx, double resize_width, double resize_height) {
            var buffer = raw_jpeg_buffer(idx);
            if (buffer == null)
                // this is only used when we can return something while we're writing
                // (at start, there will be nothing to see)
                return null;
            // https://stackoverflow.com/questions/36019595/how-to-copy-and-resize-image-in-windows-10-uwp
            using (var stream = new InMemoryRandomAccessStream()) {
                await buffer.write_from_stream_async(stream);
                stream.Seek(0); // Just to be sure.

                BitmapImage sourceImage = new BitmapImage();
                if ( resize_width > 0)
                    sourceImage.DecodePixelWidth = (int)resize_width;
                else if (resize_height > 0)
                    sourceImage.DecodePixelHeight = (int)resize_height;

                await sourceImage.SetSourceAsync(stream);
                return sourceImage;
            }
        }

        public async Task<SoftwareBitmap> disposable_soft_bmp_at_ms(double ms) {
            return await disposable_soft_bmp_at_idx(ms_to_idx(ms));
        }

        internal byte_buffer_temp_holder buffer_at_ms(double ms) {
            return raw_jpeg_buffer(ms_to_idx(ms));
        }

        public async Task<CanvasBitmap> canvas_bmp_at_ms(double ms, CanvasDevice canvas_device) {
            return await canvas_bmp_at_idx(ms_to_idx(ms), canvas_device);
        }

        // return the true ms - what the returned bitmap corresponding to this will actually be
        //
        // this can be different from ms when reading while writing
        public abstract double true_ms(double ms);


        // avg: 4.5ms (mem)
        //      4.9ms (disk) / 5.15 (disk, backwards)
        public async Task<SoftwareBitmap> disposable_soft_bmp_at_idx(int idx) {
            var buffer = raw_jpeg_buffer(idx);
            if (buffer == null)
                // this is only used when we can return something while we're writing
                // (at start, there will be nothing to see)
                return null;

            // this is jpeg - read it as such
            try {
                using (var stream = new InMemoryRandomAccessStream()) {
                    await buffer.write_from_stream_async(stream);
                    stream.Seek(0); // Just to be sure.
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var soft = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(),
                                                                    ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb);
                    return soft;
                }
            } catch (Exception e) {
                return null;
            } finally {
                buffer?.Dispose();
            }
        }

        // this is just for testing
        public void to_file(double ms, string file_name) {
            var idx = ms_to_idx(ms);
            var buffer = raw_jpeg_buffer(idx);
            byte[] bytes = new byte [buffer.size];
            buffer.bytes.CopyTo(bytes, 0);
            File.WriteAllBytes(file_name, bytes);
            buffer.Dispose();
        }

         // avg : 2.9ms (mem)
         // avg : 3.0ms (disk), 3.0ms (disk, backwards)
         /* IMPORTANT: when the canvas device is actually in use, results will vary

            I got values from 5.0ms to 6.5ms - AND MOST IMPORTANT: I should not call this from within .Draw() event
            The reason for the 5-6.5 ms values is that the LoadAsync() sometimes waits without doing nothing. But, it basically
            does not take CPU, which is insanely awesome.
         
            So to summarize, the results are AMAZING
          */
        public async Task<CanvasBitmap> canvas_bmp_at_idx(int idx, CanvasDevice canvas_device) { 
            var buffer = raw_jpeg_buffer(idx);
            if (buffer == null)
                // this is only used when we can return something while we're writing
                // (at start, there will be nothing to see)
                return null;

            // this is jpeg - read it as such
            try {
                using (var stream = new InMemoryRandomAccessStream()) {
                    await buffer.write_from_stream_async(stream);
                    stream.Seek(0); // Just to be sure.
                    var bmp = await CanvasBitmap.LoadAsync(canvas_device, stream, 96, CanvasAlphaMode.Premultiplied);

                    return bmp;
                }
            } catch (Exception e) {
                // it did happen to me once 
                // Message=The component cannot be found. (Exception from HRESULT: 0x88982F50)
                return null;
            } finally {
                buffer?.Dispose();
            }
        }

         public abstract void Dispose();
    }
}
