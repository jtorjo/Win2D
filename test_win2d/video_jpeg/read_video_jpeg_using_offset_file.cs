using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using common2.util;
using test_ui.uwp;

namespace video.video_jpeg
{
    public class video_to_jpeg { 
                // .offset2 - since we started using segments
        public const string OFFSET_SUFFIX = ".offset3";
        public const string SEGMENT_SUFFIX = ".segment3";
    }

    public abstract class read_video_jpeg_using_offset_file : read_video_jpeg_base
    {

        private const double SEGMENT_TOLERANCE = 0.00001;

        private class offset_info {
            public long offset = 0;
            public double time_ms = 0;
            public long len = 0;
        }
        private List<offset_info> offsets_ = new List<offset_info>();
        // just cause i'm too lazy to implement IComparer for offset_info
        private List<double> times_ = new List<double>();

        protected readonly string file_name_;
        private string src_file_name_hash_;

        private int width_, height_;
        private long len_;

        public string src_file_name_hash => src_file_name_hash_;

        public bool is_full => is_full_;


        public int count() => offsets_.Count;

        // if true, we're converted all file
        private bool is_full_;

        private bool valid_ = true;
        public bool valid => valid_;

        public string v0_name => v0_name_;


        public (double, double) times() => (times_[0], times_.Last());

        // start-ms, len-ms
        private List<(double,double)> segments_ = new List<(double, double)>();

        // only for testing
        private string v0_name_;

        protected read_video_jpeg_using_offset_file(string file_name)  {
            file_name_ = file_name;

            try {
                load_offsets();
                len_ = new FileInfo(file_name).Length;
            } catch (Exception e) {
                valid_ = false;
            }
        }
        public void add_segment(double start_ms, double len_ms) {
            segments_.Add((start_ms, len_ms));
        }

        private void load_offsets() {
            load_offsets_v3();
        }

        internal static void del_stream_file(string stream_file) {
            try {
                if (File.Exists(stream_file))
                    File.Delete(stream_file);
            } catch {
            }

            try {
                if (File.Exists(stream_file + ".offset3"))
                    File.Delete(stream_file + ".offset3");
                if (File.Exists(stream_file + ".segment3"))
                    File.Delete(stream_file + ".segment3");
            } catch {
            }
        }

        public bool contains_ms(double ms) {
            if (is_full_)
                return true;
            return segments_.Any(s => ms >= s.Item1 && ms < s.Item1 + s.Item2);
        }
        public bool contains_segment(double start_ms, double len_ms) {
            if (is_full_)
                return true;
            return segments_.Any(s => start_ms >= s.Item1 - SEGMENT_TOLERANCE && start_ms + len_ms <= s.Item1 + s.Item2 + SEGMENT_TOLERANCE);
        }

        private void load_offsets_v3() {
            // v0 - initial version
            // v1 - use hash
            // v2 - use segments
            // v3 - use segments + write the length from the get-go - the problem with auto computing length is that
            //                     we may have 2 consecutive offsets, and the latter to be less than the former. then we'd end up mis-computing the length
            if (!File.Exists(file_name_ + video_to_jpeg.SEGMENT_SUFFIX) || !File.Exists(file_name_ + video_to_jpeg.OFFSET_SUFFIX)) {
                valid_ = false;
                return;
            }
            var lines = File.ReadAllLines(file_name_ + video_to_jpeg.SEGMENT_SUFFIX);
            var version = lines[0];
            src_file_name_hash_ = lines[1];
            var wh_words = lines[2].Split(' ');
            width_ = int.Parse(wh_words[0]);
            height_ = int.Parse(wh_words[1]);
            for (int i = 3; i < lines.Length; ++i) {
                if (lines[i].Trim() == "")
                    continue;
                var sl_words = lines[i].Split(' ');
                var start_ms = double.Parse(sl_words[0], CultureInfo.InvariantCulture);
                var len_ms = double.Parse(sl_words[1], CultureInfo.InvariantCulture);
                segments_.Add((start_ms, len_ms));
            }

            is_full_ = segments_.First().Item1 < 0 && segments_.First().Item2 < 0;
            if (is_full_)
                segments_.Clear();

            var offset_values = File.ReadAllText(file_name_ + video_to_jpeg.OFFSET_SUFFIX); 
            var words = offset_values.Split(' ').Where(w => w != "").ToList();
            for (int i = 0; i < words.Count; i += 3) {
                try {
                    var offset = long.Parse(words[i]);
                    var len = long.Parse(words[i + 1]);
                    var ms = double.Parse(words[i + 2], CultureInfo.InvariantCulture);
                    offsets_.Add(new offset_info {offset = offset, time_ms = ms, len = len});
                } catch (Exception e) {
                    valid_ = false;
                    return;
                }
            }

            offsets_ = offsets_.OrderBy(o => o.time_ms).ToList();
            times_ = offsets_.Select(o => o.time_ms).ToList();

        }

        protected (long, long) offsets(int idx) {
            var size = offsets_[idx].len;
            var start = offsets_[idx].offset;
            return (start, size);
        }

        public override double true_ms(double ms) {
            return ms;
        }

        // always returns the item right BEFORE ms
        // so we never get into the issue of sometimes returning the same item twice, which if playing, could actually look sluggish at times
        protected override int ms_to_idx(double ms) {
            var idx = times_.BinarySearch(ms);
            if (idx < 0)
                idx = ~idx;
            if (idx > 0)
                idx --;
            if (idx >= count())
                idx = count() - 1;
            //logger.Debug("ms to idx " + ms + " to " + idx);
            return idx;
        }

        public override void Dispose() {
            
        }
    }
}
