using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;

namespace Clipple.Types;

public class MediaClass
{
    public MediaClass(string name, string description)
    {
        Name = name;
        Description = description;
    }

    #region Properties
    public string Name { get; set; }
    public string Description { get; set; }
    #endregion

    public static readonly MediaClass Clip = new("Clip", "Final edited clip");
    public static readonly MediaClass RawMedia = new("Raw media", "Raw unedited media");
    public static readonly MediaClass Automatic = new("Automatic import", "Media that has been automatically imported");
    public static readonly MediaClass Archival = new("Archival", "Media that is being stored for archival purposes");

    public static readonly ObservableCollection<MediaClass> MediaClasses = new(new List<MediaClass>()
    {
        Clip,
        RawMedia,
        Automatic,
        Archival,
    });

    public static int MediaClassCount { get; } = MediaClasses.Count;
}