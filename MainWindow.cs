using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace CannonEmuFrontend
{
    public partial class MainWindow : Form
    {
        private SerialPort? _serialPort;
        private bool _isRunning = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private string _romsPath;

        // iiSU Color Scheme
        private static readonly Color PrimaryColor = Color.FromArgb(31, 31, 31);      // Dark background
        private static readonly Color SecondaryColor = Color.FromArgb(50, 50, 50);     // Lighter panels
        private static readonly Color AccentColor = Color.FromArgb(0, 173, 239);       // Bright blue
        private static readonly Color TextColor = Color.FromArgb(240, 240, 240);       // Light text
        private static readonly Color DimTextColor = Color.FromArgb(150, 150, 150);    // Dim text

        // Comprehensive list of console ROM extensions
        private static readonly string[] ValidROMExtensions = new[]
        {
            // Nintendo
            ".nes", ".sfc", ".n64", ".z64", ".gb", ".gbc", ".gba", ".nds",
            ".3ds", ".cci", ".cxi", ".nro", ".nso", ".xci", ".nsp",

            // Sega
            ".md", ".gen", ".sms", ".gg", ".seg", ".bin", ".rom",

            // Sony
            ".ps", ".cue", ".bin", ".img", ".psx", ".ps2", ".iso",

            // Atari
            ".a26", ".bin", ".rom", ".st", ".stx", ".msa", ".dim",

            // Commodore
            ".d64", ".d81", ".t64", ".prg",

            // Arcade & MAME
            ".zip", ".7z", ".rar",

            // Handhelds
            ".gb", ".gbc", ".gba", ".vb",

            // Multi-system
            ".bin", ".rom", ".iso", ".img", ".cue", ".cso", ".pbp",

            // Dreamcast
            ".cdi", ".gdi",

            // Neo Geo
            ".zip", ".rar",

            // Turbografx-16 / PC Engine
            ".pce", ".sgx", ".ccd", ".cue",

            // Genesis/Megadrive variants
            ".asm", ".asx",

            // Master System variants
            ".sms", ".sg",

            // Game Gear variants
            ".gg",

            // Intellivision
            ".int", ".bin",

            // Colecovision
            ".col", ".bin", ".rom",

            // VIC-20
            ".prg", ".tap", ".bin",

            // Amiga
            ".adf", ".adz", ".fdi",

            // Apple II
            ".dsk", ".do", ".po",

            // Magnavox Odyssey 2
            ".bin", ".rom",

            // Fairchild Channel F
            ".bin", ".rom",

            // RCA Studio II
            ".bin", ".rom",

            // Vectrex
            ".bin", ".vec", ".rom",

            // Virtual Boy
            ".vb", ".bin",

            // PC-FX
            ".cue", ".cso", ".pbp", ".iso",

            // WonderSwan
            ".ws", ".wsc", ".bin",

            // Tiger Game.com
            ".bin", ".rom",

            // Milton Bradley Microvision
            ".bin", ".rom",

            // Epoch Game Pocket Computer
            ".bin", ".rom",

            // Sharp X1
            ".bin", ".rom", ".dx1", ".2d", ".2hd",

            // FM Towns
            ".iso", ".cue", ".cso",

            // Acorn Archimedes
            ".adf", ".adl",

            // BBC Micro
            ".ssd", ".dsd", ".adl",

            // MSX
            ".rom", ".mx1", ".mx2", ".bin",

            // Sinclair ZX Spectrum
            ".tap", ".tzx", ".z80", ".sna", ".scl", ".trd",

            // Commodore Plus/4
            ".bin", ".rom", ".prg",

            // Atari 8-bit
            ".xex", ".atr", ".cas", ".bin",

            // Bandai WonderSwan Color
            ".wsc", ".bin",

            // Mattel Auto Race
            ".bin", ".rom",

            // Generic console formats
            ".bin", ".rom", ".img", ".iso", ".cue", ".cso"
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeRomsDirectory();
            SetupUI();
            LoadComPorts();
            LoadROMs();
        }

        private void InitializeRomsDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cannonEmuPath = Path.Combine(appDataPath, "CannonEmu");
            _romsPath = Path.Combine(cannonEmuPath, "ROMs");

            if (!Directory.Exists(cannonEmuPath))
            {
                Directory.CreateDirectory(cannonEmuPath);
            }

            if (!Directory.Exists(_romsPath))
            {
                Directory.CreateDirectory(_romsPath);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ResumeLayout(false);
        }

        private void SetupUI()
        {
            // Main form settings
            this.Text = "Cannon Emulator";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = PrimaryColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 9.75f);

            // Remove default borders
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.ControlBox = true;

            // Menu strip
            var menuStrip = new MenuStrip
            {
                BackColor = SecondaryColor,
                ForeColor = TextColor,
                AutoSize = false,
                Height = 25
            };
            var fileMenu = menuStrip.Items.Add("&File");
            ((ToolStripMenuItem)fileMenu).ForeColor = TextColor;
            ((ToolStripMenuItem)fileMenu).DropDownItems.Add("Open ROMs Folder", null, (s, e) => OpenRomsFolder());
            ((ToolStripMenuItem)fileMenu).DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            this.Controls.Add(menuStrip);

            // Main container panel
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PrimaryColor,
                Padding = new Padding(0)
            };

            // Top section - Connection and Status (Side by side)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = PrimaryColor,
                Padding = new Padding(10)
            };

            // Left side - Connection
            var connectionPanel = CreateConnectionPanel();
            connectionPanel.Location = new Point(10, 10);
            connectionPanel.Size = new Size(480, 100);
            topPanel.Controls.Add(connectionPanel);

            // Right side - Status/Info
            var statusPanel = CreateStatusPanel();
            statusPanel.Location = new Point(500, 10);
            statusPanel.Size = new Size(480, 100);
            topPanel.Controls.Add(statusPanel);

            mainContainer.Controls.Add(topPanel);

            // Middle section - ROM List (Full width)
            var romPanel = CreateROMPanel();
            romPanel.Dock = DockStyle.Top;
            romPanel.Height = 200;
            mainContainer.Controls.Add(romPanel);

            // Lower middle section - Display/Console Output
            var displayPanel = CreateDisplayPanel();
            displayPanel.Dock = DockStyle.Fill;
            mainContainer.Controls.Add(displayPanel);

            // Bottom section - Control Buttons
            var controlPanel = CreateControlPanel();
            controlPanel.Dock = DockStyle.Bottom;
            controlPanel.Height = 70;
            mainContainer.Controls.Add(controlPanel);

            this.Controls.Add(mainContainer);
        }

        private Panel CreateConnectionPanel()
        {
            var panel = new Panel
            {
                BackColor = SecondaryColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = "CONNECTION",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(10, 5)
            };
            panel.Controls.Add(titleLabel);

            // COM Port
            var portLabel = new Label
            {
                Text = "Port:",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(10, 25)
            };
            panel.Controls.Add(portLabel);

            var portCombo = new ComboBox
            {
                Name = "PortCombo",
                Width = 120,
                Location = new Point(50, 22),
                BackColor = PrimaryColor,
                ForeColor = TextColor,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panel.Controls.Add(portCombo);

            // Baud Rate
            var baudLabel = new Label
            {
                Text = "Baud:",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(180, 25)
            };
            panel.Controls.Add(baudLabel);

            var baudCombo = new ComboBox
            {
                Name = "BaudCombo",
                Width = 100,
                Location = new Point(225, 22),
                BackColor = PrimaryColor,
                ForeColor = TextColor,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            baudCombo.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            baudCombo.SelectedIndex = 0;
            panel.Controls.Add(baudCombo);

            // Connect button
            var connectBtn = CreateStyledButton("Connect", 10, 50, 100);
            connectBtn.Name = "ConnectButton";
            connectBtn.Click += ConnectButton_Click;
            panel.Controls.Add(connectBtn);

            // Refresh ports button
            var refreshBtn = CreateStyledButton("Refresh", 120, 50, 80);
            refreshBtn.Click += (s, e) => LoadComPorts();
            panel.Controls.Add(refreshBtn);

            return panel;
        }

        private Panel CreateStatusPanel()
        {
            var panel = new Panel
            {
                BackColor = SecondaryColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = "STATUS",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(10, 5)
            };
            panel.Controls.Add(titleLabel);

            var statusLabel = new Label
            {
                Name = "StatusLabel",
                Text = "Disconnected",
                ForeColor = DimTextColor,
                AutoSize = true,
                Location = new Point(10, 30)
            };
            panel.Controls.Add(statusLabel);

            var infoLabel = new Label
            {
                Name = "InfoLabel",
                Text = "Ready",
                ForeColor = DimTextColor,
                AutoSize = true,
                Location = new Point(10, 50)
            };
            panel.Controls.Add(infoLabel);

            return panel;
        }

        private Panel CreateROMPanel()
        {
            var panel = new Panel
            {
                BackColor = PrimaryColor,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "ROMS",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(10, 5)
            };
            panel.Controls.Add(titleLabel);

            // ROM List
            var romListBox = new ListBox
            {
                Name = "ROMListBox",
                Dock = DockStyle.Fill,
                Location = new Point(10, 25),
                BackColor = SecondaryColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.5f)
            };
            panel.Controls.Add(romListBox);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = PrimaryColor,
                Padding = new Padding(10, 5, 10, 5)
            };

            var loadBtn = CreateStyledButton("Load ROM", 5, 5, 100);
            loadBtn.Click += (s, e) => LoadSelectedROM();
            buttonPanel.Controls.Add(loadBtn);

            var refreshBtn = CreateStyledButton("Refresh", 110, 5, 80);
            refreshBtn.Click += (s, e) => LoadROMs();
            buttonPanel.Controls.Add(refreshBtn);

            var openBtn = CreateStyledButton("Open Folder", 195, 5, 100);
            openBtn.Click += (s, e) => OpenRomsFolder();
            buttonPanel.Controls.Add(openBtn);

            panel.Controls.Add(buttonPanel);

            return panel;
        }

        private Panel CreateDisplayPanel()
        {
            var panel = new Panel
            {
                BackColor = PrimaryColor,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "CONSOLE OUTPUT",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(10, 5)
            };
            panel.Controls.Add(titleLabel);

            var displayTextBox = new TextBox
            {
                Name = "DisplayTextBox",
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Location = new Point(10, 25),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(0, 220, 100),
                Font = new Font("Courier New", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            panel.Controls.Add(displayTextBox);

            return panel;
        }

        private Panel CreateControlPanel()
        {
            var panel = new Panel
            {
                BackColor = SecondaryColor,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10)
            };

            var buttons = new[]
            {
                ("Reset", 10, (Action)(() => SendCommand("RESET"))),
                ("Start", 130, (Action)(() => SendCommand("START"))),
                ("Stop", 250, (Action)(() => SendCommand("STOP"))),
                ("Fire", 370, (Action)(() => SendCommand("FIRE"))),
            };

            foreach (var (label, xPos, handler) in buttons)
            {
                var btn = new Button
                {
                    Text = label,
                    Width = 110,
                    Height = 45,
                    Location = new Point(xPos, 8),
                    BackColor = AccentColor,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => handler();
                panel.Controls.Add(btn);
            }

            return panel;
        }

        private Button CreateStyledButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                Location = new Point(x, y),
                BackColor = AccentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void LoadROMs()
        {
            try
            {
                var romListBox = FindControlByName<ListBox>("ROMListBox");
                if (romListBox == null) return;

                romListBox.Items.Clear();

                if (!Directory.Exists(_romsPath))
                {
                    Log("ROMs directory not found");
                    return;
                }

                var romFiles = Directory.GetFiles(_romsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsValidROMExtension(f))
                    .Select(f => Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToArray();

                if (romFiles.Length == 0)
                {
                    Log($"No ROMs found in {_romsPath}");
                }
                else
                {
                    foreach (var rom in romFiles)
                    {
                        romListBox.Items.Add(rom);
                    }
                    Log($"Loaded {romFiles.Length} ROM(s)");
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading ROMs: {ex.Message}");
            }
        }

        private bool IsValidROMExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return ValidROMExtensions.Contains(extension);
        }

        private void LoadSelectedROM()
        {
            var romListBox = FindControlByName<ListBox>("ROMListBox");
            if (romListBox?.SelectedItem == null)
            {
                Log("No ROM selected");
                return;
            }

            string selectedROM = romListBox.SelectedItem.ToString()!;
            string romPath = Path.Combine(_romsPath, selectedROM);

            try
            {
                if (!File.Exists(romPath))
                {
                    Log($"ROM file not found: {selectedROM}");
                    return;
                }

                SendCommand($"LOAD_ROM:{romPath}");
                Log($"Loaded ROM: {selectedROM}");
            }
            catch (Exception ex)
            {
                Log($"Error loading ROM: {ex.Message}");
            }
        }

        private void OpenRomsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _romsPath);
            }
            catch (Exception ex)
            {
                Log($"Error opening folder: {ex.Message}");
            }
        }

        private void LoadComPorts()
        {
            var portCombo = FindControlByName<ComboBox>("PortCombo");
            if (portCombo != null)
            {
                var ports = SerialPort.GetPortNames();
                portCombo.DataSource = ports.Length > 0 ? ports : new[] { "No ports available" };
            }
        }

        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private void Connect()
        {
            try
            {
                var portCombo = FindControlByName<ComboBox>("PortCombo");
                var baudCombo = FindControlByName<ComboBox>("BaudCombo");
                var connectBtn = FindControlByName<Button>("ConnectButton");

                if (portCombo?.SelectedItem == null || baudCombo?.SelectedItem == null)
                    return;

                string port = portCombo.SelectedItem.ToString()!;
                int baudRate = (int)baudCombo.SelectedItem;

                _serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 100,
                    WriteTimeout = 100
                };

                _serialPort.Open();
                _isRunning = true;

                if (connectBtn != null)
                    connectBtn.Text = "Disconnect";

                UpdateStatus($"Connected to {port}", $"Baud: {baudRate}");
                Log($"Connected to {port} at {baudRate} baud");

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReadSerialDataAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
                UpdateStatus("Connection Failed", ex.Message);
            }
        }

        private void Disconnect()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _serialPort?.Close();
                _serialPort?.Dispose();
                _isRunning = false;

                var connectBtn = FindControlByName<Button>("ConnectButton");
                if (connectBtn != null)
                    connectBtn.Text = "Connect";

                UpdateStatus("Disconnected", "Ready");
                Log("Disconnected");
            }
            catch (Exception ex)
            {
                Log($"Disconnect error: {ex.Message}");
            }
        }

        private async Task ReadSerialDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_serialPort?.IsOpen == true && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            string data = _serialPort.ReadLine();
                            this.Invoke(new Action(() => Log($"RX: {data}")));
                        }
                        await Task.Delay(50, cancellationToken);
                    }
                    catch (TimeoutException) { }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => Log($"Read error: {ex.Message}")));
            }
        }

        private void SendCommand(string command)
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.WriteLine(command);
                    Log($"TX: {command}");
                }
                else
                {
                    Log("Not connected");
                }
            }
            catch (Exception ex)
            {
                Log($"Send error: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            var displayTextBox = FindControlByName<TextBox>("DisplayTextBox");
            if (displayTextBox != null)
            {
                displayTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        private void UpdateStatus(string status, string info)
        {
            var statusLabel = FindControlByName<Label>("StatusLabel");
            var infoLabel = FindControlByName<Label>("InfoLabel");

            if (statusLabel != null)
                statusLabel.Text = status;
            if (infoLabel != null)
                infoLabel.Text = info;
        }

        private T? FindControlByName<T>(string name) where T : Control
        {
            foreach (Control control in this.Controls)
            {
                if (control.Name == name && control is T typedControl)
                    return typedControl;

                T? found = FindControlByNameRecursive<T>(control, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private T? FindControlByNameRecursive<T>(Control parent, string name) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Name == name && control is T typedControl)
                    return typedControl;

                T? found = FindControlByNameRecursive<T>(control, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
