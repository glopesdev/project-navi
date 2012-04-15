﻿using System;
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
using ProjectNavi.Graphics;
using ProjectNavi.Bonsai.Aruco;
using System.Reactive.Linq;

namespace ProjectNavi
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class NaviControllerGame : Microsoft.Xna.Framework.Game
    {
        float zoom = 1;
        const float ZoomFactor = 0.01f;

        ReactiveWorkflow vision;
        IDisposable visionLoaded;
        IDisposable magabot;
        GraphicsDeviceManager graphics;
        SpriteRenderer renderer;
        SpriteRenderer backRenderer;
        PrimitiveBatchRenderer primitiveRenderer;
        TaskScheduler scheduler;
        ICommunicationManager communication;

        public NaviControllerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            renderer = new SpriteRenderer(this);
            backRenderer = new SpriteRenderer(this);
            primitiveRenderer = new PrimitiveBatchRenderer(this);
            renderer.PixelsPerWorldUnit = backRenderer.PixelsPerWorldUnit = 100; //100 pixels/meter
            scheduler = new TaskScheduler(this);
            Components.Add(scheduler);
            Components.Add(backRenderer);
            Components.Add(renderer);
            Components.Add(primitiveRenderer);
            Content.RootDirectory = "Content";
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 30.0);
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
            primitiveRenderer.Projection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1);

            // Create the Bonsai vision workflow using a Kinect or a Webcam
            var workflowName = Microsoft.Kinect.KinectSensor.KinectSensors.Count > 0 ? "KinectVision.bonsai" : "WebcamVision.bonsai";
            using (var reader = XmlReader.Create(workflowName))
            {
                var serializer = new XmlSerializer(typeof(WorkflowBuilder));
                var workflowBuilder = (WorkflowBuilder)serializer.Deserialize(reader);
                vision = workflowBuilder.Workflow.Build();
                visionLoaded = vision.Load();

                IObservable<IplImage> colorStream;
                IObservable<KinectFrame> kinectStream;
                IObservable<MarkerFrame> markerStream;
                var connections = vision.Connections.ToArray();
                if (connections.Length > 2)
                {
                    colorStream = Expression.Lambda<Func<IObservable<IplImage>>>(connections[1]).Compile()();
                    kinectStream = Expression.Lambda<Func<IObservable<KinectFrame>>>(connections[0]).Compile()();
                    markerStream = Expression.Lambda<Func<IObservable<MarkerFrame>>>(connections[2]).Compile()();
                }
                else
                {
                    kinectStream = Observable.Never<KinectFrame>();
                    colorStream = Expression.Lambda<Func<IObservable<IplImage>>>(connections[0]).Compile()();
                    markerStream = Expression.Lambda<Func<IObservable<MarkerFrame>>>(connections[1]).Compile()();
                }

                Services.AddService(typeof(IObservable<IplImage>), colorStream);
                Services.AddService(typeof(IObservable<KinectFrame>), kinectStream);
                Services.AddService(typeof(IObservable<MarkerFrame>), markerStream);
            }

            // TODO: use this.Content to load your game content here
            //Grid.Create(this, renderer);
            magabot = Magabot.Create(this, renderer, backRenderer, primitiveRenderer, scheduler, communication);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            magabot.Dispose();
            visionLoaded.Dispose();
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
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Q)) zoom += ZoomFactor;
            if (keyboard.IsKeyDown(Keys.A)) zoom -= ZoomFactor;

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
            renderer.ViewMatrix = backRenderer.ViewMatrix = Matrix.CreateScale(zoom);
            primitiveRenderer.View = Matrix.CreateScale(zoom);

            base.Draw(gameTime);
        }
    }
}
