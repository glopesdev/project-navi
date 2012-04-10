using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Cyberiad.Graphics;
using ProjectNavi.Entities;
using ProjectNavi.Hardware;
using Cyberiad;
using Bonsai;
using Bonsai.Expressions;
using System.Xml;
using System.Xml.Serialization;
using System.Linq.Expressions;
using OpenCV.Net;
using Microsoft.Kinect;
using ProjectNavi.Bonsai.Kinect;
using Aruco.Net;
using ProjectNavi.SkypeController;

namespace ProjectNavi
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class NaviControllerGame : Microsoft.Xna.Framework.Game
    {
        ReactiveWorkflow vision;
        IDisposable visionLoaded;
        GraphicsDeviceManager graphics;
        SpriteRenderer renderer;
        TaskScheduler scheduler;
        ICommunicationManager communication;

        public NaviControllerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            renderer = new SpriteRenderer(this);
            renderer.PixelsPerWorldUnit = 100; //100 pixels/meter
            scheduler = new TaskScheduler(this);
            Components.Add(scheduler);
            Components.Add(renderer);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            communication = new CommunicationManagerSerial("COM30", 9600);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            //using (var reader = XmlReader.Create("Vision.bonsai"))
            //{
            //    var serializer = new XmlSerializer(typeof(WorkflowBuilder));
            //    var workflowBuilder = (WorkflowBuilder)serializer.Deserialize(reader);
            //    vision = workflowBuilder.Workflow.Build();
            
                //visionLoaded = vision.Load();

            //    var connections = vision.Connections.ToArray();
            //    var kinectStream = Expression.Lambda<Func<IObservable<KinectFrame>>>(connections[0]).Compile()();

            //    kinectStream.Subscribe(frame =>
            //    {
            //        Console.WriteLine(frame);
            //    });
            //}

            // TODO: use this.Content to load your game content here
            Grid.Create(this, renderer);
            Magabot.Create(this, renderer, scheduler, communication, vision);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
