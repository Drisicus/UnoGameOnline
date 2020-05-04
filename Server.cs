using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnoServer
{
    class Server {

        private const int MAX_CONNECTIONS = 10;

        static int lastPlayerIndex = -1;

        // Juegos que tienen al menos un player
        static List<Game> gamesInProgress;

        // Lockers para controlar el acceso a valores de cada cliente por cada juego
        static readonly List<object> lockers = new List<object>();

        // [gameId][playerId][playerSocket]
        Dictionary<int, Dictionary<int, Socket>> gameAndPlayerSockets = new Dictionary<int, Dictionary<int, Socket>>();

        static void Main(string[] args) {

            UInt16 playersPerGame = 2;

            gamesInProgress = new List<Game>();

            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress iPAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(iPAddress, 5555);

            try {
                Socket socketListener = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socketListener.Bind(localEndPoint);
                socketListener.Listen(MAX_CONNECTIONS);

                while (true) {
                    Console.WriteLine("Waiting for a connection...");
                    Socket socket = socketListener.Accept();
                    Console.WriteLine("Connected to {0}" + socket.RemoteEndPoint.ToString());

                    // Send player ID
                    lastPlayerIndex += 1;
                    
                    // Game id depending on number of players
                    int gameId = (int)(lastPlayerIndex - (lastPlayerIndex % playersPerGame)) / playersPerGame;

                    if (lastPlayerIndex % playersPerGame == 0) {
                        Console.WriteLine("Creating game and its locker");
                        Game game = new Game(playersPerGame, gameId);
                        gamesInProgress.Add(game);
                        lockers.Add(new object());
                    } else {
                        Console.WriteLine("Assigning existing game to player and unlocking players of that game");
                        lock (lockers[gameId]) {
                            gamesInProgress[gameId].isGameReady = true;
                            Monitor.PulseAll(lockers[gameId]);
                        }
                    }

                    Thread newTread = new Thread(() => ClientManagementThread(socket, lastPlayerIndex, gameId));
                    newTread.Start();
                }
           
                socketListener.Shutdown(SocketShutdown.Both);
                socketListener.Close();

            }
            catch (Exception exception) {
                Console.WriteLine(exception.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();

        }



        public static void ClientManagementThread(Socket socket, int playerId, int gameId) {
            Console.WriteLine("Thread para player: {0}", playerId);

            try {
                byte[] buffer = new byte[1024];
                byte[] playerIdMessage = Utils.WrapMessage(BitConverter.GetBytes(playerId));
                int bytesSent = socket.Send(playerIdMessage);
                Game game;
                while (socket.Connected) {

                    game = gamesInProgress[gameId];

                    // Lock hasta que se conecten los players suficientes
                    lock (lockers[gameId]) {
                        while (!game.isGameReady) {
                            Monitor.Wait(lockers[gameId]);
                        }
                    }

                    // Se envia el estado del game.
                    bytesSent = SendGameToPlayers(socket, game);

                    // Se espera hasta que el active player conteste
                    byte[] totalBytes = Utils.ReceiveMessage(socket, buffer);

                    Thread.Sleep(500);
                }
            }
            catch (ArgumentNullException ane) { Console.WriteLine("ArgumentNullException : {0}", ane.ToString()); }
            catch (SocketException se) { Console.WriteLine("SocketException : {0}", se.ToString()); }
            catch (Exception e) { Console.WriteLine("Unexpected exception : {0}", e.ToString()); }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public static int SendGameToPlayers(Socket socket, Game game) {
            string jsonGame = Utils.Serialize(game);
            byte[] bytesGame = Encoding.Unicode.GetBytes(jsonGame);
            byte[] gameWithLenghPrefix = Utils.WrapMessage(bytesGame);
            return socket.Send(gameWithLenghPrefix);
        }
        private static Game receiveGameFromPlayer(Socket socketClient, byte[] buffer) {
            byte[] totalBytes = Utils.ReceiveMessage(socketClient, buffer);
            string jsonGame = Encoding.Unicode.GetString(totalBytes);
            Game game = Utils.Deserialize(jsonGame);
            Console.WriteLine("Game successfully received. My GameId: {0}", game.gameId);
            return game;
        }

    }
}
