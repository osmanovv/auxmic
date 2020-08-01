using Microsoft.Win32;
using System.Windows;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Close window without saving the settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Cancel(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            this.Close();
        }

        /// <summary>
        /// Close window and save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Save(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
        }

        /// <summary>
        /// Prompts to choose FFmpeg executable file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_OpenFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "FFmpeg executable|ffmpeg.exe|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                // change settings but not save yet
                Properties.Settings.Default.FFMPEG_EXE_PATH = openFileDialog.FileName;
            }
        }
    }
}
