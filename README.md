
# FishyUnityTransport

A UnityTransport implementation for Fish-Net.

	This is an improved fork from https://github.com/matthewshirley/FishyUTP.

The FishyUnityTransport library API is close to **[UnityTransport for Netcode For Gameobjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/tree/develop/com.unity.netcode.gameobjects/Runtime/Transports/UTP)** and uses some of its code.

## Dependencies
Make sure you have the following packages installed:
1. **[UnityTransport](https://docs-multiplayer.unity3d.com/transport/current/install)**
2. **[Fish-Net](https://github.com/FirstGearGames/FishNet)**

## Setting Up
1. Install Fish-Net from the **[official repo](https://github.com/FirstGearGames/FishNet/releases)** or **[Asset Store](https://assetstore.unity.com/packages/tools/network/fish-net-networking-evolved-207815)**.
2. Install **[UnityTransport](https://docs-multiplayer.unity3d.com/transport/current/install)** package.
3. Install FishyUnityTransport **[unitypackage](https://github.com/ooonush/FishyUnityTransport/releases)** from the release section.
4. Add the **"FishyUnityTransport"** component to your **"NetworkManager"**.

## Unity Relay support
This library supports Unity Relay, and since its API is similar to the UnityTransport for NGO API, I recommend reading the official Relay **[documentation](https://docs.unity.com/relay/en/manual/relay-and-ngo)**.

### Simple host connection sample:
```csharp
// Get FishyUnityTransport  
var utp = (FishyUnityTransport.FishyUnityTransport)NetworkManager.TransportManager.Transport;  
  
// Setup HostAllocation  
Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(4);  
utp.SetRelayServerData(new RelayServerData(hostAllocation, "dtls"));  
  
// Start Server Connection  
NetworkManager.ServerManager.StartConnection();  
  
// Setup JoinAllocation  
string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);  
JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);  
utp.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));  
  
// Start Client Connection  
NetworkManager.ClientManager.StartConnection();
```
