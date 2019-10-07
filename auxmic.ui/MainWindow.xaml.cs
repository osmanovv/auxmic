using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows.Data;
using System.ComponentModel;
using System;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SyncParams _syncParams = new SyncParams
        {
            L = 256,
            FreqRangeStep = 60,
            WindowFunction = WindowFunctions.Hamming
        };

        public ClipSynchronizer _clipSynchronizer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MasterPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                SetMaster(files);
            }
        }

        private void SetMaster(string[] files)
        {
            if (files.Length == 0) return;

            try
            {
                _clipSynchronizer.SetMaster(files[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        private void LQItems_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                AddLQ(files);
            }
        }

        private void AddLQ(string[] files)
        {
            if (files.Length == 0) return;

            foreach (var file in files)
            {
                try
                {
                    _clipSynchronizer.AddLQ(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _clipSynchronizer = new ClipSynchronizer(_syncParams);

            // Очищаем списки, т.к. для отображения разметки в конструкторе форм добавлена строка
            HQItems.Items.Clear();
            LQItems.Items.Clear();

            HQItems.ItemsSource = _clipSynchronizer.MasterClips;
            LQItems.ItemsSource = _clipSynchronizer.LQClips;

            //CollectionViewSource viewSource = new CollectionViewSource();
            //viewSource.Source = _clipSynchronizer.LQClips;
            ////SortDescription sorting = new SortDescription("Offset", ListSortDirection.Ascending);
            ////sorting.IsLiveSortingRequested = true; // ! .NET Framework 4.5 feature!
            //viewSource.SortDescriptions.Add(new SortDescription("Offset", ListSortDirection.Ascending));
            //LQItems.ItemsSource = viewSource.View;
        }

        private void cmdRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            Button removeButton = (Button)sender;

            Clip clipToCancel = (Clip)removeButton.Tag;

            _clipSynchronizer.Cancel(clipToCancel);
        }

        private void cmdSaveMatch_Click(object sender, RoutedEventArgs e)
        {
            Button saveMatchButton = (Button)sender;

            Clip clip = (Clip)saveMatchButton.Tag;

            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "WAV file|*.wav",
                DefaultExt = ".wav",
                FileName = Path.GetFileNameWithoutExtension(clip.DisplayName)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _clipSynchronizer.Save(clip, saveFileDialog.FileName);
            }
        }

        private void cmd_AddMaster(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            SetMaster(PickFiles());
        }

        private void cmd_AddLQ(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            AddLQ(PickFiles());
        }

        private string[] PickFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            /* Supported Media Formats in Media Foundation
             * http://msdn.microsoft.com/ru-ru/library/windows/desktop/dd757927(v=vs.85).aspx */
            openFileDialog.Filter = "All media files|*.wav;*.mp3;*.3g2;*.3gp;*.3gp2;*.3gpp;*.aac;*.adts;*.avi;*.asf;*.wma;*.wmv;*.m4a;*.m4v;*.mov;*.mp4;*.m2ts;*.mts|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = true;

            string[] result = {};

            if (openFileDialog.ShowDialog() == true)
            {
                result = openFileDialog.FileNames;
            }

            return result;
        }

        private void cmd_About(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            About about = new About();
            about.Owner = this;
            about.ShowDialog();
        }

        private void cmd_OpenCacheFolder(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(_clipSynchronizer.GetTempPath());
        }

        private void cmd_ClearCache(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("All files will be closed. Continue anyway?", "Clear cache", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.OK)
            {
                _clipSynchronizer.ClearCache();
            }
        }
    }
}
