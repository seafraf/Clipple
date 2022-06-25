using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.PPA
{
    /// <summary>
    /// Represents either a video or clip post processing action
    /// </summary>
    public abstract class PostProcessingAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameter">Either the video or clip that this action should be ran on</param>
        public PostProcessingAction(object parameter)
        {
            Parameter = parameter;
        }

        /// <summary>
        /// Runs this post processing action.
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// A long description for this post processing action.
        /// </summary>
        public abstract string LongDescription { get; }

        /// <summary>
        /// A short description for this post processing action.  This is used in this object's ToString method
        /// </summary>
        public abstract string ShortDescription { get; }

        /// <summary>
        /// The action to run on the video or clip
        /// </summary>
        private object Parameter { get; set; }

        /// <summary>
        /// Helper function to cast param to video
        /// </summary>
        protected VideoViewModel Video => (VideoViewModel)Parameter;

        /// <summary>
        /// Helper function to cast param to clip
        /// </summary>
        protected ClipViewModel Clip => (ClipViewModel)Parameter;

        public override string? ToString()
        {
            return ShortDescription;
        }
    }
}
