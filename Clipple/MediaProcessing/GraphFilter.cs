using Clipple.ViewModel;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    internal abstract class GraphFilter
    {
        public unsafe GraphFilter()
        {
            FilterGraph = FE.Null(ffmpeg.avfilter_graph_alloc());
        }

        #region Properties

        /// <summary>
        /// Reference to this filter's graph
        /// </summary>
        protected unsafe AVFilterGraph* FilterGraph { get; }

        /// <summary>
        /// Reference to this filter's input buffer
        /// </summary>
        public abstract unsafe AVFilterContext* GetBufferContext(int inputStreamIndex);

        /// <summary>
        /// Reference to this filter's output buffer
        /// </summary>
        public abstract unsafe AVFilterContext* BufferSinkContext { get; }
        #endregion

        #region Members
        protected unsafe AVFilterContext* lastContext;
        #endregion

        protected unsafe void ResetLastContext()
        {
            lastContext = null;
        }

        protected unsafe AVFilterContext* AddUnlinkedFilter(string name, string? args = null)
        {
            AVFilterContext* context;
            FE.Code(ffmpeg.avfilter_graph_create_filter(&context, 
                FE.Null(ffmpeg.avfilter_get_by_name(name)), null, args, null, FilterGraph));

            return context;
        }

        protected unsafe AVFilterContext* AddFilter(string name, string? args = null)
        {
            AVFilterContext* context = AddUnlinkedFilter(name, args);

            // Back link unless this is the first filter
            if (lastContext != null)
                FE.Code(ffmpeg.avfilter_link(lastContext, 0, context, 0));

            lastContext = context;
            return context;
        }
    }
}
