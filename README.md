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
