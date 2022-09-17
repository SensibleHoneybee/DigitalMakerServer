using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Models;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using Newtonsoft.Json;

namespace DigitalMakerServer
{
    public class DigitalMakerEngine : IDigitalMakerEngine
    {
        public DigitalMakerEngine(IDynamoDBContext instanceTableDDBContext, IDynamoDBContext shoppingSessionTableDDBContext)
        {
            this.InstanceTableDDBContext = instanceTableDDBContext;
            this.ShoppingSessionTableDDBContext = shoppingSessionTableDDBContext;
        }

        IDynamoDBContext InstanceTableDDBContext { get; }

        IDynamoDBContext ShoppingSessionTableDDBContext { get; }

        public async Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("CreateInstanceRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InstanceName))
            {
                throw new Exception("CreateInstanceRequest.InstanceName must be supplied");
            }

            // Get a unique ID and code for this instance
            ////var secondsSinceY2K = (long)DateTime.UtcNow.Subtract(new DateTime(2000, 1, 1)).TotalSeconds;
            ////var InstanceCode = CreateInstanceCode(secondsSinceY2K);

            var instance = new Instance
            {
                InstanceId = request.InstanceId,
                InstanceName = request.InstanceName,
                ////InstanceCode = instanceCode,
                InstanceState = InstanceState.NotRunning
            };

            logger.LogLine($"Created instance bits. ID: {instance.InstanceId}. Name: {instance.InstanceName}");

            // And create wrapper to store it in DynamoDB
            var instanceStorage = new InstanceStorage
            {
                Id = request.InstanceId,
                ////GameCode = instanceCode,
                CreatedTimestamp = DateTime.UtcNow,
                Content = JsonConvert.SerializeObject(instance)
            };

            logger.LogLine($"Saving instance with id {instanceStorage.Id}");
            await this.InstanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new InstanceCreatedResponse { InstanceId = request.InstanceId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> StartShoppingAsync(StartShoppingRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("StartShoppingRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("StartShoppingRequest.InstanceId must be supplied");
            }

            var shoppingSession = new ShoppingSession
            {
                ShoppingSessionId = request.ShoppingSessionId,
                InstanceId = request.InstanceId
            };

            logger.LogLine($"Created shopping session. ID: {shoppingSession.ShoppingSessionId}. Instance: {shoppingSession.InstanceId}");

            // And create wrapper to store it in DynamoDB
            var shoppingSessionStorage = new ShoppingSessionStorage
            {
                Id = request.InstanceId,
                CreatedTimestamp = DateTime.UtcNow,
                Content = JsonConvert.SerializeObject(shoppingSession)
            };

            logger.LogLine($"Saving shopping session with id {shoppingSession.ShoppingSessionId}");
            await this.ShoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new ShoppingSessionCreatedResponse { ShoppingSessionId = request.ShoppingSessionId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        ////    public async Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger)
        ////    {
        ////        if (string.IsNullOrEmpty(request.ShoppingSessionId))
        ////        {
        ////            throw new Exception("HandleInputReceived.GameCode must be supplied");
        ////        }

        ////        if (string.IsNullOrEmpty(request.InputName))
        ////        {
        ////            throw new Exception("HandleInputReceived.InputName must be supplied");
        ////        }

        ////        // Find the shopping session in question
        ////        var shoppingSessionStorage = await this.ShoppingSessionTableDDBContext.LoadAsync<ShoppingSessionStorage>(request.ShoppingSessionId);

        ////        var shoppingSession = JsonConvert.DeserializeObject<ShoppingSession>(shoppingSessionStorage.Content);

        ////        if (shoppingSession.GameState != GameState.Started)
        ////        {
        ////            throw new Exception($"The game in which you are trying to play a card is not in the started state. State: {game.GameState}.");
        ////        }

        ////        if (!string.Equals(request.Username, game.PlayerToMoveUsername, StringComparison.OrdinalIgnoreCase))
        ////        {
        ////            var playerToMove = game.Players.SingleOrDefault(x => string.Equals(x.Username, game.PlayerToMoveUsername, StringComparison.OrdinalIgnoreCase));
        ////            var playerToMoveNameDisplay = playerToMove != null ? playerToMove.PlayerName : $"<unknown player {game.PlayerToMoveUsername}>";
        ////            throw new Exception($"You may not play a card to the deck, because it is not your turn. It is {playerToMoveNameDisplay}'s turn.");
        ////        }

        ////        var player = game.Players.SingleOrDefault(x => string.Equals(x.Username, request.Username, StringComparison.OrdinalIgnoreCase));
        ////        if (player == null)
        ////        {
        ////            throw new Exception($"The user {request.Username} was not found in the game. Please click \"Join Game\" if you wish to join as a new player.");
        ////        }

        ////        var hand = game.Hands.SingleOrDefault(x => string.Equals(x.PlayerUsername, request.Username, StringComparison.OrdinalIgnoreCase));
        ////        if (hand == null)
        ////        {
        ////            throw new Exception($"The user {request.Username} does not have a hand in the game.");
        ////        }

        ////        var cardIndex = hand.Cards.IndexOf(request.Card);
        ////        if (cardIndex == -1)
        ////        {
        ////            throw new Exception($"The card {request.Card} was not in {request.Username}'s hand.");
        ////        }

        ////        // Check that the move is legal, and apply any extra logic dependent on which card is played,
        ////        // including setting the next player as active, if appropriate.
        ////        var extraMessage = CheckMoveAndSetMoveState(game, request.Card, player);

        ////        // Now perform the card operation itself.
        ////        hand.Cards.RemoveAt(cardIndex);

        ////        var deck = game.Decks.FirstOrDefault(x => x.CanDropFromHand);
        ////        if (deck == null)
        ////        {
        ////            throw new Exception($"There are no decks which accept cards from your hand.");
        ////        }

        ////        deck.Cards.Insert(0, request.Card);

        ////        // Update the connection data in case it's changed
        ////        player.ConnectionId = connectionId;

        ////        // Add a message for everyone that the game started
        ////        var msg = $"{player.PlayerName} played the {request.Card.CardDescription()}.";
        ////        if (!string.IsNullOrEmpty(extraMessage))
        ////        {
        ////            msg += $" {extraMessage}";
        ////        }
        ////        game.Messages.Add(new Message
        ////        {
        ////            Content = msg,
        ////            ToPlayerUsernames = game.Players.Select(x => x.Username).ToList()
        ////        });

        ////        game.Moves.Add(new Move
        ////        {
        ////            PlayerUsername = request.Username,
        ////            Card = request.Card,
        ////            DeckId = deck.Id,
        ////            MoveDirection = MoveDirection.HandToDeck
        ////        });

        ////        // Update the game in the DB
        ////        gameStorage.Content = JsonConvert.SerializeObject(game);

        ////        // And save it back
        ////        logger.LogLine($"Saving game with id {gameStorage.Id}.");
        ////        await this.GameTableDDBContext.SaveAsync<GameStorage>(gameStorage);

        ////        // Send a full game info to all players, as most of the info has changed
        ////        var results = new List<ResponseWithClientId>();
        ////        foreach (var particularPlayer in game.Players)
        ////        {
        ////            var handsByPlayerId = game.Hands.ToDictionary(x => x.PlayerUsername, StringComparer.OrdinalIgnoreCase);
        ////            var thePlayersHand = handsByPlayerId.ContainsKey(particularPlayer.Username) ? handsByPlayerId[particularPlayer.Username] : default(Hand);
        ////            var fullGameResponse = new FullGameResponse
        ////            {
        ////                GameId = game.Id,
        ////                GameCode = game.GameCode,
        ////                GameName = game.GameName,
        ////                GameState = game.GameState,
        ////                PlayerInfo = game.Players.SingleOrDefault(x => string.Equals(x.Username, particularPlayer.Username, StringComparison.OrdinalIgnoreCase)).ToPlayerInfo(handsByPlayerId),
        ////                AllPlayers = game.Players.Select(x => x.ToPlayerInfo(handsByPlayerId)).ToList(),
        ////                Hand = thePlayersHand?.Cards ?? new List<string>(),
        ////                Decks = game.Decks.GetVisibleDecks(),
        ////                MoveCanBeUndone = game.Moves.Any(),
        ////                Messages = game.Messages.Where(x => x.ToPlayerUsernames.Contains(particularPlayer.Username, StringComparer.OrdinalIgnoreCase)).Select(x => x.Content).ToList(),
        ////                PlayerToMoveUsername = game.PlayerToMoveUsername,
        ////                PlayDirection = game.PlayDirection,
        ////                MoveState = game.MoveState,
        ////                WinnerName = game.WinnerName
        ////            };

        ////            results.Add(new ResponseWithClientId(fullGameResponse, particularPlayer.ConnectionId));
        ////        }

        ////        return results;
        ////    }
    }
}
