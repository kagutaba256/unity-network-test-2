using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace NetworkTest2Server {
    public class Client {
        public int id;
        public TCP tcp;
        public static int dataBufferSize = 4096;
        public Client (int _clientId) {
            id = _clientId;
            tcp = new TCP (id);
        }
        public class TCP {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            private byte[] recieveBuffer;
            private Packet recievedData;
            public TCP (int _id) {
                id = _id;
            }
            public void Connect (TcpClient _socket) {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;
                stream = socket.GetStream ();
                recievedData = new Packet ();
                recieveBuffer = new byte[dataBufferSize];
                stream.BeginRead (recieveBuffer, 0, dataBufferSize, RecieveCallback, null);
                ServerSend.Welcome (id, "Welcome to the server");
            }
            public void SendData (Packet _packet) {
                try {
                    if (socket != null) {
                        stream.BeginWrite (_packet.ToArray (), 0, _packet.Length (), null, null);
                    }
                } catch (Exception _ex) {
                    Console.WriteLine ($"Error sending data to player {id} via TCP: {_ex}");
                }
            }

            private void RecieveCallback (IAsyncResult _result) {
                try {
                    int _byteLength = stream.EndRead (_result);
                    if (_byteLength <= 0) {
                        // TODO disconnect
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy (recieveBuffer, _data, _byteLength);
                    recievedData.Reset (HandleData (_data));
                    stream.BeginRead (recieveBuffer, 0, dataBufferSize, RecieveCallback, null);
                } catch (Exception _ex) {
                    Console.WriteLine ($"Error recieving TCP Data: {_ex}");
                    // TODO Properly disconnect client
                }
            }
            private bool HandleData (byte[] _data) {
                int _packetLength = 0;
                recievedData.SetBytes (_data);
                if (recievedData.UnreadLength () >= 4) {
                    _packetLength = recievedData.ReadInt ();
                    if (_packetLength <= 0) {
                        return true;
                    }
                }

                while (_packetLength > 0 && _packetLength <= recievedData.UnreadLength ()) {
                    byte[] _packetBytes = recievedData.ReadBytes (_packetLength);
                    ThreadManager.ExecuteOnMainThread (() => {
                        using (Packet _packet = new Packet (_packetBytes)) {
                            int _packetId = _packet.ReadInt ();
                            Server.packetHandlers[_packetId] (id, _packet);
                        }
                    });
                    _packetLength = 0;
                    if (recievedData.UnreadLength () >= 4) {
                        _packetLength = recievedData.ReadInt ();
                        if (_packetLength <= 0) {
                            return true;
                        }
                    }
                }
                if (_packetLength <= 1) {
                    return true;
                }
                return false;
            }
        }
        public class UDP {
            public UdpClient socket;
            public IPEndPoint endPoint;
        }
    }
}