using System;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    // ******************************************************************************************
    // !! IMPORTANT: REPLACE THESE PLACEHOLDERS WITH YOUR ACTUAL DEVICE DETAILS !!
    // ******************************************************************************************
    private const string NANOLEAF_HOST_IP = "YOUR_NANOLEAF_IP_ADDRESS"; // e.g., "192.168.1.100"
    private const string NANOLEAF_AUTH_TOKEN = "YOUR_NANOLEAF_AUTH_TOKEN"; // Get this by following Nanoleaf API instructions

    // For Cync, specify the cync-lan server URL and the IP of the Cync device to control
    private const string CYNC_LAN_SERVER_URL = "http://localhost:8080"; // Or your Mac's IP if cync-lan is running there
    private const string CYNC_DEVICE_IP = "YOUR_CYNC_DEVICE_IP"; // Get this from cync-lan server logs when device connects
    // ******************************************************************************************

    public static async Task Main(string[] args)
    {
        Console.WriteLine("LightSync Application Starting...");

        if (NANOLEAF_HOST_IP == "YOUR_NANOLEAF_IP_ADDRESS" || NANOLEAF_AUTH_TOKEN == "YOUR_NANOLEAF_AUTH_TOKEN")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: Nanoleaf IP address or Auth Token is not set in Program.cs.");
            Console.WriteLine("Please update the placeholder values before running.");
            Console.ResetColor();
        }

        if (CYNC_LAN_SERVER_URL == "http://localhost:8080" && CYNC_DEVICE_IP == "YOUR_CYNC_DEVICE_IP") // Basic check
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: Cync LAN Server URL or Device IP might not be configured in Program.cs.");
            Console.WriteLine("Please update placeholder values if they are not correct.");
            Console.ResetColor();
        }

        var nanoleafController = new NanoleafController(NANOLEAF_HOST_IP, NANOLEAF_AUTH_TOKEN);
        // Instantiate CyncController with the cync-lan server URL and device IP
        var cyncController = new CyncController(CYNC_LAN_SERVER_URL, CYNC_DEVICE_IP);

        using (var syncManager = new SyncManager(nanoleafController, cyncController))
        using (var cts = new CancellationTokenSource())
        {
            // Cync authentication is handled by the cync-lan server.
            // We assume it's running and accessible.
            Console.WriteLine("Cync control will be attempted via the cync-lan server.");
            Console.WriteLine($"Targeting Cync device IP: {CYNC_DEVICE_IP} via server: {CYNC_LAN_SERVER_URL}");

            Console.WriteLine("Press Enter to stop the application...");

            // Start the synchronization task in the background
            var syncTask = syncManager.StartSyncAsync(cts);

            // Wait for user to press Enter
            Console.ReadLine();

            Console.WriteLine("Stopping LightSync application...");
            syncManager.StopSync(); // Signal the sync manager to stop listening

            try
            {
                await syncTask; // Wait for the listening task to complete its cleanup
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Main: Sync task was canceled as expected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Main: Exception during sync task shutdown: {ex.Message}");
            }
        }

        Console.WriteLine("LightSync Application Exited.");
    }
}