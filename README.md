Your general question is about executing tasks in multiple stages, but specifically _"with Arduino over a serial port"_. In that case, you might experiment with a `Queue<Command>` structure for this, because you could load it up with any number of [polymorphic](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/polymorphism) commands that await completion before proceeding to the next until the queue is empty. For example, your other [question]() and my [answer]() as a basis shows a `Home` command that waits for "home done", and an XY seeking command waits for both "x done" _and_ "y done" which can occur in either order. 

___

#### Awaitable Commands...

```

public abstract class Command
{
    public abstract TaskAwaiter GetAwaiter();
}

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
public class XYCommand : Command
{
    public SemaphoreSlim BusyX { get; } = new SemaphoreSlim(0, 1);
    public SemaphoreSlim BusyY { get; } = new SemaphoreSlim(0, 1);
    public int? X { get; set; }
    public int? Y { get; set; }
    public bool Valid => X != null || Y != null;

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
}
```

___

#### ... And the Queue that runs them

You mentioned (offline) that the Arduino can run concurrent processes, so a command like XYCommand might "Fire and Forget" two processes and then await for its BusyX and BusyY semaphores to be released in either order. 

```
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

#### Demo (WinForms)

If the command is set up as shown, and the [QueueCommand] button is clicked 3x:

- The Home command always completes before X or Y
- The X and Y are awaited, but might come back in either order.

[![log][1]][1]

```
public partial class CommandComposerForm : Form
{
    public CommandComposerForm()
    {
        InitializeComponent();
        ArduinoComms = new ArduinoComms();
        ArduinoComms.Log += (sender, e) =>
        {
            richTextBox.AppendText($@"{DateTime.Now:hh\:mm\:ss\.ffff}: {e.Message}{Environment.NewLine}");
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        };
        buttonQueueCommand.Click += (sender, e) =>
        {
            if(checkBoxHome.Checked) ArduinoComms.Enqueue(new HomeCommand());
            var xyCommand = new XYCommand();
            if(int.TryParse(textBoxX.Text, out int x)) xyCommand.X = x;
            if(int.TryParse(textBoxY.Text, out int y)) xyCommand.Y = y;
            if(xyCommand.Valid) ArduinoComms.Enqueue(xyCommand);
        };
        buttonClearCommand.Click += (sender, e) =>
        {
            checkBoxHome.Checked = false; 
            textBoxX.Text = string.Empty; 
            textBoxY.Text = string.Empty;
        };
        textBoxX.TextChanged += localValidate;
        textBoxY.TextChanged += localValidate;
        checkBoxHome.CheckedChanged += localValidate;
        void localValidate(object? sender, EventArgs e)
        {
            buttonQueueCommand.Enabled = buttonClearCommand.Enabled =
                (int.TryParse(textBoxX.Text, out var _) ||
                    int.TryParse(textBoxY.Text, out var _) ||
                    checkBoxHome.Checked);
        }
    }
    ArduinoComms ArduinoComms { get; }
}
```

  [1]: https://i.sstatic.net/z1GxoPW5.png