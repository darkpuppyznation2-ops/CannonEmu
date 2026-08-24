using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace CannonEmuFrontend
{
    public partial class MainWindow : Form
    {
        private SerialPort? _serialPort;
        private bool _isRunning = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            SetupUI();
            LoadComPorts();
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
            ((ToolStripMenuItem)fileMenu).DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            this.Controls.Add(menuStrip);

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(30, 30, 30)
            };
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
            mainPanel.Controls.Add(displayPanel, 0, 1);
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
            mainPanel.Controls.Add(ctrlPanel, 0, 2);
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

        private void LoadComPorts()
        {
            var portCombo = this.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "PortCombo");
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
                var portCombo = this.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "PortCombo");
                var baudCombo = this.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "BaudCombo");
                var connectBtn = this.Controls.OfType<Button>().FirstOrDefault(c => c.Name == "ConnectButton");

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

                var connectBtn = this.Controls.OfType<Button>().FirstOrDefault(c => c.Name == "ConnectButton");
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
            var displayTextBox = this.Controls.OfType<TextBox>().FirstOrDefault(c => c.Name == "DisplayTextBox");
            if (displayTextBox != null)
            {
                displayTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
    }
}
