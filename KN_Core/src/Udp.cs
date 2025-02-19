using System.Reflection;
using SyncMultiplayer;

namespace KN_Core {
  public class Udp {
    public const int TypeSuspension = 0xE4;
    public const int TypeSwaps = 0xE6;
    public const int TypeLights = 0xE7;
    public const int TypeHazard = 0xE8;

    public SmartfoxRoomClient Room { get; private set; }
    public SmartfoxClient Client { get; private set; }

    public NetGameSubroomsSystem SubRoom { get; private set; }

    public bool ReloadClient;
    public bool ReloadSubRoom;

    public delegate void PacketCallback(SmartfoxDataPackage data);
    public event PacketCallback ProcessPacket;

    private readonly string guidString_;
    private MethodInfo sendSubRoomId_;

    public Udp() {
      guidString_ = "8de61547-4c31-49cd-b8c6-7e12d6ff23bc";
    }

    public void Update() {
      if (Room == null || ReloadClient) {
        Room = NetworkController.InstanceGame.Client;
        TrySetupSubRoom();
        TrySetupClient();
      }

      if (SubRoom == null || ReloadSubRoom) {
        TrySetupSubRoom();
      }

      if (Client == null || ReloadClient) {
        TrySetupClient();
      }

      if (Client != null && !Client.Sfs.IsConnected) {
        Client.Sfs.InitUDP();
      }

      if (Client != null && Client.State != ClientState.Joined) {
        Client.Sfs.InitUDP();
      }
    }

    public static SmartfoxDataPackage MakePackage() {
      return new SmartfoxDataPackage(PacketId.Chat);
    }
    
    public void Send(SmartfoxDataPackage data) {
      if (Client != null) {
        Client.Send(data.ToDataPackage(), true);
        if (Client.State != ClientState.Joined) {
          ReloadClient = true;
          Client.Sfs.InitUDP();
        }
      }
    }

    public void SendChangeRoomId(NetworkPlayer receiver, bool enabled) {
      if (SubRoom != null) {
        string guid = enabled ? "" : guidString_;
        sendSubRoomId_?.Invoke(SubRoom, new object[] {receiver, guid});
      }
    }

    private void TrySetupSubRoom() {
      SubRoom = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
      sendSubRoomId_ = typeof(NetGameSubroomsSystem).GetMethod("SEND_ChangeRoomID", BindingFlags.NonPublic | BindingFlags.Instance);

      ReloadSubRoom = false;
    }

    private void TrySetupClient() {
      if (Room != null) {
        Client = typeof(SmartfoxRoomClient).GetField("m_client", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(Room) as SmartfoxClient;

        if (!Client?.Sfs.IsConnected ?? false) {
          Client?.Sfs.InitUDP();
        }

        NetworkController.InstanceGame.packetHandler.Subscribe(PacketId.Subroom, MainPacketHandler);
        typeof(SmartfoxRoomClient).GetField("m_client", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(Room, Client);

        ReloadClient = false;
      }
    }

    private void MainPacketHandler(NetworkPlayer sender, SmartfoxDataPackage data) {
      ProcessPacket?.Invoke(data);
    }
  }
}