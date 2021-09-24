using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Threading.Tasks;
using Windows.Storage;
using test_ui.uwp;
using Microsoft.Graphics.Canvas;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_win2d
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private object lock_ = new object();
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void MainPage_OnLoaded(object sender, RoutedEventArgs e) {
        }

        private void create_resources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            Task.Run(load_bitmaps);
            //Task.Run(load_bitmaps_sequencial);
        }

        private async void load_bitmaps_sequencial() {
            var device = ctrl.Device;
            var file = ApplicationData.Current.LocalFolder.Path + "\\0b5f85674ef29e6d6acf5a51cae9a2de-354-637677695863517970-0-37566.cinematic-stream";
            var read = new read_video_jpeg_from_disk(file);

            List<CanvasBitmap> bmps = new List<CanvasBitmap>();
            // warmup
            for (int i = 0; i < 250; ++i)
                await read.canvas_bmp_at_idx(i, device);
            for (int i = 0; i < 250; ++i)
                await read.canvas_bmp_at_idx(i, device);

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 250; ++i)
                bmps.Add(await read.canvas_bmp_at_idx(i, device));
            Debug.WriteLine("loaded " + watch.ElapsedMilliseconds);
            return;
        }

        private async void load_bitmaps() {
            var device = ctrl.Device;
            var file = ApplicationData.Current.LocalFolder.Path + "\\0b5f85674ef29e6d6acf5a51cae9a2de-354-637677695863517970-0-37566.cinematic-stream";
            var read = new read_video_jpeg_from_disk(file);

            // warmup
            for (int i = 0; i < 250; ++i)
                await read.canvas_bmp_at_idx(i, device);
            for (int i = 0; i < 250; ++i)
                await read.canvas_bmp_at_idx(i, device);

            int error_count = 0;
            List<CanvasBitmap> bmps = new List<CanvasBitmap>();
            //for (int i = 0; i < 50; ++i)
              //  bmps.Add(await read.canvas_bmp_at_idx(i, device));
            //return;

            List<int> indexes = new List<int>();
            for (int i = 0; i < 250; ++i)
                indexes.Add(i);

            var watch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0,5).Select(i => Task.Run(async () => { 
                while (true) {
                    int idx = -1;
                    lock(lock_) {
                        if (indexes.Count < 1)
                            break;
                        idx = indexes[0];
                        indexes.RemoveAt(0);
                    }
                    var bmp = await read.canvas_bmp_at_idx(idx, device);
                    if ( bmp != null) {
                        bmps.Add(bmp);
                    } else {
                        ++error_count;
                        lock(lock_)
                            indexes.Add(idx);
                    }
                }
            })).ToArray();
            Task.WaitAll(tasks);
            Debug.WriteLine("loaded " + watch.ElapsedMilliseconds + " errors=" + error_count);
        }
    }
}
