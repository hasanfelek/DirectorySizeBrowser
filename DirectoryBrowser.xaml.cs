using System.Linq;

namespace DirectorySizes
{

    /// <summary>
    /// Interaction logic for DirectoryBrowser.xaml
    /// </summary>
    public partial class DirectoryBrowser : System.Windows.Window
    {

        #region Fields

        private System.DateTime _lastCountersUpdate;
        private delegate void UpdateGatherCountersDelegate(bool forceUpdate);
        //private delegate void AddRootNodeDelegate(DirectoryDetail directoryDetail);
        //private delegate void GatherInProgressChangedDelegate(bool newState);
        //private delegate void AddSkippedDirectoryDelegate(string skippedDirectory);

        #endregion Fields

        #region Constructor
        public DirectoryBrowser()
        {
            InitializeComponent();

            // Wire the events
            DirectoryHelper.StatusUpdate += DirectoryHelper_StatusUpdate;
            DirectoryHelper.CountersChanged += DirectoryHelper_CountersChanged;
            DirectoryHelper.GatherInProgressChanges += DirectoryHelper_GatherInProgressChanges;
            DirectoryHelper.DirectoryComplete += DirectoryHelper_DirectoryComplete;
            DirectoryHelper.DirectorySkipped += DirectoryHelper_DirectorySkipped;

            // Populate the drive combo and select the first drive
            this.Drives.ItemsSource = DirectoryHelper.GetDrives();
            this.Drives.SelectedIndex = 0;
        }
        #endregion Constructor

        #region Event handlers
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

        private void DirectoryHelper_StatusUpdate(object sender, string e)
        {
            this.Status.Dispatcher.BeginInvoke((System.Action)(() => { UpdateStatus(e); }));
        }

        void DirectoryHelper_CountersChanged(object sender, System.EventArgs e)
        {
            this.CountInfo.Dispatcher.BeginInvoke(new UpdateGatherCountersDelegate(UpdateGatherCounters), false);
        }

        void DirectoryHelper_DirectorySkipped(object sender, string e)
        {
            this.SkippedDirectories.Dispatcher.BeginInvoke((System.Action)(() => { AddSkippedDirectory(e); }));
        }

        void DirectoryHelper_DirectoryComplete(object sender, DirectoryDetail e)
        {
            this.DirectoryTree.Dispatcher.BeginInvoke((System.Action)(() => { AddRootNode(e); }));
        }


        private void DirectoryHelper_GatherInProgressChanges(object sender, bool newState)
        {
            this.ChangeState.Dispatcher.BeginInvoke((System.Action)(() => { GatherInProgressChanged(newState); }));
        }

        void tvi_Expanded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            this.ExpandNode((System.Windows.Controls.TreeViewItem)sender);
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

        private void DirectoryTree_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            this.ListDirectoryFiles(e.NewValue as System.Windows.Controls.TreeViewItem);
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            this.StartStopAction();
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
        private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Controls.ListView listView = sender as System.Windows.Controls.ListView;

            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (listView.SelectedItem != null)
            {
                this.OpenFolder(((FileDetail)listView.SelectedItem).Path);
            }
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

        private void FileTypeList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            this.Top100ByExtensionList.ItemsSource = null;
            if (this.FileTypeList.SelectedItem != null)
            {
                this.Top100ByExtensionList.ItemsSource = DirectoryHelper.BiggestFilesByExtension(((ExtensionInfo)this.FileTypeList.SelectedItem).Extension, 100);
            }
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
        #endregion Event handlers

        #region Methods

        /// <summary>
        /// Updates the counters
        /// </summary>
        void UpdateGatherCounters(bool forceUpdate)
        {
            if (forceUpdate || System.DateTime.Now.Subtract(this._lastCountersUpdate).TotalMilliseconds > 500)
            {
                this.CountInfo.Content = string.Format("{0} directories, {1} files", DirectoryHelper.OverallDirectoryCount.ToString("N0"), DirectoryHelper.OverallFileCount.ToString("N0"));
                this._lastCountersUpdate = System.DateTime.Now;
            }
        }

        /// <summary>
        /// Adds a directory to the list of skipped directories
        /// </summary>
        /// <param name="skippedDirectory"></param>
        private void AddSkippedDirectory(string skippedDirectory)
        {
            this.SkippedDirectories.Items.Add(skippedDirectory);
        }

        /// <summary>
        /// Changes the button text based on the state of the data gather
        /// </summary>
        /// <param name="newState"></param>
        private void GatherInProgressChanged(bool newState)
        {
            if (newState == true)
            {
                StatisticsTab.IsEnabled = false;
                this.ChangeState.Content = "Stop analysis";
            }
            else
            {
                this.ChangeState.Content = "Start analysis";
                this.UpdateGatherCounters(true);
                this.ShowStatistics();
                StatisticsTab.IsEnabled = true;
            }
        }

        /// <summary>
        /// Used to add root folders
        /// </summary>
        /// <param name="directoryDetail">Directory to add</param>
        private void AddRootNode(DirectoryDetail directoryDetail)
        {
            AddDirectoryNode(this.DirectoryTree.Items, directoryDetail);
        }

