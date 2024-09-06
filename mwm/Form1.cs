using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace mwm
{
    public partial class Form1 : Form
    {
        // Define a single variable for the height of the top bar
        private readonly int TopBarHeight = 30;

        // Constants for AppBar messages
        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_QUERYPOS = 0x00000002;
        private const int ABM_SETPOS = 0x00000003;
        private const int ABE_TOP = 1;

        // Structure to hold information about the app bar
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

        // Import necessary functions from shell32.dll and user32.dll
        [DllImport("shell32.dll", SetLastError = true)]
        static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private Screen associatedScreen;

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
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            UnregisterAppBar();
        }

        // Method to register the window as an app bar
        private void RegisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = this.Handle,
                uEdge = ABE_TOP
            };

            // Set the position and size of the app bar according to the associated screen
            abd.rc.left = associatedScreen.Bounds.Left;
            abd.rc.top = associatedScreen.Bounds.Top;
            abd.rc.right = associatedScreen.Bounds.Right;
            abd.rc.bottom = associatedScreen.Bounds.Top + TopBarHeight;

            // Register the app bar
            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            // Override the form's position to reclaim the full height on the respective screen
            this.Top = associatedScreen.Bounds.Top;
            this.Left = associatedScreen.Bounds.Left;
            this.Width = associatedScreen.Bounds.Width;
            this.Height = TopBarHeight;
            SetWindowPos(this.Handle, IntPtr.Zero, this.Left, this.Top, this.Width, this.Height, 0);
        }

        // Method to unregister the app bar
        private void UnregisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = this.Handle
            };
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        // Static method to create and show the top bars on all screens
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
