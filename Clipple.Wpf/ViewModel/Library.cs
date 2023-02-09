﻿using Clipple.Types;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using FFmpeg.AutoGen;
using static Clipple.ViewModel.LibraryEditTagsTask;
using MaterialDesignColors.Recommended;
using System.Data;

namespace Clipple.ViewModel
{
    public class Library : ObservableObject
    {
        public Library()
        {
            Database = new(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                Application.ProductName, 
                "library.db"));

            var collection = Database.GetCollection<Media>();
            collection.EnsureIndex(x => x.FilePath, true);

            MediaSource = new ObservableCollection<Media>();
            Media = new ListCollectionView(MediaSource)
                    {
                        Filter = MediaFilter
                    };

            MediaSource.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(SearchHelp));
            };
        }

        #region Members
        private          Media?          selectedMedia;
        private readonly HashSet<Media>  dirtyMedia = new();
        private          string?         searchText = string.Empty;
        private          string?         activeSearchText = string.Empty;
        private MediaFilterMode          filterTagsMode = MediaFilterMode.All;
        private MediaFilterMode          filterClassesMode = MediaFilterMode.Any;
        private MediaFilterMode          filterFormatsMode = MediaFilterMode.All;
        private int                      activeFilterCount = 0;
        #endregion

        #region Properties
        private LiteDatabase Database { get; }

        /// <summary>
        /// The source collection for the Media collection view
        /// </summary>
        private ObservableCollection<Media> MediaSource { get; }

        /// <summary>   
        /// All of the media in the library
        /// </summary>
        public ListCollectionView Media { get; }

        /// <summary>
        /// The selected media entry.  Can be null if the Media list is empty
        /// </summary>
        public Media? SelectedMedia
        {
            get => selectedMedia;
            set => SetProperty(ref selectedMedia, value);
        }

        /// <summary>
        /// Search phrase
        /// </summary>
        public string? SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                OnPropertyChanged(nameof(SearchHelp));

                // Only set to null by the clear button, so we can use this to apply filters instead of the requirement of pressing enter
                if (value == null)
                    ApplyFilters();
            }
        }

        /// <summary>
        /// The text that was used to generate the current filtered list
        /// </summary>
        public string? ActiveSearchText
        {
            get => activeSearchText;
            set
            {
                SetProperty(ref activeSearchText, value);
                OnPropertyChanged(nameof(SearchHelp));
            }
        }

        /// <summary>
        /// Helper text for the search bar.  Due to a bug with XAML this must be a property
        /// </summary>
        public string SearchHelp
        {
            get
            {
                if (ActiveSearchText != SearchText)
                    return "Press enter to search";

                return $"Displaying {Media.Count} of {MediaSource.Count}";
            }
        }

        /// <summary>
        /// Tags used to filter displayed media
        /// </summary>
        public ObservableCollection<Tag> FilterTags { get; } = new();

        /// <summary>
        /// Classes used to filter displayed media
        /// </summary>
        public ObservableCollection<MediaClassFilter> FilterClasses { get; } = new();

        /// <summary>
        /// Types used to filter displayed media
        /// </summary>
        public ObservableCollection<ContainerFormat> FilterFormats { get; } = new();

        /// <summary>
        /// Mode used to filter tags
        /// </summary>
        public MediaFilterMode FilterTagsMode
        {
            get => filterTagsMode;
            set => SetProperty(ref filterTagsMode, value);
        }

        /// <summary>
        /// Mode used to filter classes
        /// </summary>
        public MediaFilterMode FilterClassesMode
        {
            get => filterClassesMode;
            set => SetProperty(ref filterClassesMode, value);
        }

        /// <summary>
        /// Mode used to filter formats
        /// </summary>
        public MediaFilterMode FilterFormatsMode
        {
            get => filterFormatsMode;
            set => SetProperty(ref filterFormatsMode, value);
        }

        /// <summary>
        /// The number of active filters.  Only set when filter are applied (when ApplyFilters method is called)
        /// </summary>
        public int ActiveFilterCount
        {
            get => activeFilterCount;
            set => SetProperty(ref activeFilterCount, value);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Creates a list of Media from the Media collection in the database.
        /// </summary>
        /// <returns>Task</returns>
        public async Task<List<Media>> GetMediaFromDatabase()
        {
            return await Task.Run(() => Database.GetCollection<Media>().FindAll().ToList());
        }

        /// <summary>
        /// Takes a list of media and initialises it for use in the library.  This will set the Media property.
        /// </summary>
        /// <returns>Task</returns>
        public async Task LoadMedia(List<Media> mediaList)
        {
            var errors = new List<Exception>();
            foreach (var media in mediaList)
            {
                try
                {
                    media.Initialise();

                    media.MediaRequestUpdate += OnMediaRequestUpdate;
                    media.MediaDirty += OnMediaDirty;
                    media.MediaRequestDelete += OnMediaRequestDelete;
                    await media.BuildOrCacheResources();

                    Media.AddNewItem(media);
                    Media.CommitNew();
                }
                catch (Exception e)
                {
                    // Media that fails to load should be removed from the library
                    Database.GetCollection<Media>().Delete(media.ID);

                    errors.Add(e);
                }
            }

            if (errors.Count > 0)
                App.Notifications.NotifyException("Errors loading library", errors.ToArray());
        }

        /// <summary>
        /// Called when media changes and requires saving.
        /// </summary>
        private void OnMediaDirty(object? mediaObj, EventArgs _)
        {
            if (mediaObj is Media media)
                dirtyMedia.Add(media);
        }

        /// <summary>
        /// Called when media changes and requires saving.  This event is rate limited and only called a max of one time
        /// per second per media. 
        /// </summary>
        private void OnMediaRequestUpdate(object? mediaObj, EventArgs _)
        {
            if (mediaObj is not Media media)
                return;
            
            dirtyMedia.Remove(media);
            Database.GetCollection<Media>().Update(media);
        }

        /// <summary>
        /// Called when a media a user has requested to delete a media file.  This operation needs will:
        /// - Unload the media from the editor if it is loaded
        /// - Delete the file from the disk if deleteFile is true
        /// - Delete the media's cache directory
        /// - Remove the media from the database
        /// - Remove the media from any collection in the library that might have it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMediaRequestDelete(object? mediaObj, bool deleteFile)
        {
            if (mediaObj is not Media media) 
                return;
            
            // Make sure nothing is using files from this media
            if (App.ViewModel.MediaEditor.Media == media)
                App.ViewModel.MediaEditor.Media = null;

            media.DeleteCache();

            // Delete the media file if the user has requested that
            if (deleteFile)
                File.Delete(media.FilePath);

            // Remove from the database
            Database.GetCollection<Media>().Delete(media.ID);

            // Remove from every collection that might have it so that it gets GC'd
            Media.Remove(media);
            Media.CommitEdit();
            dirtyMedia.Remove(media);
        }

        /// <summary>
        /// Add a media file to the library.
        /// </summary>
        /// <param name="file">The file path</param>
        private async Task AddMediaInternal(string file)
        {
            // Create and initialise
            var media = new Media(file);
            media.Initialise();

            // Add listeners
            media.MediaRequestUpdate    += OnMediaRequestUpdate;
            media.MediaDirty            += OnMediaDirty;
            media.MediaRequestDelete    += OnMediaRequestDelete;
            await media.BuildOrCacheResources();

            // Add to UI
            Media.AddNewItem(media);
            Media.CommitNew();

            // Add to database
            var collection = Database.GetCollection<Media>();
            collection.Insert(media);
        }


        /// <summary>
        /// Adds a single media file to the library
        /// </summary>
        /// <param name="file">The file to add</param>
        /// <returns>Task</returns>
        public async Task AddMedia(string file)
        {
            if (Database.GetCollection<Media>().Exists(x => x.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
            {
                App.Notifications.NotifyWarning($"{file} is already imported");
                return;
            }

            try
            {
                await AddMediaInternal(file);
                App.Notifications.NotifyInfo($"Added {file}!");
            }
            catch (Exception e)
            {
                App.Notifications.NotifyException($"Failed to add {file}", e);
            }
        }

        /// <summary>
        /// Adds a group of media, e.g from a folder
        /// </summary>
        /// <param name="files">The files to add</param>
        /// <returns>Where the files came from</returns>
        public async Task AddMedias(string[] files, string source)
        {
            // This function should only be used on collections of media > 1
            if (files.Length == 1)
            {
                await AddMedia(files[0]);
                return;
            }

            var errors = new List<Exception>();
            foreach (var file in files)
            {
                try
                {
                    if (Database.GetCollection<Media>().Exists(x => x.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                        throw new DuplicateNameException($"{file} already exists");

                    await AddMediaInternal(file);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            if (errors.Count == 0)
            {
                App.Notifications.NotifyInfo($"Added {files.Length} media files from {source}");
            }
            else
                App.Notifications.NotifyException($"Failed to import {errors.Count}/{files.Length} media files from {source}", errors.ToArray());
        }

        /// <summary>
        /// Saves the media from the dirty media list.  This function should be called before 
        /// </summary>
        public void SaveDirtyMedia()
        {
            foreach (var media in dirtyMedia)
                Database.GetCollection<Media>().Update(media);
        }

        /// <summary>
        /// Checks whether or not a specific media matches the currently configured filters.
        /// </summary>
        /// <param name="media">The media to check</param>
        /// <returns>Whether or not filters are matched</returns>
        private bool MediaFilter(object obj)
        {
            if (obj is not Media media)
                return false;

            if (FilterTags.Count > 0)
            {
                if (FilterTagsMode == MediaFilterMode.All && !FilterTags.All(x => media.Tags.Any(y => y.Equals(x))))
                    return false;

                if (FilterTagsMode == MediaFilterMode.Any && !FilterTags.Any(x => media.Tags.Any(y => y.Equals(x))))
                    return false;

                if (FilterTagsMode == MediaFilterMode.None && FilterTags.Any(x => media.Tags.Any(y => y.Equals(x))))
                    return false;
            }

            if (FilterClasses.Count > 0)
            {
                if (FilterClassesMode == MediaFilterMode.Any && !FilterClasses.Any(x => x.Class == media.Class))
                    return false;

                if (FilterClassesMode == MediaFilterMode.None && FilterClasses.Any(x => x.Class == media.Class))
                    return false;
            }

            // Filter via text after filters have been applied
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (media.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (media.Description != null && media.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Test
        /// </summary>
        public void ApplyFilters()
        {
            ActiveSearchText = SearchText;
            ActiveFilterCount = FilterTags.Count + FilterClasses.Count + FilterFormats.Count;

            Media.Refresh();
            OnPropertyChanged(nameof(SearchHelp));
        }
        #endregion

        #region Media commands
        public ICommand OpenInEditorCommand => new RelayCommand(() =>
        {
            if (SelectedMedia == null)
                return;

            App.ViewModel.MediaEditor.Media = SelectedMedia;
            App.ViewModel.IsEditorSelected  = true;
        });

        public ICommand OpenInExplorerCommand => new RelayCommand(() =>
        {
            if (SelectedMedia == null || SelectedMedia.Uri == null)
                return;

            Process.Start(new ProcessStartInfo("explorer.exe")
                          {
                              UseShellExecute = true,
                              Arguments       = $"/select,\"{SelectedMedia.Uri.AbsoluteUri}\""
                          });
        });
        
        public static ICommand OpenDeleteDialogCommand { get; } = new RelayCommand<IList>(list =>
        {
            if (list == null)
                return;

            DialogHost.Show(new View.LibraryDeleteMedia()
            {
                DataContext = new LibraryDeleteTask(list.OfType<Media>())
            });
        });

        public static ICommand OpenEditTagsCommand { get; } = new RelayCommand<IList>(list =>
        {
            if (list == null)
                return;

            DialogHost.Show(new View.LibraryEditTags()
            {
                DataContext = new LibraryEditTagsTask(list.OfType<Media>())
            });
        });

        public static ICommand EditClassesCommand { get; } = new RelayCommand<EditClassesTask>(task =>
        {
            if (task == null)
                return;

            task.SelectedMedia.ForEach(x =>
            {
                x.ClassIndex = MediaClass.MediaClasses.IndexOf(task.Class);
            });
        });
        #endregion

        #region Filter tag commands
        public ICommand AddFilterTagCommand => new RelayCommand(() =>
        {
            var name = App.ViewModel.TagSuggestionRegistry.ActiveTagNames.LastOrDefault();
            if (name == null)
                return;

            var value = App.ViewModel.TagSuggestionRegistry.Tags.GetValueOrDefault(name)?.ActiveValues.LastOrDefault();
            if (value == null)
                return;

            FilterTags.Add(new Tag(name, value, true));
        });

        public ICommand RemoveFilterTagCommand => new RelayCommand<Tag>((tag) =>
        {
            if (tag != null)
                FilterTags.Remove(tag);
        });

        public ICommand ClearFilterTagsCommand => new RelayCommand(FilterTags.Clear);
        #endregion

        #region Filter class commands
        public ICommand AddFilterClassCommand => new RelayCommand(() =>
        {
            foreach (var @class in MediaClass.MediaClasses)
            {
                if (!FilterClasses.Any(x => x.Class == @class))
                {
                    FilterClasses.Add(new MediaClassFilter(@class));
                    break;
                }
            }
        });

        public ICommand RemoveFilterClassCommand => new RelayCommand<MediaClassFilter>((@class) =>
        {
            if (@class != null)
                FilterClasses.Remove(@class);
        });

        public ICommand ClearFilterClassesCommand => new RelayCommand(FilterClasses.Clear);
        #endregion

        #region Filter type commands
        public ICommand AddFilterFormatCommand => new RelayCommand(() =>
        {
            // foreach (var format in MediaFormat.SupportedFormats)
            // {
            //     if (!FilterFormats.Contains(format))
            //     {
            //         FilterFormats.Add(format);
            //         break;
            //     }
            // }
        });

        public ICommand RemoveFilterFormatCommand => new RelayCommand<ContainerFormat>((output) =>
        {
            if (output != null)
                FilterFormats.Remove(output);
        });

        public ICommand ClearFilterFormatsCommand => new RelayCommand(FilterFormats.Clear);
        #endregion

        #region Filter commands
        public ICommand ApplyFiltersCommand => new RelayCommand(ApplyFilters);
        public ICommand ResetFiltersCommand => new RelayCommand(() =>
        {
            FilterTags.Clear();
            FilterClasses.Clear();
            FilterFormats.Clear();
        });
        #endregion
    }
}