        /// <summary>
        /// When the node is expanded, adds sub-directory nodes
        /// </summary>
        /// <param name="tvi"></param>
        private void ExpandNode(System.Windows.Controls.TreeViewItem tvi)
        {
            if (tvi.Items.Count == 1 && ((System.Windows.Controls.TreeViewItem)tvi.Items[0]).Name == "placeholder")
            {
                tvi.Items.Clear();
                foreach (DirectoryDetail directoryDetail in ((DirectoryDetail)tvi.Tag).SubDirectoryDetails.OrderBy(x => x.Path))
                {
                    this.AddDirectoryNode(tvi.Items, directoryDetail);
                }
            }
        }

        /// <summary>
        /// Adds a directory node to the specified items collection
        /// </summary>
        /// <param name="parentItemCollection">Items collection of the parent directory</param>
        /// <param name="directoryDetail">Directory to add</param>
        /// <returns>True if succesful</returns>
        private bool AddDirectoryNode(System.Windows.Controls.ItemCollection parentItemCollection, DirectoryDetail directoryDetail)
        {
            System.Windows.Controls.TreeViewItem treeViewItem;
            System.Windows.Controls.StackPanel stackPanel;

            // Create the stackpanel and it's content
            stackPanel = new System.Windows.Controls.StackPanel();
            stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            // Content
            stackPanel.Children.Add(this.CreateProgressBar("Cumulative percentage from total used space {0}% ({1}))", directoryDetail.CumulativeSizePercentage, directoryDetail.FormattedCumulativeBytes));
            stackPanel.Children.Add(new System.Windows.Controls.TextBlock() { Text = directoryDetail.DirectoryName });

            // Create the treeview item
            treeViewItem = new System.Windows.Controls.TreeViewItem();
            treeViewItem.Tag = directoryDetail;
            treeViewItem.Header = stackPanel;
            treeViewItem.Expanded += tvi_Expanded;

            // If this directory contains subdirectories, add a placeholder
            if (directoryDetail.SubDirectoryDetails.Count() > 0)
            {
                treeViewItem.Items.Add(new System.Windows.Controls.TreeViewItem() { Name = "placeholder" });
            }

            // Add the treeview item into the items collection
            parentItemCollection.Add(treeViewItem);

            return true;
        }

        /// <summary>
        /// Creates and formats a directory node progress bar
        /// </summary>
        /// <param name="tooltipText">Tooltip to show</param>
        /// <param name="percentage">Percentage to show in tooltip</param>
        /// <param name="formattedNumberValue">Value to show in the tooltip</param>
        /// <returns></returns>
        private System.Windows.Controls.ProgressBar CreateProgressBar(string tooltipText, double percentage, string formattedNumberValue)
        {
            System.Windows.Controls.ProgressBar progressBar;
            System.Windows.Controls.TextBlock toolTipText;

            progressBar = new System.Windows.Controls.ProgressBar();
            progressBar.Value = percentage;
            toolTipText = new System.Windows.Controls.TextBlock()
            {
                Text = string.Format(tooltipText, percentage.ToString("F2"), formattedNumberValue)
            };
            progressBar.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipText };
            progressBar.Width = 90;
            progressBar.Margin = new System.Windows.Thickness(3, 3, 3, 3);

            return progressBar;
        }

        /// <summary>
        /// Updates the status bar
        /// </summary>
        /// <param name="state"></param>
        private void UpdateStatus(string state)
        {
            this.Status.Content = state;
        }

        /// <summary>
        /// Populates the file list for a directory in descending order based on the file sizes
        /// </summary>
        /// <param name="tvi">Directory to populate files for</param>
        private void ListDirectoryFiles(System.Windows.Controls.TreeViewItem tvi)
        {
            DirectoryDetail directoryDetail;

            this.FileList.ItemsSource = null;
            this.Top100FileList.ItemsSource = null;
            if (tvi != null)
            {
                directoryDetail = (DirectoryDetail)tvi.Tag;
                this.FileList.ItemsSource = DirectoryHelper.FilesInDirectory(directoryDetail);
                this.Top100FileList.ItemsSource = DirectoryHelper.BiggestFilesInPath(directoryDetail, 100);
            }
        }

        /// <summary>
        /// Starts or stops the data gathering
        /// </summary>
        private void StartStopAction()
        {
            if (DirectoryHelper.GatherInProgress)
            {
                DirectoryHelper.StopDataGathering();
            }
            else
            {
                this.DirectoryTree.Items.Clear();
                DirectoryHelper.StartDataGathering((System.IO.DriveInfo)this.Drives.SelectedItem);
            }
        }

        /// <summary>
        /// Opens the specified folder to Windows Explorer
        /// </summary>
        private void OpenFolder(string path)
        {
            System.Diagnostics.Process.Start(path);
        }

        /// <summary>
        /// Files the file type list
        /// </summary>
        private void ShowStatistics()
        {
            this.FileTypeList.ItemsSource = DirectoryHelper.ExtensionsBySize();
        }
        #endregion Methods


    }
}
