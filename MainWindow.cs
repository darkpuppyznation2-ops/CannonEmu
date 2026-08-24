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
            this.Text = "Cannon Emulator Frontend";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // Menu strip
            var menuStrip = new MenuStrip();
            var fileMenu = menuStrip.Items.Add("&File");
            ((ToolStripMenuItem)fileMenu).DropDownItems.Add("Open ROMs Folder", null, (s, e) => OpenRomsFolder());
            ((ToolStripMenuItem)fileMenu).DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            this.Controls.Add(menuStrip);

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(30, 30, 30)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Connection panel
            var connPanel = new GroupBox
            {
                Text = "Connection Settings",
                Padding = new Padding(10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            connPanel.Controls.AddRange(CreateConnectionControls());
            mainPanel.Controls.Add(connPanel, 0, 0);
            mainPanel.SetColumnSpan(connPanel, 2);

            // ROM panel
            var romPanel = new GroupBox
            {
                Text = "ROMs",
                Padding = new Padding(10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            romPanel.Controls.AddRange(CreateROMControls());
            mainPanel.Controls.Add(romPanel, 0, 1);
            mainPanel.SetColumnSpan(romPanel, 2);

            // Display panel
            var displayPanel = new GroupBox
            {
                Text = "Display",
                Padding = new Padding(10),
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            var displayTextBox = new TextBox
            {
                Name = "DisplayTextBox",
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.Lime,
                Font = new Font("Courier New", 10)
            };
            displayPanel.Controls.Add(displayTextBox);
            mainPanel.Controls.Add(displayPanel, 0, 2);
            mainPanel.SetColumnSpan(displayPanel, 2);

            // Control panel
            var ctrlPanel = new GroupBox
            {
                Text = "Controls",
                Padding = new Padding(10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            ctrlPanel.Controls.AddRange(CreateControlButtons());
            mainPanel.Controls.Add(ctrlPanel, 0, 3);
            mainPanel.SetColumnSpan(ctrlPanel, 2);

            this.Controls.Add(mainPanel);
        }

        private Control[] CreateConnectionControls()
        {
            var controls = new List<Control>();
            var tableLayout = new TableLayoutPanel
            {
                RowCount = 3,
                ColumnCount = 3,
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // COM Port
            var portLabel = new Label { Text = "COM Port:", AutoSize = true, ForeColor = Color.White };
            var portCombo = new ComboBox { Name = "PortCombo", Width = 150 };
            tableLayout.Controls.Add(portLabel, 0, 0);
            tableLayout.Controls.Add(portCombo, 1, 0);

            // Baud Rate
            var baudLabel = new Label { Text = "Baud Rate:", AutoSize = true, ForeColor = Color.White };
            var baudCombo = new ComboBox { Name = "BaudCombo", Width = 150 };
            baudCombo.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            baudCombo.SelectedIndex = 0;
            tableLayout.Controls.Add(baudLabel, 0, 1);
            tableLayout.Controls.Add(baudCombo, 1, 1);

            // Connect button
            var connectBtn = new Button
            {
                Name = "ConnectButton",
                Text = "Connect",
                Width = 100,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            connectBtn.Click += ConnectButton_Click;
            tableLayout.Controls.Add(connectBtn, 2, 0);

            // Refresh ports button
            var refreshBtn = new Button
            {
                Text = "Refresh",
                Width = 100,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White
            };
            refreshBtn.Click += (s, e) => LoadComPorts();
            tableLayout.Controls.Add(refreshBtn, 2, 1);

            controls.Add(tableLayout);
            return controls.ToArray();
        }

        private Control[] CreateROMControls()
        {
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 45),
                FlowDirection = FlowDirection.TopDown
            };

            // ROMs List
            var romListBox = new ListBox
            {
                Name = "ROMListBox",
                Width = 750,
                Height = 100,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White
            };
            flowLayout.Controls.Add(romListBox);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 45)
            };

            var loadBtn = new Button
            {
                Text = "Load Selected ROM",
                Width = 120,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            loadBtn.Click += (s, e) => LoadSelectedROM();
            buttonPanel.Controls.Add(loadBtn);

            var refreshRomsBtn = new Button
            {
                Text = "Refresh ROMs",
                Width = 100,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White
            };
            refreshRomsBtn.Click += (s, e) => LoadROMs();
            buttonPanel.Controls.Add(refreshRomsBtn);

            var openFolderBtn = new Button
            {
                Text = "Open Folder",
                Width = 100,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White
            };
            openFolderBtn.Click += (s, e) => OpenRomsFolder();
            buttonPanel.Controls.Add(openFolderBtn);

            flowLayout.Controls.Add(buttonPanel);

            return new[] { flowLayout };
        }

        private Control[] CreateControlButtons()
        {
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 45)
            };

            var buttons = new[]
            {
                ("Reset", new EventHandler((s, e) => SendCommand("RESET"))),
                ("Start", new EventHandler((s, e) => SendCommand("START"))),
                ("Stop", new EventHandler((s, e) => SendCommand("STOP"))),
                ("Fire", new EventHandler((s, e) => SendCommand("FIRE"))),
            };

            foreach (var (label, handler) in buttons)
            {
                var btn = new Button
                {
                    Text = label,
                    Width = 80,
                    Height = 40,
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                btn.Click += handler;
                flowLayout.Controls.Add(btn);
            }

            return new[] { flowLayout };
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
            string[] validExtensions = { ".bin", ".rom", ".gb", ".gba", ".nes", ".sfc", ".z64" };
            string extension = Path.GetExtension(filePath).ToLower();
            return validExtensions.Contains(extension);
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

                Log($"Connected to {port} at {baudRate} baud");

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReadSerialDataAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
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
