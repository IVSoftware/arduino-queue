using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace arduino_queue
{
    public partial class CommandComposerForm : Form
    {
        public CommandComposerForm()
        {
            InitializeComponent();
            ArduinoComms = new ArduinoComms();
            ArduinoComms.Log += Log;
            buttonQueueCommand.Click += (sender, e) =>
            {
                if(checkBoxHome.Checked) ArduinoComms.Enqueue(new HomeCommand());
                var xyCommand = new XYCommand();
                if(int.TryParse(textBoxX.Text, out int x)) xyCommand.X = x;
                if(int.TryParse(textBoxY.Text, out int y)) xyCommand.Y = y;
                if(xyCommand.Valid) ArduinoComms.Enqueue(xyCommand);
                if (int.TryParse(textBoxDelay.Text, out int delay))
                {
                    var delayCommand = new DelayCommand { Delay = delay };
                    Memory.Add(delayCommand);
                    Log(this, $"MEMORY: {delayCommand}");
                }
            };
            buttonSaveCommand.Click += (sender, e) =>
            {
                AwaitableCommand command;
                if(checkBoxHome.Checked)
                {
                    command = new HomeCommand();
                    Memory.Add(command);
                    Log(this, new LoggerMessageArgs($"MEMORY: {command}", false));
                }
                var xyCommand = new XYCommand();
                if(int.TryParse(textBoxX.Text, out int x)) xyCommand.X = x;
                if(int.TryParse(textBoxY.Text, out int y)) xyCommand.Y = y;
                if(xyCommand.Valid)
                {
                    Memory.Add(xyCommand);
                    Log(this, new LoggerMessageArgs($"MEMORY: {xyCommand}", false));
                }
                if (int.TryParse(textBoxDelay.Text, out int delay))
                {
                    var delayCommand = new DelayCommand { Delay = delay };
                    Memory.Add(delayCommand);
                    Log(this, new LoggerMessageArgs($"MEMORY: {delayCommand}", false));
                }
            };
            buttonClearCommand.Click += (sender, e) =>
            {
                checkBoxHome.Checked = false; 
                textBoxX.Text = string.Empty; 
                textBoxY.Text = string.Empty;
                textBoxDelay.Text = string.Empty;
                checkBoxHome.Focus();
            };
            buttonClearLog.Click += (sender, e) => richTextBox.Clear();
            textBoxX.TextChanged += localValidate;
            textBoxY.TextChanged += localValidate;
            textBoxDelay.TextChanged += localValidate;
            checkBoxHome.CheckedChanged += localValidate;
            void localValidate(object? sender, EventArgs e)
            {
                buttonQueueCommand.Enabled = 
                    buttonSaveCommand.Enabled = 
                    buttonClearCommand.Visible =
                    (int.TryParse(textBoxX.Text, out var _) ||
                     int.TryParse(textBoxY.Text, out var _) ||
                     int.TryParse(textBoxDelay.Text, out var _) ||
                     checkBoxHome.Checked);
            }
            richTextBox.TextChanged += (sender, e) =>buttonClearLog.Visible = richTextBox.Text.Any();
            loadToolStripMenuItem.Click += (sender, e) =>
            {
                if(File.Exists(ProgramPath))
                {
                    var json = File.ReadAllText(ProgramPath);
                    var staged = 
                        JsonConvert.DeserializeObject<ObservableCollection<AwaitableCommand>>(json) ??
                        new ObservableCollection<AwaitableCommand>();
                    Memory.Clear();
                    foreach(var command in staged)
                    {
                        Memory.Add(command);
                    }
                }
                richTextBox.Clear();
                foreach (var command in Memory)
                {
                    Log(this, new LoggerMessageArgs($"{command}", false));
                }
            };
            saveToolStripMenuItem.Click += (sender, e) =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProgramPath));
                File.WriteAllText(ProgramPath, JsonConvert.SerializeObject(Memory, Formatting.Indented));
            };
            clearToolStripMenuItem.Click += (sender, e) => Memory.Clear();
            runToolStripMenuItem.Click += (sender, e) =>
            {
                richTextBox.Clear();
                ArduinoComms.EnqueueAll(Memory);
            };
            listToolStripMenuItem.Click += (sender, e) =>
            {
                richTextBox.Clear();
                foreach (var command in Memory)
                {
                    Log(this, new LoggerMessageArgs($"{command}", false));
                }
            };
            editInNotepadToolStripMenuItem.Click += (sender, e) =>
            {
                if (File.Exists(ProgramPath))
                {
                    Process.Start("notepad.exe", ProgramPath);
                }
            };
            Memory.CollectionChanged += (sender, e) => 
                clearToolStripMenuItem.Enabled = 
                runToolStripMenuItem.Enabled = 
                listToolStripMenuItem.Enabled = 
                Memory.Any();
        }

        void Log(object? sender, LoggerMessageArgs e)
        {
            if(ReferenceEquals(sender, this)) richTextBox.SelectionColor = Color.Blue;
            else richTextBox.SelectionColor = Color.Black;
            if(e.IncludeTimeStamp) richTextBox.AppendText($@"{DateTime.Now:hh\:mm\:ss\.ffff}: {e.Message}{Environment.NewLine}");
            else richTextBox.AppendText($@"{e.Message}{Environment.NewLine}");
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        }

        ArduinoComms ArduinoComms { get; }
        ObservableCollection<AwaitableCommand> Memory { get; } = new ObservableCollection<AwaitableCommand>();

        string ProgramPath => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                Assembly.GetExecutingAssembly().GetName().Name,
                "DefaultProgram.json");
    }
    [JsonConverter(typeof(CommandConverter))]
    public abstract class AwaitableCommand
    {
        public abstract TaskAwaiter GetAwaiter();
        public override string ToString() => this.GetType().Name;
    }
    [JsonObject (MemberSerialization.OptIn)]
    public class HomeCommand : AwaitableCommand
    {
        public SemaphoreSlim Busy { get; } = new SemaphoreSlim(0, 1);
        public override TaskAwaiter GetAwaiter() => 
            Busy
            .WaitAsync()
            .GetAwaiter();
    }
    /// <summary>
    /// Wait for X and Y in either order
    /// </summary>
    [JsonObject (MemberSerialization.OptIn)]
    public class XYCommand : AwaitableCommand
    {
        [JsonProperty]
        public int? X { get; set; }
        [JsonProperty]
        public int? Y { get; set; }
        public bool Valid => X != null || Y != null;
        public SemaphoreSlim BusyX { get; } = new SemaphoreSlim(0, 1);
        public SemaphoreSlim BusyY { get; } = new SemaphoreSlim(0, 1);

        public override TaskAwaiter GetAwaiter()
        {
            return localReady().GetAwaiter();
            async Task localReady()
            {
                var tasks = new List<Task>();
                if (X != null)
                    tasks.Add(BusyX.WaitAsync());
                if (Y != null)
                    tasks.Add(BusyY.WaitAsync());
                await Task.WhenAll(tasks);
            }
        }
        public override string ToString()
        {
            var builder = new StringBuilder(this.GetType().Name);
            if (X != null)
                builder.Append($" {X}");
            if (Y != null)
                builder.Append($" {Y}");
            return builder.ToString();
        }
    }
    /// <summary>
    /// Program delay on PC side (not in Arduino)
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class DelayCommand : AwaitableCommand
    {
        [JsonProperty]
        public int? Delay { get; set; }
        public override TaskAwaiter GetAwaiter() =>
            Task
            .Delay(TimeSpan.FromMilliseconds(Delay ?? 0))
            .GetAwaiter();
        public override string ToString()
        {
            var builder = new StringBuilder(this.GetType().Name);
            if (Delay != null)
                builder.Append($" {Delay}");
            return builder.ToString();
        }
    }
    public class ArduinoComms : Queue<AwaitableCommand>
    {
        object _critical = new object();
        SemaphoreSlim _running = new SemaphoreSlim(1, 1);
        public new void Enqueue(AwaitableCommand command)
        {
            lock (_critical)
            {
                base.Enqueue(command);
            }
            RunQueue();
        }
        public void EnqueueAll(IEnumerable<AwaitableCommand> commands)
        {
            lock (_critical)
            {
                foreach (var command in commands) base.Enqueue(command);
            }
            RunQueue();
        }

        AwaitableCommand? _currentCommand = default;

        private async void RunQueue()
        {
            // Do not allow reentry
            if (_running.Wait(0))
            {
                try
                {
                    while (true)
                    {
                        lock (_critical)
                        {
                            if (this.Any())
                            {
                                _currentCommand = Dequeue();
                            }
                            else _currentCommand = null;
                        }
                        if (_currentCommand is null)
                        {
                            Logger("QUEUE EMPTY");
                            return;
                        }
                        else
                        {
                            Logger($"RUNNING: {_currentCommand}");
                            switch (_currentCommand)
                            {
                                case AwaitableCommand cmd when cmd is HomeCommand home:
                                    StartArduinoProcess(cmd: 2);
                                    await home;
                                    break;
                                case AwaitableCommand cmd when cmd is XYCommand xy:
                                    if (xy.X is int x)
                                    {
                                        StartArduinoProcess(cmd: 0);
                                    }
                                    else xy.BusyX.Release();
                                    if (xy.Y is int y)
                                    {
                                        StartArduinoProcess(cmd: 1);
                                    }
                                    else xy.BusyY.Release();
                                    await xy;
                                    break;
                                case AwaitableCommand cmd when cmd is DelayCommand delay:
                                    // Spin this here, on the client side.
                                    // Don't make Arduino do it.
                                    await delay;
                                    Logger($"Delay Done {delay.Delay}");
                                    break;
                                default:
                                    Logger("UNRECOGNIZED COMMAND");
                                    break;
                            }
                        }
                    }
                }
                finally
                {
                    _running.Release();
                }
            }
        }
        #region S I M

        Random _rando = new Random(Seed: 2); // Seed is for repeatability during testing
        private void StartArduinoProcess(int cmd)
        {
            // The task delays simulate the seek time, but the Arduino has
            // concurrent processes so do 'not' await the delays. Yes, this
            // means that the ack can come at any time in any order.
            switch (cmd)
            {
                case 0:
                    Task
                        .Delay(TimeSpan.FromMilliseconds(_rando.Next(100, 500)))
                        .GetAwaiter()
                        .OnCompleted(() => Port_DataReceived(new MockSerialPort("x done"), default));
                    break;
                case 1:
                    Task
                        .Delay(TimeSpan.FromMilliseconds(_rando.Next(100, 500)))
                        .GetAwaiter()
                        .OnCompleted(() => Port_DataReceived(new MockSerialPort("y done"), default));
                    break;
                case 2:
                    Task
                        .Delay(TimeSpan.FromMilliseconds(_rando.Next(100, 500)))
                        .GetAwaiter()
                        .OnCompleted(() => Port_DataReceived(new MockSerialPort("home done"), default));
                    break;
                default:
                    Debug.Fail("Unrecognized command");
                    break;
            }
        }
        class MockSerialPort
        {
            public MockSerialPort(string ack) => SimBuffer = System.Text.Encoding.ASCII.GetBytes(ack);
            public byte[] SimBuffer { get; }
        };
        #endregion S I M
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buf;
            switch (sender)
            {
                case object o when o is SerialPort spL:
                    buf = new byte[spL.BytesToRead]; //instantiates a buffer of appropriate length.
                    spL.Read(buf, 0, buf.Length); //reads from the sender, which inherits the data from the sender, which *is* our serial port.
                    break;
                case object o when o is MockSerialPort mspL:
                    buf = mspL.SimBuffer;
                    break;
                default: throw new NotImplementedException();
            }

            var ascii = $"{System.Text.Encoding.ASCII.GetString(buf)}"; //assembles the byte array into a string.
            Logger($"Received: {ascii}"); //prints the result for debug.
            string[] thingsToParse = ascii.Split('\n'); //splits the string into an array along the newline in case multiple responses are sent in the same message.

            foreach (string thing in thingsToParse) //checks each newline instance individually.
            {
                try
                {
                    switch (thing)
                    {
                        case string c when c.Contains("home done", StringComparison.OrdinalIgnoreCase): //checks incoming data for the arduino's report phrase "Home done" when it is homed.
                            if(_currentCommand is HomeCommand home)
                            {
                                home.Busy.Release();
                            }
                            else Debug.Fail("Expecting response to match current command.");
                            Logger($"Homed");
                            break;
                        case string c when c.Contains("x done", StringComparison.OrdinalIgnoreCase):
                            if (_currentCommand is XYCommand xProcess)
                            {
                                xProcess.BusyX.Release();
                                Logger($"X Done {xProcess.X}");
                            }
                            else Debug.Fail("Expecting response to match current command.");
                            break;
                        case string c when c.Contains("y done", StringComparison.OrdinalIgnoreCase):
                            if (_currentCommand is XYCommand yProcess)
                            {
                                yProcess.BusyY.Release();
                                Logger($"Y Done {yProcess.Y}");
                            }
                            else Debug.Fail("Expecting response to match current command.");
                            break;

                        default: break; //do nothing
                    }
                }
                catch (Exception)
                {
                    // DO: figure out what went wrong, because this shouldn't happen
                    // DON'T: Crash
                }
            }
        }
        public event EventHandler<LoggerMessageArgs> Log;
        public void Logger(string message) => Log?.Invoke(this, message);
    }

    public class LoggerMessageArgs
    {
        public LoggerMessageArgs(string message, bool includeTImeStamp = true)
        {
            Message = message;
            IncludeTimeStamp = includeTImeStamp;
        }
        public static implicit operator LoggerMessageArgs(string message) =>
            new LoggerMessageArgs(message);
        public string? Message { get; }
        public bool IncludeTimeStamp { get; } = true;
    }
}
