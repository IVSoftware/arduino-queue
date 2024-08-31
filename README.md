Your general question is about executing tasks in multiple stages, but specifically _"trying to get a call/response system working with an Arduino"_. In that case, you might experiment with a `Queue<Command>` structure for this, because you could load it up with any number of [polymorphic](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/polymorphism) commands that await completion before proceeding to the next until the queue is empty. For example, your other [question](https://stackoverflow.com/q/78925195/5438626) and my [answer](https://stackoverflow.com/a/78925871/5438626) as a basis, you show a `Home` command that waits for "home done", and an XY seeking command waits for both "x done" _and_ "y done" which can occur in either order.
___

*This question is getting enough upvotes that I'd like to take a little time to attempt something of a canonical answer loosely based on my years of experience programming [Linduino](https://www.analog.com/en/resources/evaluation-hardware-and-software/evaluation-development-platforms/linduino.html) at LTC and ADI.*
___

#### Awaitable Commands...

```
public abstract class AwaitableCommand
{
    public abstract TaskAwaiter GetAwaiter();
    public override string ToString() => this.GetType().Name;
}
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
public class XYCommand : AwaitableCommand
{
    public int? X { get; set; }
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
public class DelayCommand : AwaitableCommand
{
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
```

___

#### ... And the Queue that runs them

You mentioned (offline) that the Arduino can run concurrent processes, so a command like XYCommand might "Fire and Forget" two processes and then await for its BusyX and BusyY semaphores to be released in either order. 

```
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
    .
    .
    .
}
```
___

#### Arduino Rx

Here's a preliminary take on a receiver method. As an improvement, check the `spL.BytesToRead` against the number of bytes you're _expecting_ because it's possible to get a partial return. In other words, if the command is expecting "home done\n" then check for `System.Text.Encoding.ASCII.GetBytes("home done\n").Length` and spin until the Arduino has pushed ALL the bytes into its RX buffer.

```
private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
{
    byte[] buf;
    switch (sender?.GetType().Name)
    {
        case nameof(SerialPort):
            var spL = (SerialPort)sender;
            buf = new byte[spL.BytesToRead]; //instantiates a buffer of appropriate length.
            spL.Read(buf, 0, buf.Length); //reads from the sender, which inherits the data from the sender, which *is* our serial port.
            break;
        case nameof(MockSerialPort):
            var mspL = (MockSerialPort)sender;
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
                    }
                    else Debug.Fail("Expecting response to match current command.");
                    Logger($"XDone");
                    break;
                case string c when c.Contains("y done", StringComparison.OrdinalIgnoreCase):
                    if (_currentCommand is XYCommand yProcess)
                    {
                        yProcess.BusyY.Release();
                    }
                    else Debug.Fail("Expecting response to match current command.");
                    Logger($"YDone");
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
```

___

#### Demo (WinForms)

Stage eight awaitable commands in memory using the [Save Command] button, and then rapidly enqueue them all using the [Run] menu item.

- The Home command always completes before X or Y
- The X and Y are awaited, and might come back in either order, but this transaction will be "atomic" in the sense that both X and Y must release before the queue will advance to the next program step.
- A delay, if specified, will execute on the PC side (rather than spin on the Arduino side) before advancing the queue.

[![logs][1]][1]

```
public partial class CommandComposerForm : Form
{
    public CommandComposerForm()
    {
        InitializeComponent();
        ArduinoComms = new ArduinoComms();
        ArduinoComms.Log += Log;
        
        buttonEnqueue.Click += (sender, e) =>
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
        buttonMemPlus.Click += (sender, e) =>
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
        runToolStripMenuItem.Click += (sender, e) =>
        {
            richTextBox.Clear();
            ArduinoComms.EnqueueAll(Memory);
        };
        
        void Log(object? sender, LoggerMessageArgs e, bool timeStamp = true)
        {
            if(ReferenceEquals(sender, this)) richTextBox.SelectionColor = Color.Blue;
            else richTextBox.SelectionColor = Color.Black;
            if(timeStamp) richTextBox.AppendText($@"{DateTime.Now:hh\:mm\:ss\.ffff}: {e.Message}{Environment.NewLine}");
            else richTextBox.AppendText($@"{e.Message}{Environment.NewLine}");
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        }
        .
        .
        .
    }
}
```


  [1]: https://i.sstatic.net/rEonMvak.png