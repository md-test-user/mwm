using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace mwm
{
    public partial class Form1 : Form
    {
        private readonly int TopBarHeight = 30;
        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_QUERYPOS = 0x00000002;
        private const int ABM_SETPOS = 0x00000003;
        private const int ABE_TOP = 1;

        private FileSystemWatcher watcher;  // FileSystemWatcher to monitor changes

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private Screen associatedScreen;
        private static string selectedPath = string.Empty;

        public Form1(Screen screen)
        {
            associatedScreen = screen;
            InitializeComponent();
            this.Load += Form1_Loaded;
            this.FormClosing += Form1_Closing;
        }

        private void Form1_Loaded(object sender, EventArgs e)
        {
            RegisterAppBar();
            DisplayFolderContent();
            StartWatchingFolder();  // Start watching folder for changes
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            UnregisterAppBar();
            StopWatchingFolder();  // Stop watching folder when closing
        }

        private void RegisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = this.Handle,
                uEdge = ABE_TOP
            };

            abd.rc.left = associatedScreen.Bounds.Left;
            abd.rc.top = associatedScreen.Bounds.Top;
            abd.rc.right = associatedScreen.Bounds.Right;
            abd.rc.bottom = associatedScreen.Bounds.Top + TopBarHeight;

            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            this.Top = associatedScreen.Bounds.Top;
            this.Left = associatedScreen.Bounds.Left;
            this.Width = associatedScreen.Bounds.Width;
            this.Height = TopBarHeight;
            SetWindowPos(this.Handle, IntPtr.Zero, this.Left, this.Top, this.Width, this.Height, 0);
        }

        private void UnregisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = this.Handle
            };
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        public static void UpdateTopBars(string path)
        {
            selectedPath = path;
            foreach (Form barForm in Application.OpenForms)
            {
                if (barForm is Form1)
                {
                    ((Form1)barForm).DisplayFolderContent();
                    ((Form1)barForm).StartWatchingFolder();  // Restart watching the new folder
                }
            }
        }

        // Method to display the contents of the folder
        private readonly string[] blacklistedExtensions = { ".lnk", ".exe", ".url", ".bat", ".cmd" };

        private void DisplayFolderContent()
        {
            this.Controls.Clear();
            if (Directory.Exists(selectedPath))
            {
                var items = Directory.GetFileSystemEntries(selectedPath);
                int xOffset = 10;

                foreach (var item in items)
                {
                    string fileName = Path.GetFileName(item);
                    string extension = Path.GetExtension(fileName).ToLower();

                    // If the extension is in the blacklistedExtensions array, remove it from the displayed text
                    if (Array.Exists(blacklistedExtensions, ext => ext == extension))
                    {
                        fileName = Path.GetFileNameWithoutExtension(item); // Remove the extension
                    }

                    var button = new Button
                    {
                        Text = fileName,
                        Tag = item,
                        Left = xOffset,
                        Top = 5,
                        Width = 150,
                        Height = TopBarHeight - 10
                    };
                    button.Click += Item_Click;
                    this.Controls.Add(button);

                    xOffset += button.Width + 5;
                }
            }
        }


        // Method to start watching the selected folder for changes
        private void StartWatchingFolder()
        {
            if (watcher != null)
            {
                watcher.Dispose();  // Dispose of any previous watcher
            }

            if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
            {
                watcher = new FileSystemWatcher
                {
                    Path = selectedPath,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                };

                // Hook up event handlers for changes in the directory
                watcher.Created += OnFolderChanged;
                watcher.Deleted += OnFolderChanged;
                watcher.Renamed += OnFolderChanged;

                watcher.EnableRaisingEvents = true;  // Start watching
            }
        }

        // Method to stop watching the folder for changes
        private void StopWatchingFolder()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }

        // Event handler for when the folder content changes (files added, removed, or renamed)
        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            // Invoke the DisplayFolderContent method on the UI thread to update the display
            this.Invoke(new Action(() =>
            {
                DisplayFolderContent();
            }));
        }

        private void Item_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            var path = button.Tag.ToString();

            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path); // Open folder
            }
            else if (File.Exists(path))
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true // Use the default app associated with the file type
                };
                process.Start();
            }
        }

        public static void CreateTopBars()
        {
            foreach (var screen in Screen.AllScreens)
            {
                Form barForm = new Form1(screen)
                {
                    FormBorderStyle = FormBorderStyle.None,
                    TopMost = true,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(screen.Bounds.Left, screen.Bounds.Top),
                    Width = screen.Bounds.Width
                };

                barForm.Show();
            }
        }
    }
}
