using Newtonsoft.Json;
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
            };
            buttonSaveCommand.Click += (sender, e) =>
            {
                Command command;
                if(checkBoxHome.Checked)
                {
                    command = new HomeCommand();
                    Memory.Add(command);
                    Log(this, $"MEMORY: {command}");
                }
                var xyCommand = new XYCommand();
                if(int.TryParse(textBoxX.Text, out int x)) xyCommand.X = x;
                if(int.TryParse(textBoxY.Text, out int y)) xyCommand.Y = y;
                if(xyCommand.Valid)
                {
                    Memory.Add(xyCommand);
                    Log(this, $"MEMORY: {xyCommand}");
                }
            };
            buttonClearCommand.Click += (sender, e) =>
            {
                checkBoxHome.Checked = false; 
                textBoxX.Text = string.Empty; 
                textBoxY.Text = string.Empty;
            };
            buttonClearLog.Click += (sender, e) => richTextBox.Clear();
            textBoxX.TextChanged += localValidate;
            textBoxY.TextChanged += localValidate;
            checkBoxHome.CheckedChanged += localValidate;
            void localValidate(object? sender, EventArgs e)
            {
                buttonQueueCommand.Enabled = 
                    buttonSaveCommand.Enabled = 
                    buttonClearCommand.Visible =
                    (int.TryParse(textBoxX.Text, out var _) ||
                     int.TryParse(textBoxY.Text, out var _) ||
                     checkBoxHome.Checked);
            }
            richTextBox.TextChanged += (sender, e) =>buttonClearLog.Visible = richTextBox.Text.Any();
            loadToolStripMenuItem.Click += (sender, e) =>{ };
            saveToolStripMenuItem.Click += (sender, e) =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProgramPath));
                File.WriteAllText(ProgramPath, JsonConvert.SerializeObject(Memory));
            };
            clearToolStripMenuItem.Click += (sender, e) => Memory.Clear();
            runToolStripMenuItem.Click += (sender, e) =>
            {
                richTextBox.Clear();
                ArduinoComms.EnqueueAll(Memory);
            };
        }

        void Log(object? sender, LoggerMessageArgs e)
        {
            richTextBox.AppendText($@"{DateTime.Now:hh\:mm\:ss\.ffff}: {e.Message}{Environment.NewLine}");
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        }

        ArduinoComms ArduinoComms { get; }
        List<Command> Memory { get; } = new List<Command>();

        string ProgramPath => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                Assembly.GetExecutingAssembly().GetName().Name,
                "DefaultProgram.json");
    }
    [JsonConverter(typeof(CommandConverter))]
    public abstract class Command
    {
        public abstract TaskAwaiter GetAwaiter();
        public override string ToString() => this.GetType().Name;
    }
    [JsonObject (MemberSerialization.OptIn)]
    public class HomeCommand : Command
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
    public class XYCommand : Command
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
    public class ArduinoComms : Queue<Command>
    {
        object _critical = new object();
        SemaphoreSlim _running = new SemaphoreSlim(1, 1);
        public new void Enqueue(Command command)
        {
            lock (_critical)
            {
                base.Enqueue(command);
            }
            RunQueue();
        }
        public void EnqueueAll(IEnumerable<Command> commands)
        {
            lock (_critical)
            {
                foreach (var command in commands) base.Enqueue(command);
            }
            RunQueue();
        }

        Command? _currentCommand = default;

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
                            Logger($"RUNNING: {_currentCommand.GetType().Name}");
                            switch (_currentCommand)
                            {
                                case Command cmd when cmd is HomeCommand home:
                                    StartArduinoProcess(cmd: 2);
                                    await home;
                                    break;
                                case Command cmd when cmd is XYCommand xy:
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
                                default:
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
                        .Delay(TimeSpan.FromSeconds(_rando.Next(1, 4)))
                        .GetAwaiter()
                        .OnCompleted(() => Port_DataReceived(new MockSerialPort("x done"), default));
                    break;
                case 1:
                    Task
                        .Delay(TimeSpan.FromSeconds(_rando.Next(1, 4)))
                        .GetAwaiter()
                        .OnCompleted(() => Port_DataReceived(new MockSerialPort("y done"), default));
                    break;
                case 2:
                    Task
                        .Delay(TimeSpan.FromSeconds(_rando.Next(1, 4)))
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
                                Logger($"XDone {xProcess.X}");
                            }
                            else Debug.Fail("Expecting response to match current command.");
                            break;
                        case string c when c.Contains("y done", StringComparison.OrdinalIgnoreCase):
                            if (_currentCommand is XYCommand yProcess)
                            {
                                yProcess.BusyY.Release();
                                Logger($"YDone {yProcess.Y}");
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
        public static implicit operator LoggerMessageArgs(string msg) =>
            new LoggerMessageArgs { Message = msg };
        public string? Message { get; private set; }
    }
}
