using System;
using System.Windows.Forms;

namespace mwm
{
    static class Program
    {
        private static NotifyIcon trayIcon;
        private static ContextMenuStrip trayMenu;

        [STAThread]
        static void Main()
        {
            ConfigManager.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize the tray icon and menu
            InitializeTrayIcon();

            // Create and display the top bars on all screens
            Form1.CreateTopBars();

            // Run the application without showing any form
            Application.Run();
        }

        // Initialize the tray icon and context menu
        private static void InitializeTrayIcon()
        {
            // Create a context menu for the tray icon
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Select Folder", null, OnSelectFolder);
            trayMenu.Items.Add("Exit", null, OnExit);

            // Create the tray icon
            trayIcon = new NotifyIcon
            {
                Text = "Top Bar",
                Icon = SystemIcons.Application, // You can customize this with your own icon
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        // Event handler for selecting a folder
        private static void OnSelectFolder(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    Form1.UpdateTopBars(folderDialog.SelectedPath);
                }
            }
        }

        // Event handler for the "Exit" menu item
        private static void OnExit(object sender, EventArgs e)
        {
            trayIcon.Dispose(); // Dispose of the tray icon
            Application.Exit(); // Close the application
        }
    }
}
