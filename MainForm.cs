using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasswordProtectedApp
{
    public partial class MainForm : Form
    {
        private const string CorrectPassword = "123";
        private const string WelcomeMessage = ".!.";

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelProc _keyboardProc;
        private LowLevelProc _mouseProc;
        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private const string TaskMgrKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\System";
        private const string TaskMgrValueName = "DisableTaskMgr";
        private const string CtrlAltDelKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
        private const string CtrlAltDelValueName = "DisableTaskMgr";

        private Label lblWelcome;
        private Label lblPassword;
        public TextBox txtPassword;
        public Button btnSubmit;

        private bool _isAltKeyPressed = false;

       public MainForm()
{
    InitializeComponent();
    this.FormBorderStyle = FormBorderStyle.None;
    this.StartPosition = FormStartPosition.Manual;
    this.Size = new Size(250, 200);
    this.BackColor = Color.Black;

    // Position the form at the bottom-right corner
    Rectangle workingArea = Screen.GetWorkingArea(this);
    this.Location = new Point(workingArea.Right - this.Width, workingArea.Bottom - this.Height);

    InitializeCustomControls();
}

        private void InitializeCustomControls()
{
    lblWelcome = new Label
    {
        Text = WelcomeMessage,
        Font = new Font("Arial", 14, FontStyle.Bold),
        ForeColor = Color.White,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 10, 0, 10)
    };

    lblPassword = new Label
    {
        Text = ".!.",
        Font = new Font("Arial", 10),
        ForeColor = Color.White,
        Dock = DockStyle.Top,
        Padding = new Padding(5, 5, 5, 0)
    };

    txtPassword = new TextBox
    {
        PasswordChar = '*',
        Font = new Font("Arial", 10),
        Dock = DockStyle.Top,
        BackColor = Color.Black,
        ForeColor = Color.White,
        Margin = new Padding(5, 0, 5, 5)
    };

    btnSubmit = new Button
    {
        Text = "?!",
        Font = new Font("Arial", 10),
        BackColor = Color.DarkGray,
        ForeColor = Color.White,
        Dock = DockStyle.None, // Changed from Top
        Size = new Size(80, 30)
    };

    // Create a panel to hold the submit button
    Panel buttonPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = Color.Transparent
    };

    // Add controls in the desired order
    this.Controls.Add(lblWelcome);
    this.Controls.Add(lblPassword);
    this.Controls.Add(txtPassword);
    this.Controls.Add(buttonPanel);

    // Center the submit button in the panel
    buttonPanel.Controls.Add(btnSubmit);
    btnSubmit.Location = new Point((buttonPanel.Width - btnSubmit.Width) / 2, 
                                   (buttonPanel.Height - btnSubmit.Height) / 2);
    buttonPanel.Resize += (sender, e) => 
    {
        btnSubmit.Location = new Point((buttonPanel.Width - btnSubmit.Width) / 2, 
                                       (buttonPanel.Height - btnSubmit.Height) / 2);
    };

    btnSubmit.Click += btnSubmit_Click;
}
        private void MainForm_Load(object sender, EventArgs e)
        {
            DisableTaskManagerAndCtrlAltDel();

            SetFocus(txtPassword.Handle);
            txtPassword.KeyPress += TxtPassword_KeyPress;

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            RECT rect = new RECT
            {
                Left = this.Left,
                Top = this.Top,
                Right = this.Right,
                Bottom = this.Bottom
            };
            ClipCursor(ref rect);

            HookKeyboardAndMouse();
        }

        private void DisableTaskManagerAndCtrlAltDel()
        {
            try
            {
                Registry.SetValue(TaskMgrKey, TaskMgrValueName, 1, RegistryValueKind.DWord);
                Registry.SetValue(CtrlAltDelKey, CtrlAltDelValueName, 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to modify registry: {ex.Message}");
            }
        }

        private void EnableTaskManagerAndCtrlAltDel()
        {
            try
            {
                Registry.SetValue(TaskMgrKey, TaskMgrValueName, 0, RegistryValueKind.DWord);
                Registry.SetValue(CtrlAltDelKey, CtrlAltDelValueName, 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restore registry: {ex.Message}");
            }
        }

        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            SetFocus(txtPassword.Handle);
            if (e.KeyChar == (char)Keys.Enter)
            {
                CheckPassword();
                e.Handled = true;
            }
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            CheckPassword();
        }

        private void CheckPassword()
        {
            if (txtPassword.Text == CorrectPassword)
            {
                EnableInputAndClose();
            }
            else
            {
                MessageBox.Show(".!.");
                SetFocus(txtPassword.Handle);
                txtPassword.SelectAll();
            }
        }

        private void EnableInputAndClose()
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);

            RECT rect = new RECT();
            ClipCursor(ref rect);

            EnableTaskManagerAndCtrlAltDel();

            MessageBox.Show("Access Granted!");

            Application.Exit();
        }

        private void HookKeyboardAndMouse()
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHookID = SetHook(13, _keyboardProc);
            _mouseHookID = SetHook(14, _mouseProc);
        }

        private IntPtr SetHook(int hookType, LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0)
    {
        int vkCode = Marshal.ReadInt32(lParam);
        Keys key = (Keys)vkCode;

        // Check if the key is being pressed or released
        bool keyDown = ((int)wParam == 0x100 || (int)wParam == 0x104);

        // Track Alt key state
        if (key == Keys.LMenu || key == Keys.RMenu)
        {
            _isAltKeyPressed = keyDown;
        }

        // Block Alt+Tab, Alt+F4, Ctrl+Esc, Windows key, Alt+Esc, Ctrl+Alt+Esc, and Ctrl+Alt+Delete
        if ((_isAltKeyPressed && (key == Keys.Tab || key == Keys.F4 || key == Keys.Escape)) ||
            (Control.ModifierKeys == Keys.Control && key == Keys.Escape) ||
            key == Keys.LWin || key == Keys.RWin ||
            (Control.ModifierKeys == (Keys.Control | Keys.Alt) && (key == Keys.Delete || key == Keys.Escape)))
        {
            return (IntPtr)1;
        }
    }
    return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
}

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Block all mouse events except for the submit button
                if (!IsClickOnSubmitButton(lParam))
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private bool IsClickOnSubmitButton(IntPtr lParam)
        {
            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            Point clickPoint = new Point(hookStruct.pt.x, hookStruct.pt.y);
            return btnSubmit.ClientRectangle.Contains(btnSubmit.PointToClient(clickPoint));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (txtPassword.Text != CorrectPassword)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
            RECT rect = new RECT();
            ClipCursor(ref rect);
            EnableTaskManagerAndCtrlAltDel();
        }
    }
}