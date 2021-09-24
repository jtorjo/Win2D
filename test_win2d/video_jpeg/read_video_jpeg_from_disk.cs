using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using common2.util;
using common2.util.byte_buffer;
using video.video_jpeg;

namespace test_ui.uwp
{
    public class read_video_jpeg_from_disk : read_video_jpeg_using_offset_file {

        private FileStream stream_;
        public read_video_jpeg_from_disk(string file_name) : base(file_name) {
            load();
        }


        private void load() {
            if (!valid)
                return;
            if (stream_ != null)
                return;
            try {
                stream_ = new FileStream(file_name_, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            } catch (Exception e) {
            }
        }

        public override byte_buffer_temp_holder raw_jpeg_buffer(int idx) {
            load();
            if (stream_ == null)
                // some error loading the video
                return null;

            var ( start, size) = offsets(idx);
            if (size <= 0)
                return null;
            stream_.Seek(start, SeekOrigin.Begin);

            var buff = byte_buffer_holder_array.inst.get_temp_holder((int)size);
            buff.read(stream_);
            return buff;
        }

        public override void Dispose() {
            base.Dispose();
            stream_?.Close();
            stream_?.Dispose();
            stream_ = null;
        }
    }
}
