﻿using Ballz.GameSession.World;
using Ballz.Messages;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ballz.GameSession.Logic
{
    public enum SessionState
    {
        Starting,
        Running,
        Finished,
        Paused,
    }

    public class GameLogic: GameComponent
    {
        new private Ballz Game;

        Dictionary<Player, BallControl> BallControllers = new Dictionary<Player, BallControl>();

        public GameLogic(Ballz game):
            base(game)
        {
            Game = game;
        }

        public void AddPlayer(Player player, Ball ball)
        {
            BallControllers[player] = new BallControl(Game, Game.Match, ball);
        }

        public override void Update(GameTime time)
        {
            var elapsedSeconds = (float)time.ElapsedGameTime.TotalSeconds;

            var worldState = Game.World;

            if (Game.Match.State == SessionState.Running)
            {
                // Update all balls
                foreach (var controller in BallControllers.Values)
                    controller.Update(elapsedSeconds, worldState);
                
                // Check for dead players
                int alivePlayers = 0;
                Player winner = null;
                foreach (var controller in BallControllers.Values)
                {
                    if (controller.IsAlive)
                    {
                        alivePlayers++;
                        winner = controller.Ball.Player;
                    }
                }

                if (alivePlayers < 2)
                {
                    Game.Match.State = SessionState.Finished;
                    Game.Match.Winner = winner;
                }
            }

        }

        public void HandleMessage(object sender, Message message)
        {
            if (message.Kind == Message.MessageType.InputMessage)
            {
                // Pass input messages to the respective ball controllers
                InputMessage msg = (InputMessage)message;
                if(msg.Player != null && BallControllers.ContainsKey(msg.Player))
                {
                    BallControllers[msg.Player].HandleMessage(sender, msg);
                }
            }

            if (message.Kind == Message.MessageType.LogicMessage)
            {
                LogicMessage msg = (LogicMessage)message;

                if (msg.Kind == LogicMessage.MessageType.GameMessage)
                {
                    Enabled = !Enabled;
                    if (Enabled)
                        Game.Match.State = SessionState.Running;
                    else
                    {
                        if (Game.Match.State == SessionState.Finished)
                        {
                            Game.Components.Remove(Game.Match);
                            Game.Match = new GameSession.Session(Game);
                            Game.Components.Add(Game.Match);
                        }
                        else
                        {
                            Game.Match.State = SessionState.Paused;
                        }
                    }
                }
            }
        }
    }
}
