﻿using System;
using System.IO;
using System.Xml.Serialization;
using Ballz.GameSession.World;
using Ballz.Input;
using Ballz.Logic;
using Ballz.Renderer;
using Microsoft.Xna.Framework;
using Ballz.Menu;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Ballz
{
    #region Using Statements

    

    #endregion

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class Ballz : Game
    {
        //SpriteBatch spriteBatch;
        private static Ballz _instance;

        public GraphicsDeviceManager Graphics { get; set; }

        public LogicControl Logic { get; set; }

        public World World { get; set; }

		public Network.Network Network { get; set; }

        public GameSession.Session Match;

        public Settings.ProgrammSettings GameSettings;

        public Composite MainMenu;

        private Ballz()
        {
            Graphics = new GraphicsDeviceManager(this);
            initSettings();
            Content.RootDirectory = "Content";
            Graphics.IsFullScreen = GameSettings.Fullscreen.Value;
            Graphics.PreferredBackBufferHeight = GameSettings.ScreenResolution.Value.Height;
            Graphics.PreferredBackBufferWidth = GameSettings.ScreenResolution.Value.Width;

            // create the Game Components
            var menuRendering = new MenuRenderer(this, DefaultMenu());
            //var physics = new PhysicsControl(this);
            var input = new InputTranslator(this);
            Network = new Network.Network(this);

            Components.Add(input);
            //Components.Add(physics);
			Components.Add(Network);
            Components.Add(menuRendering);
            Components.Add(new PerformanceRenderer(this));

            MainMenu = DefaultMenu();
            Logic = new LogicControl(this);

            Services.AddService(Logic);
            Services.AddService(input);

            Match = new GameSession.Session(this);
            Components.Add(Match);

            //add eventhandlers to events
            input.Input += Logic.HandleInputMessage;
            //input.Input += physics.HandleMessage;
			input.Input += Network.HandleMessage;

            //Logic.Message += physics.HandleMessage;
			Logic.Message += Network.HandleMessage;
            //Logic.Message += gameRendering.HandleMessage;
            Logic.Message += menuRendering.HandleMessage;
        }

        public static Ballz The()
        {
            if (_instance != null)
                return _instance;
            _instance = new Ballz();
            return _instance;
        }

        /// <summary>
        /// Inits the settings, by deserializing an existing Settings file.
        /// If no setingsFile is found, the default Settings are provided and serialized.
        /// </summary>
        private void initSettings()
        {
            try
            {
                FileStream stream = new FileStream("Settings.xml", FileMode.Open);
                //found an existing Settings file try to deserialize it
                try
                {
                    loadSettings(stream);
                    sanitizeSettings();
                }
                catch(Exception )    //loading failed so throw away the old xml
                {
                    stream.Close();
                    File.Delete("Settings.xml");
                    FileStream theStream = new FileStream("Settings.xml", FileMode.OpenOrCreate);
                    GameSettings = new Settings.ProgrammSettings();
                    storeSettings(theStream);
                }
                stream.Close();
            }
            catch(Exception )
            {
                //no settings file was found, create one.
                FileStream theStream = new FileStream("Settings.xml", FileMode.OpenOrCreate);
                GameSettings = new Settings.ProgrammSettings();
                storeSettings(theStream);
            }
        }

        private void sanitizeSettings()
        {
            if (!getResolutions().Contains(GameSettings.ScreenResolution.Value))
                throw new Exception("Settings.xml holds bogus values");
        }

        private List<Settings.Resolution> getResolutions()
        {
            List<Settings.Resolution> result = new List<Settings.Resolution>();
            DisplayModeCollection dmc = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes;
            foreach (DisplayMode dm in dmc)
            {
                Settings.Resolution resolution = new Settings.Resolution(dm.Width, dm.Height);
                if (!result.Contains(resolution))
                    result.Add(resolution);
            }
            return result;
        }

        private void loadSettings(FileStream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings.ProgrammSettings));
            GameSettings = (Settings.ProgrammSettings)serializer.Deserialize(stream);
        }

        private void storeSettings(FileStream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings.ProgrammSettings));
            serializer.Serialize(stream, GameSettings);
        }

        private Composite DefaultMenu()
		{
			// options menu
			var optionsMenu = new Composite ("Options", true);
			//optionsMenu.AddItem(new Label("Not Implemented", false));
			optionsMenu.AddItem (new CheckBox ("FullScreen: ", GameSettings.Fullscreen));
			optionsMenu.AddItem (new Choice<Settings.Resolution> ("Resolution: ", GameSettings.ScreenResolution, getResolutions ()));
			Label apply = new Label ("Apply", true);
			apply.OnSelect += () => {
				File.Delete ("Settings.xml");
				FileStream stream = new FileStream ("Settings.xml", FileMode.OpenOrCreate);
				storeSettings (stream);
				stream.Close ();
				Graphics.IsFullScreen = GameSettings.Fullscreen.Value;
				Graphics.PreferredBackBufferWidth = GameSettings.ScreenResolution.Value.Width;
				Graphics.PreferredBackBufferHeight = GameSettings.ScreenResolution.Value.Height;
				Graphics.ApplyChanges ();
			};
			optionsMenu.AddItem (apply);

			// multiplayer menu
			var networkMenu = new Composite ("Multiplayer", true);
			// - connect to server
			var networkConnectToMenu = new Composite ("Connect to", true);
			networkConnectToMenu.AddItem (new InputBox ("Host Name: ", true)); //TODO: process input
			var networkConnectToLabel = new Label ("Connect", true);
			networkConnectToLabel.OnSelect += () => Network.ConnectToServer ("localhost", 13337); //TODO: use input
			networkConnectToMenu.AddItem (networkConnectToLabel);
            // - start server
            var networkServerMenu = new Label("Start server", true);
			networkServerMenu.OnSelect += () => 
			{
				Network.StartServer (13337); //TODO: port input
				Logic.startGame ();
			};
            // - add items
            networkMenu.AddItem(networkConnectToMenu);
            networkMenu.AddItem(networkServerMenu);
            networkMenu.AddItem(new Back());

            // main menu
            var mainMenu = new Composite("Main Menu");

            var play = new Label("Play",true);
            play.OnSelect += () =>
                {
                    Logic.startGame();
                };

            mainMenu.AddItem(play);
            mainMenu.AddItem(optionsMenu);
            mainMenu.AddItem(networkMenu);

            var quit = new Label("Quit", true);
            quit.OnSelect += Exit;
            mainMenu.AddItem(quit);

            return mainMenu;
        }

        /// <summary>
        ///     Allows the game to perform any initialization it needs to before starting to run.
        ///     This is where it can query for any required services and load any non-graphic
        ///     related content.  Calling base.Initialize will enumerate through any components
        ///     and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        ///     LoadContent will be called once per game and is the place to load
        ///     all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Content.RootDirectory = "Content";

            //TODO: use this.Content to load your game content here
        }

        /// <summary>
        ///     Allows the game to run logic such as updating the world,
        ///     checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <summary>
        ///     This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}