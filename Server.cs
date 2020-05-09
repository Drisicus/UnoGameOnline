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

        // [gameId][playerId][playerSocket]
        static Dictionary<int, Dictionary<int, Socket>> gameAndPlayerSockets = new Dictionary<int, Dictionary<int, Socket>>();

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
                        gameAndPlayerSockets.Add(gameId, new Dictionary<int, Socket>());
                        gameAndPlayerSockets[gameId].Add(lastPlayerIndex, socket);
                    }
                    else if (lastPlayerIndex % playersPerGame == playersPerGame - 1) {
                        Console.WriteLine("Assigning existing game to player and unlocking players of that game");
                        gameAndPlayerSockets[gameId].Add(lastPlayerIndex, socket);
                        gamesInProgress[gameId].isGameReady = true;
                        Thread newTread = new Thread(() => ClientManagementThread(gameId));
                        newTread.Start();
                    }
                    else {
                        gameAndPlayerSockets[gameId].Add(lastPlayerIndex, socket);
                    }
                }
            }
            catch (Exception exception) {
                Console.WriteLine(exception.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }

        // Un thread por cada Game. Se encarga de notificar a cada player su Id y controlar la partida.
        public static void ClientManagementThread(int gameId) {
            Console.WriteLine("Thread para game: {0}", gameId);
            Dictionary<int, Socket> playersAndSockets = gameAndPlayerSockets[gameId];
            int[] playersIds = playersAndSockets.Keys.ToArray();
            byte[] buffer = new byte[1024];
            Game game = gamesInProgress[gameId];

            try {

                // 1. Mandar a cada player su playerID 
                foreach (var playerId in playersAndSockets.Keys) {
                    Socket socket = playersAndSockets[playerId];
                    byte[] playerIdMessage = Utils.WrapMessage(BitConverter.GetBytes(playerId));
                    int bytesSent = socket.Send(playerIdMessage);
                }

                while (!game.isGameOver) {
                    // 2. Mandar a cada player el estado actual del game
                    SendGameToAllPlayers(game, playersAndSockets);

                    // 3. Esperar hasta que el player activo conteste
                    // Se recibe el game con la activeCard actualizada, y las cartas del jugador tambien
                    Socket activePlayerSocket = playersAndSockets[playersIds[game.activePlayerIdx]];
                    Game updatedGame = receiveGameFromPlayer(activePlayerSocket, buffer);

                    // 4. Se debe decidir que hacer con la active card antes de elegir al siguiente jugador
                    game = updateGameWithLastPlayerDecision(updatedGame, game);

                    // 5. Se comprueba si alguien se ha quedado sin cartas
                    game.isGameOver = game.playersCards.Any(player => player.Value.Count == 0) || game.cardsStack.Count == 0;
                }

                // 6. Se indica a los player que el juego ha terminado
                SendGameToAllPlayers(game, playersAndSockets);
                Console.WriteLine("Game Finished");
                Console.ReadLine(); // FIXME
            }
            catch (ArgumentNullException ane) { Console.WriteLine("ArgumentNullException : {0}", ane.ToString()); }
            catch (SocketException se) { 
                //Console.WriteLine("SocketException : {0}", se.ToString()); 
                Console.WriteLine("A player disconnected ending game");
                game.playersDisconnected = true;
                game.isGameOver = true;
                SendGameToAllPlayers(game, playersAndSockets);
            }
            catch (Exception e) { Console.WriteLine("Unexpected exception : {0}", e.ToString()); }

            foreach (var playerId in playersAndSockets.Keys) {
                Socket socket = playersAndSockets[playerId];
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private static Game updateGameWithLastPlayerDecision(Game updatedGame, Game previousGame) {

            Cards activeCard = updatedGame.activeCard;

            // Si la carta activa es la misma y se ha robado --> no ha jugado carta, se pasa turno
            if (activeCard == previousGame.activeCard && updatedGame.cardsStack.Count != previousGame.cardsStack.Count) {
                updatedGame.activePlayerIdx = ComputeNextPlayerIndex(updatedGame.direction, updatedGame.activePlayerIdx, updatedGame.numberOfPlayers);
            }
            else if (activeCard != previousGame.activeCard) {
                UInt16 nextPlayerIdx;
                if (activeCard.plus4 || activeCard.plus2) {
                    Console.WriteLine("CHUPATE CARTAS");
                    nextPlayerIdx = ComputeNextPlayerIndex(updatedGame.direction, updatedGame.activePlayerIdx, updatedGame.numberOfPlayers);
                    int cardsToDraw = activeCard.plus4 ? 4 : 2;
                    updatedGame.DrawCardPlayer(nextPlayerIdx, cardsToDraw);
                }
                else if (activeCard.reverse) {
                    Console.WriteLine("REVERSE!!");
                    updatedGame.direction = !updatedGame.direction;
                    nextPlayerIdx = updatedGame.numberOfPlayers == 2 ? updatedGame.activePlayerIdx :
                        ComputeNextPlayerIndex(updatedGame.direction, updatedGame.activePlayerIdx, updatedGame.numberOfPlayers);
                }
                else if (activeCard.skip) {
                    Console.WriteLine("Skipping Player!!");
                    nextPlayerIdx = ComputeNextPlayerIndex(updatedGame.direction, updatedGame.activePlayerIdx, updatedGame.numberOfPlayers);
                    nextPlayerIdx = ComputeNextPlayerIndex(updatedGame.direction, nextPlayerIdx, updatedGame.numberOfPlayers);
                }
                else {
                    nextPlayerIdx = ComputeNextPlayerIndex(updatedGame.direction, updatedGame.activePlayerIdx, updatedGame.numberOfPlayers);
                }
                updatedGame.activePlayerIdx = nextPlayerIdx;
            }
            else {
                // Si la carta activa es la misma y no se ha robado --> problema
                throw new Exception("Invalid operation from client.");
            }
            Console.WriteLine("Active Player: {0}", updatedGame.activePlayerIdx);
            return updatedGame;
        }  

        private static UInt16 ComputeNextPlayerIndex(bool direction, int activePlayerIdx, int numberOfPlayers) {
            return (UInt16)(direction ? (activePlayerIdx + 1) % numberOfPlayers :
                    activePlayerIdx - 1 < 0 ? numberOfPlayers - 1 : activePlayerIdx - 1);
        }

        private static void SendGameToAllPlayers(Game game, Dictionary<int, Socket> playersAndSockets) {
            foreach (Socket socket in playersAndSockets.Where(entry => entry.Value.Connected).Select(x => x.Value)) {
                SendGameToPlayer(socket, game);
            }
        }
        public static int SendGameToPlayer(Socket socket, Game game) {
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
