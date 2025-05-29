# LightSync
# README.md

# LightSync (C# Edition)

Synchronize Nanoleaf 4D TV lights and Cync Full Color Dynamic Effect lights with low latency.

## Features

- Control Nanoleaf lights via their local API (async, low-latency)
- (Planned) Control Cync lights (requires API or integration)
- Sync color and effects between both brands

## Structure

- `NanoleafController.cs` — Class for Nanoleaf API control
- `CyncController.cs` — Class for Cync light control (stub, to be implemented)
- `SyncManager.cs` — Class to coordinate both controllers

## Usage

1. Fill in your Nanoleaf and Cync credentials in the respective controller classes.
2. Use `SyncManager` to set colors or effects on both lights.

## Example

```csharp
var nanoleaf = new NanoleafController("192.168.1.100", "YOUR_TOKEN");
var cync = new CyncController("YOUR_DEVICE_ID", "YOUR_TOKEN");
var sync = new SyncManager(nanoleaf, cync);

await sync.SyncColorAsync(255, 100, 50);
await sync.SyncEffectAsync("Sunrise");

```

# LightSync Project

## Goal
Synchronize Cync LED strips with Nanoleaf 4D screen-mirroring lights in real-time. The Cync strips should display colors based on the average color output from the Nanoleaf 4D.

## Current Status
-   **Nanoleaf Integration:** `NanoleafController.cs` successfully connects to a Nanoleaf device, authenticates, and uses Server-Sent Events (SSE) to listen for real-time average color changes. It raises a C# event (`StateChanged`) with RGB values. This component is working.
-   **Cync Integration (Planned):** `CyncController.cs` is implemented as an HTTP client to communicate with a local Cync control server (`iburistu/cync-lan`). It's designed to send "set color," "turn on/off," and "set brightness" commands.
-   **Synchronization Logic:** `SyncManager.cs` subscribes to `NanoleafController.StateChanged` events and is set up to call methods on `CyncController` to update the Cync lights.
-   **C# Project:** The `.csproj` file and basic `Program.cs` structure are in place.

## Cync Control Strategy
The chosen strategy is to use a third-party local Cync server, specifically **`iburistu/cync-lan`** (a Node.js application). This server will act as a bridge to control Cync Wi-Fi devices locally by emulating the official Cync cloud servers. This requires DNS redirection on the local network.

## Next Steps (To be performed after new router installation)

The following steps are crucial for enabling Cync light control and were deferred pending the installation of a new internet router.

**1. Ensure Cync Device is on Wi-Fi:**
   -   Using the official Cync mobile app, confirm that your Cync LED strip is a **Wi-Fi capable model** (e.g., "Direct Connect").
   -   Ensure it is **connected to your home Wi-Fi network**. If it has only been used via Bluetooth, you must add it to your Wi-Fi network through the Cync app. This is essential for `cync-lan` to discover and control it.

**2. Set up `iburistu/cync-lan` Server (on your macOS machine):**
   -   **Prerequisites:**
        *   Install **Node.js and npm**: Download the LTS version from [https://nodejs.org/](https://nodejs.org/) or use Homebrew (`brew install node`). Verify with `node -v` and `npm -v` in a new terminal.
        *   Install **OpenSSL**: If using Homebrew, `brew install openssl`.
   -   **Clone the repository:**
        ```bash
        git clone https://github.com/iburistu/cync-lan.git
        cd cync-lan
        ```
   -   **Install dependencies:**
        ```bash
        npm install
        ```
        (This also generates necessary SSL certificates).

**3. Configure DNS Redirection (on your NEW router):**
   -   **Find your Mac's Local IP Address:** (System Settings > Network > Wi-Fi/Ethernet).
   -   **Access your new router's admin page** (usually via its IP like `192.168.1.1` in a browser).
   -   **Locate DNS settings:** Look for sections like "DNS Settings," "Local DNS," "Static DNS," or "Static Hostnames." (Consult your new router's manual).
   -   **Add a custom DNS entry:**
        *   Map the hostname `cm.gelighting.com` to your Mac's local IP address.
        *   Optionally, also map `cm-ge.xlink.cn` to your Mac's IP if the first one alone doesn't work (as per `cync-lan` docs).
   -   Save the settings on your router. It might reboot.

**4. Start `cync-lan` Server and Identify Cync Device IP:**
   -   Navigate to the `cync-lan` directory in your terminal.
   -   **Start the server:**
        ```bash
        npm run start
        ```
   -   **Power cycle your Cync LED strip(s):** Unplug them and plug them back in. This forces them to re-query DNS.
   -   **Monitor `cync-lan` logs:** Watch the terminal output. If DNS redirection is correct and your Cync device is on Wi-Fi, it should connect to the `cync-lan` server.
   -   **Note the IP address of your Cync LED strip** as it appears in the `cync-lan` logs. This IP is needed for `Program.cs`.
   -   The `cync-lan` server also usually provides an API endpoint (e.g., `http://<your-mac-ip>:8080/api/devices`) to list connected Cync devices and their IPs.

**5. Update `Program.cs` in your LightSync Project:**
   -   Open `LightSync/Program.cs`.
   -   Modify the following placeholder constants:
        *   `NANOLEAF_HOST_IP`: Your Nanoleaf device's IP address.
        *   `NANOLEAF_AUTH_TOKEN`: Your Nanoleaf device's authentication token.
        *   `CYNC_LAN_SERVER_URL`: The URL of your `cync-lan` server (e.g., `http://<your-mac-ip>:8080` - replace `<your-mac-ip>` with your Mac's IP).
        *   `CYNC_DEVICE_IP`: The local IP address of your Cync LED strip (obtained from `cync-lan` logs in the previous step).

**6. Build and Run LightSync Application:**
   -   Compile and run your C# LightSync project.
   -   If all steps are successful, the Cync LED strip should now synchronize with the Nanoleaf 4D.

## C# Project Components
-   **`NanoleafController.cs`**: Handles all communication with the Nanoleaf device (connection, authentication, SSE event listening for color changes).
-   **`CyncController.cs`**: Acts as an HTTP client to send commands (set color, brightness, on/off) to the `iburistu/cync-lan` local server API.
-   **`SyncManager.cs`**: Subscribes to events from `NanoleafController` and triggers actions on `CyncController` to keep the lights synchronized.
-   **`Program.cs`**: Initializes controllers and the sync manager, and contains placeholder values for device IPs and URLs that need to be configured.
-   **`*.csproj`**: C# project file (ensure all .cs files are included).
