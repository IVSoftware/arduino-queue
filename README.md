Your general question is about executing tasks in multiple stages, but specifically _"trying to get a call/response system working with an Arduino"_. In that case, you could experiment with designing an `AwaitableCommand` base class along with a `Queue<AwaitableCommand>` structure to run any number of derived actions sequentially until the queue is empty. (For example, using your other [question](https://stackoverflow.com/q/78925195/5438626) and my [answer](https://stackoverflow.com/a/78925871/5438626) as a basis, you show a `Home` command that waits for "home done", and an XY seeking command that waits for both "x done" _and_ "y done" which can occur in either order.) An additional benefit is that any collection of `AwaitableCommand` could be easily written to, and reloaded from, a JSON file in order to save routines and load them in bulk to the queue.
___

*OP's question has gotten several upvotes so I'm attempting something of a canonical answer having worked with [Linduino](https://www.analog.com/en/resources/evaluation-hardware-and-software/evaluation-development-platforms/linduino.html) in test environments at LTC and ADI.*
___

#### Awaitable Commands...
To solve the problem of interacting with the same task awaiter multiple times, any new instance of a command will have its own (initially blocked) semaphore. This is going to change what it means when you say "request another instance of the same job" because now for example each new instance of `HomeCommand` will have an entirely new instance of the awaiter as well.
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

You mentioned (offline) that the Arduino can run concurrent processes, so a command like `XYCommand` might "Fire and Forget" two processes and then await for its `BusyX` and `BusyY` semaphores to be released in either order. 

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

I've incorporated this idea into your original receiver method as a starting point. As an improvement to your code, consider checking the `spL.BytesToRead` against the number of bytes you're expecting because it's possible to get a partial return. In other words, if the command is expecting "home done\n" then check for `System.Text.Encoding.ASCII.GetBytes("home done\n").Length` and spin until the Arduino has pushed ALL the bytes into its RX buffer.

```
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
```

___

#### Demo (WinForms)

The left panel shows eight awaitable commands that have been staged in memory using the [Mem+] button. 

The right panel shows the effect of clicking the [Run] menu item which rapidly dumps the list into the run queue.

The log shows:
- The Home command completing before X or Y.
- Multiple repetitions of XY command, where X and Y are awaited, and might come back in either order, but this transaction will be "atomic" in the sense that both X and Y must release before the queue will advance to the next program step.
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
            // Add one or more commands to queue based on valid (or not) values in UI controls.
            if(checkBoxHome.Checked) ArduinoComms.Enqueue(new HomeCommand());
            var xyCommand = new XYCommand();
            if(int.TryParse(textBoxX.Text, out int x)) xyCommand.X = x;
            if(int.TryParse(textBoxY.Text, out int y)) xyCommand.Y = y;
            if(xyCommand.Valid) ArduinoComms.Enqueue(xyCommand);
            if (int.TryParse(textBoxDelay.Text, out int delay))
            {
                var delayCommand = new DelayCommand { Delay = delay };
                ArduinoComms.Enqueue(delayCommand);
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
        ObservableCollection<AwaitableCommand> Memory { get; } = 
            new ObservableCollection<AwaitableCommand>();
    }
}
```


  [1]: https://i.sstatic.net/rEonMvak.png