using System;
using System.Threading;

/// <summary>
/// Manages the Emuera game execution thread.
/// The game runs on a separate thread, independent of Unity's frame rate.
/// This allows the game logic to continue processing even when not rendering.
/// </summary>
public class EmueraThread
{
    /// <summary>
    /// Gets the singleton instance of the EmueraThread.
    /// </summary>
    public static EmueraThread instance => instance_;
    private static readonly EmueraThread instance_ = new EmueraThread();

    // Thread synchronization
    private readonly object sync_lock_ = new object();
    private readonly ManualResetEventSlim input_event_ = new ManualResetEventSlim(false);
    
    // Thread state
    private Thread game_thread_;
    private volatile bool running_;
    private volatile bool debugmode_;
    
    // Input state (volatile for thread safety)
    private volatile string input_;
    private volatile bool skipflag_;
    
    // Performance tracking
    private const int TIMER_UPDATE_INTERVAL_MS = 1;
    private const int INPUT_PROCESS_DELAY_MS = 10;

    private EmueraThread()
    { }

    /// <summary>
    /// Starts the game engine on a background thread.
    /// </summary>
    /// <param name="debug">Enable debug mode.</param>
    /// <param name="use_coroutine">Ignored - always uses background thread for framerate independence.</param>
    public void Start(bool debug, bool use_coroutine)
    {
        debugmode_ = debug;
        running_ = true;
        input_event_.Reset();
        
        // Always use background thread for framerate-independent execution
        game_thread_ = new Thread(Work)
        {
            Name = "EmueraGameThread",
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };
        game_thread_.Start();
    }

    /// <summary>
    /// Stops the game execution thread.
    /// </summary>
    public void End()
    {
        running_ = false;
        input_event_.Set(); // Wake up any waiting thread
        
        // Wait for thread to finish (with timeout)
        if (game_thread_ != null && game_thread_.IsAlive)
        {
            if (!game_thread_.Join(TimeSpan.FromSeconds(2)))
            {
                // Thread didn't stop gracefully - this shouldn't happen
                // but we handle it to prevent hangs
                uEmuera.Logger.Warn("Game thread did not stop gracefully");
            }
        }
        game_thread_ = null;
    }

    /// <summary>
    /// Checks if the game is currently processing.
    /// </summary>
    /// <returns>True if the game is actively processing.</returns>
    public bool Running()
    {
        var console = MinorShift.Emuera.GlobalStatic.Console;
        return console != null && console.IsInProcess;
    }

    /// <summary>
    /// Sends input to the game.
    /// </summary>
    /// <param name="c">The input string.</param>
    /// <param name="from_button">Whether the input is from a button press.</param>
    /// <param name="skip">Whether to skip (double-click behavior).</param>
    public void Input(string c, bool from_button, bool skip = false)
    {
        var console = MinorShift.Emuera.GlobalStatic.Console;
        if (console == null)
            return;
        if (!from_button && console.IsWaitingInputSomething)
            return;
            
        lock (sync_lock_)
        {
            input_ = c;
            skipflag_ = skip;
            input_event_.Set(); // Signal that input is available
        }
    }
    
    /// <summary>
    /// Gets whether the skip flag is set.
    /// </summary>
    public bool IsSkipFlag => skipflag_;

    /// <summary>
    /// Main game loop running on a background thread.
    /// This loop is independent of Unity's frame rate.
    /// </summary>
    private void Work()
    {
        try
        {
            // Initialize game
            MinorShift.Emuera.Program.debugMode = debugmode_;
            MinorShift.Emuera.Program.Main(Array.Empty<string>());

            uEmuera.Utils.ResourceClear();
            GC.Collect();

            input_ = null;
            
            while (running_)
            {
                skipflag_ = false;

                // Wait for input with periodic timer updates
                while (input_ == null)
                {
                    // Wait for input signal with timeout for timer updates
                    if (input_event_.Wait(TIMER_UPDATE_INTERVAL_MS))
                    {
                        // Input received, exit wait loop
                        input_event_.Reset();
                        break;
                    }
                    
                    if (!running_)
                        return;
                        
                    // Update timers even while waiting for input
                    uEmuera.Forms.Timer.Update();
                }

                // Get fresh console reference each iteration (may be disposed during game exit)
                var console = MinorShift.Emuera.GlobalStatic.Console;
                
                // Process input if console is ready and not disposed
                // Use null-conditional to avoid race condition where console could be disposed between checks
                if (console?.IsWaitingInput == true)
                {
                    string current_input;
                    bool current_skip;
                    
                    lock (sync_lock_)
                    {
                        current_input = input_;
                        current_skip = skipflag_;
                    }
                    
                    if (console.IsWaitingEnterKey)
                        current_input = "";
                        
                    console.PressEnterKey(current_skip, current_input, false);
                }
                
                // Small delay to prevent CPU spinning
                Thread.Sleep(INPUT_PROCESS_DELAY_MS);
                
                lock (sync_lock_)
                {
                    input_ = null;
                }
            }
        }
        catch (Exception ex) when (ex is ThreadInterruptedException || ex is OperationCanceledException)
        {
            // Thread was interrupted or cancelled - expected during shutdown
        }
        catch (Exception ex)
        {
            uEmuera.Logger.Error($"Game thread error: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
