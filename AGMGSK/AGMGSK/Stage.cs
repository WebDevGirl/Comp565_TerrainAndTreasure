﻿/* 
 * -------------------------------------------
 * HACKER TERRAIN AND TREASURE GAME v.01
 * -------------------------------------------
 * Class: Comp 565 - Spring 2014
 * Author: Ursula Messick <ursula.messick.95@my.csun.edu>
 * Author: Billy Morales <billy.morales.520@my.csun.edu>
 * 
*/

/*  
  Copyright (C) 2014 G. Michael Barnes
 
  The file Stage.cs is part of AGMGSKv5 a port of AGXNASKv4 from
  XNA 4 refresh to MonoGames 3.0.6.

  AGMGSKv5 is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if ! __XNA4__  // when __XNA4__ == true build for MonoGames
   using Microsoft.Xna.Framework.Storage; 
#endif
using Microsoft.Xna.Framework.GamerServices;
#endregion

namespace AGMGSK
{

    /// <summary>
    /// Stage.cs  is the environment / framework for AGXNASK.
    /// 
    /// Stage declares and initializes the common devices, Inspector pane,
    /// and user input options.  Stage attempts to "hide" the infrastructure of AGMGSK
    /// for the developer.  It's subclass Stage is where the program specific aspects are
    /// declared and created.
    /// 
    /// AGXNASK is a starter kit for Comp 565 assignments using XNA Game Studio 4.0
    /// and Visual Studio 2010.
    /// 
    /// See AGXNASKv4-doc.pdf file for class diagram and usage information. 
    /// 
    /// 2/2/2014   last  updated
    /// </summary>
    public class Stage : Game
    {
        // Range is the length of the cubic volume of stage's terrain.
        // Each dimension (x, y, z) of the terrain is from 0 .. range (512)
        // The terrain will be centered about the origin when created.
        // Note some recursive terrain height generation algorithms (ie SquareDiamond)
        // work best with range = (2^n) - 1 values (513 = (2^9) -1).
        protected const int range = 512;
        protected const int spacing = 150;  // x and z spaces between vertices in the terrain
        protected const int terrainSize = range * spacing;
        // Graphics device
        protected GraphicsDeviceManager graphics;
        protected GraphicsDevice display;    // graphics context
        protected BasicEffect effect;        // default effects (shaders)
        protected SpriteBatch spriteBatch;   // for Trace's displayStrings
        protected BlendState blending, notBlending;
        // stage Models
        protected Model boundingSphere3D;    // a  bounding sphere model
        protected Model wayPoint3D;          // a way point marker -- for paths.
        protected bool drawBoundingSpheres = false;
        protected bool fog = false;
        protected bool fixedStepRendering = true;     // 60 updates / second
        // Viewports and matrix for split screen display w/ Inspector.cs
        protected Viewport defaultViewport;
        protected Viewport inspectorViewport, sceneViewport;     // top and bottom "windows"
        protected Matrix sceneProjection, inspectorProjection;
        // variables required for use with Inspector
        protected const int InfoPaneSize = 5;   // number of lines / info display pane
        protected const int InfoDisplayStrings = 20;  // number of total display strings
        protected Inspector inspector;
        protected SpriteFont inspectorFont;
        // Projection values
        protected Matrix projection;
        protected float fov = (float)Math.PI / 4;
        protected float hither = 5.0f, yon = terrainSize / 5.0f, farYon = terrainSize * 1.3f;
        protected float fogStart = 4000;
        protected float fogEnd = 10000;
        protected bool yonFlag = true;
        // User event state
        protected GamePadState oldGamePadState;
        protected KeyboardState oldKeyboardState;
        // Lights
        protected Vector3 lightDirection, ambientColor, diffuseColor;
        // Cameras
        protected List<Camera> camera = new List<Camera>();  // collection of cameras
        protected Camera currentCamera, topDownCamera;
        protected int cameraIndex = 0;
        // Required entities -- all AGXNASK programs have a Player and Terrain
        protected Player player = null;
        protected NPAgent npAgent = null;
        protected Terrain terrain = null;
        protected List<Object3D> collidable = null;
        // Screen display information variables
        protected double fpsSecond;
        protected int draws, updates;
        // Treasures to tag
        protected List<Treasure> treasures = new List<Treasure>();
        public  Graph graph;
        // Pack object to test flocking
        protected Pack pack;
        /// <summary>
        /// Set the Scene.
        /// The Scene contains all "application-specific" content that is to added to
        /// the "stage".
        /// LoadScene() is called at the end of Stage's LoadContent().
        /// </summary>
        private void LoadScene()
        {
            terrain = new Terrain(this, "terrain", "heightTexture", "colorTexture");
            Components.Add(terrain);
            // Load Avatar mesh objects, Avatar meshes do not have textures
            player = new Player(this, "Chaser",
               new Vector3(510 * spacing, terrain.surfaceHeight(510, 507), 507 * spacing),
               new Vector3(0, 1, 0), 0.80f, "redAvatarV3");  // face looking diagonally across stage
            player.IsCollidable = true;
            Components.Add(player);
            npAgent = new NPAgent(this, "Evader",
               new Vector3(400 * spacing, terrain.surfaceHeight(400, 400), 400 * spacing),
               new Vector3(0, 1, 0), 0.0f, "magentaAvatarV3");  // facing +Z
            npAgent.IsCollidable = true;
            Components.Add(npAgent);
        }

        public Stage()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.SynchronizeWithVerticalRetrace = false;  // allow faster FPS
            // Directional light values
            lightDirection = Vector3.Normalize(new Vector3(-1.0f, -1.0f, -1.0f));
            ambientColor = new Vector3(0.4f, 0.4f, 0.4f);
            diffuseColor = new Vector3(0.2f, 0.2f, 0.2f);
            IsMouseVisible = true;  // make mouse cursor visible
            // information display variables
            fpsSecond = 0.0;
            draws = updates = 0;
        }

        // Properties

        public Vector3 AmbientLight
        {
            get { return ambientColor; }
        }

        public Model BoundingSphere3D
        {
            get { return boundingSphere3D; }
        }

        public List<Object3D> Collidable
        {
            get { return collidable; }
        }

        public Vector3 DiffuseLight
        {
            get { return diffuseColor; }
        }

        public GraphicsDevice Display
        {
            get { return display; }
        }

        public bool DrawBoundingSpheres
        {
            get { return drawBoundingSpheres; }
            set
            {
                drawBoundingSpheres = value;
                inspector.setInfo(8, String.Format("Draw bounding spheres = {0}", drawBoundingSpheres));
            }
        }

        public float FarYon
        {
            get { return farYon; }
        }

        public bool FixedStepRendering
        {
            get { return fixedStepRendering; }
            set
            {
                fixedStepRendering = value;
                IsFixedTimeStep = fixedStepRendering;
            }
        }

        public bool Fog
        {
            get { return fog; }
            set { fog = value; }
        }

        public float FogStart
        {
            get { return fogStart; }
        }

        public float FogEnd
        {
            get { return fogEnd; }
        }

        public Vector3 LightDirection
        {
            get { return lightDirection; }
        }

        public Matrix Projection
        {
            get { return projection; }
        }

        public int Range
        {
            get { return range; }
        }

        public BasicEffect SceneEffect
        {
            get { return effect; }
        }

        public int Spacing
        {
            get { return spacing; }
        }

        public Terrain Terrain
        {
            get { return terrain; }
        }

        public int TerrainSize
        {
            get { return terrainSize; }
        }

        public Matrix View
        {
            get { return currentCamera.ViewMatrix; }
        }

        public Model WayPoint3D
        {
            get { return wayPoint3D; }
        }

        public bool YonFlag
        {
            get { return yonFlag; }
            set
            {
                yonFlag = value;
                if (yonFlag) setProjection(yon);
                else setProjection(farYon);
            }
        }

        public List<Treasure> Treasures
        {
            get { return treasures; }
        }


        // Methods

        public bool isCollidable(Object3D obj3d)
        {
            if (collidable.Contains(obj3d)) return true;
            else return false;
        }

        /// <summary>
        /// Make sure that aMovableModel3D does not move off the terain.
        /// Called from MovableModel3D.Update()
        /// The Y dimension is not constrained -- code commented out.
        /// </summary>
        /// <param name="aName"> </param>
        /// <param name="newLocation"></param>
        /// <returns>true iff newLocation is within range</returns>
        public bool withinRange(String aName, Vector3 newLocation)
        {
            if (newLocation.X < spacing || newLocation.X > (terrainSize - 2 * spacing) ||
               newLocation.Z < spacing || newLocation.Z > (terrainSize - 2 * spacing))
            {
                inspector.setInfo(14, String.Format("{0} can't move out of range", aName));
                return false;
            }
            else
            {
                inspector.setInfo(14, " ");
                return true;
            }
        }

        public void addCamera(Camera aCamera)
        {
            camera.Add(aCamera);
            cameraIndex++;
        }

        public void setInfo(int index, string info)
        {
            inspector.setInfo(index, info);
        }

        protected void setProjection(float yonValue)
        {
            projection = Matrix.CreatePerspectiveFieldOfView(fov,
            graphics.GraphicsDevice.Viewport.AspectRatio, hither, yonValue);
        }

        /// <summary>
        /// Changing camera view for Agents will always set YonFlag false
        /// and provide a clipped view.
        /// </summary>
        public void nextCamera()
        {
            cameraIndex = (cameraIndex + 1) % camera.Count;
            currentCamera = camera[cameraIndex];
            // set the appropriate projection matrix
            YonFlag = false;
            setProjection(farYon);
        }

        /// <summary>
        /// Get the height of the surface containing stage coordinates (x, z)
        /// </summary>
        public float surfaceHeight(float x, float z)
        {
            return terrain.surfaceHeight((int)x / spacing, (int)z / spacing);
        }

        public void setSurfaceHeight(Object3D anObject3D)
        {
            float terrainHeight = terrain.surfaceHeight((anObject3D.Translation.X / spacing),
                                                         (anObject3D.Translation.Z / spacing));
            anObject3D.Translation = new Vector3(anObject3D.Translation.X,
                                                  terrainHeight, anObject3D.Translation.Z);
        }

        public void setBlendingState(bool state)
        {
            if (state) display.BlendState = blending;
            else display.BlendState = notBlending;
        }

        // Overridden Game class methods. 

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }


        /// <summary>
        /// Set GraphicDevice display and rendering BasicEffect effect.  
        /// Create SpriteBatch, font, and font positions.
        /// Creates the traceViewport to display information and the sceneViewport
        /// to render the environment.
        /// Create and add all DrawableGameComponents and Cameras.
        /// First, add all required contest:  Inspector, Cameras, Terrain, Agents
        /// Second, add all optional (scene specific) content
        /// </summary>
        protected override void LoadContent()
        {
            display = graphics.GraphicsDevice;
            effect = new BasicEffect(display);
            // Set up Inspector display
            spriteBatch = new SpriteBatch(display);      // Create a new SpriteBatch
            inspectorFont = Content.Load<SpriteFont>("CourierNew");    // Windows XNA || MonoGames
            // inspectorFont = Content.Load<SpriteFont>("Consolas");       // Ubuntu MonoGames
            // viewports
            defaultViewport = GraphicsDevice.Viewport;
            inspectorViewport = defaultViewport;
            sceneViewport = defaultViewport;
            inspectorViewport.Height = InfoPaneSize * inspectorFont.LineSpacing;
            inspectorProjection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 4.0f,
               inspectorViewport.Width / inspectorViewport.Height, 1.0f, 200.0f);
            sceneViewport.Height = defaultViewport.Height - inspectorViewport.Height;
            sceneViewport.Y = inspectorViewport.Height;
            sceneProjection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 4.0f,
               sceneViewport.Width / sceneViewport.Height, 1.0f, 1000.0f);
            // create Inspector display
            Texture2D inspectorBackground = Content.Load<Texture2D>("inspectorBackground");
            inspector = new Inspector(display, inspectorViewport, inspectorFont, Color.Black, inspectorBackground);
            // create information display strings
            // help strings
            inspector.setInfo(0, "AGMGSKv5 -- Academic Graphics MonoGames/XNA Starter Kit for CSUN Comp 565 assignments.");
            inspector.setInfo(1, "Press keyboard for input (not case sensitive 'H'  'h')");
            inspector.setInfo(2, "Inspector toggles:  'H' help or info   'M'  matrix or info   'I'  displays next info pane.");
            inspector.setInfo(3, "Arrow keys move the player in, out, left, or right.  'R' resets player to initial orientation.");
            inspector.setInfo(4, "Stage toggles:  'B' bounding spheres, 'C' cameras, 'F' fog, 'T' updates, 'Y' yon");
            inspector.setInfo(5, "Press 'P' to change packing level of flock");
            // initialize empty info strings
            for (int i = 5; i < 20; i++) inspector.setInfo(i, "  ");
            inspector.setInfo(5, "matrics info pane, initially empty");
            inspector.setInfo(10, "first info pane, initially empty");
            inspector.setInfo(15, "second info pane, initially empty");
            // set blending for bounding sphere drawing
            blending = new BlendState();
            blending.ColorSourceBlend = Blend.SourceAlpha;
            blending.ColorDestinationBlend = Blend.InverseSourceAlpha;
            blending.ColorBlendFunction = BlendFunction.Add;
            notBlending = new BlendState();
            notBlending = display.BlendState;
            // Create and add stage components
            // You must have a TopDownCamera, BoundingSphere3D, Terrain, and Agents (player, npAgent) in your stage!
            // Place objects at a position, provide rotation axis and rotation radians.
            // All location vectors are specified relative to the center of the stage.
            // Create a top-down "Whole stage" camera view, make it first camera in collection.
            topDownCamera = new Camera(this, Camera.CameraEnum.TopDownCamera);
            camera.Add(topDownCamera);
            boundingSphere3D = Content.Load<Model>("boundingSphereV3");
            wayPoint3D = Content.Load<Model>("100x50x100Marker");
            // Create required entities:  
            collidable = new List<Object3D>();
            terrain = new Terrain(this, "terrain", "heightTexture", "colorTexture");
            Components.Add(terrain);
            // Load Agent mesh objects, meshes do not have textures
            player = new Player(this, "Chaser",
               new Vector3(510 * spacing, terrain.surfaceHeight(510, 507), 507 * spacing),
               new Vector3(0, 1, 0), 0.78f, "redAvatarV3");  // face looking diagonally across stage
            player.IsCollidable = true; // test collisions for player
            Components.Add(player);
            npAgent = new NPAgent(this, "Evader",
               new Vector3(490 * spacing, terrain.surfaceHeight(490, 475), 475 * spacing),
               new Vector3(0, 1, 0), 0.0f, "magentaAvatarV3");  // facing +Z
            npAgent.IsCollidable = false;
            // npAgent does not test for collisions
            Components.Add(npAgent);
            // ----------- CREATE OPTIONAL CONTENT HERE -----------------------
            // Load all optional content.  Content not requried by AGXNASK
            // create a temple
            Model3D m3d = new Model3D(this, "temple", "templeV3");
            m3d.IsCollidable = true;  // must be set before addObject(...) and Model3D doesn't set it
            m3d.addObject(new Vector3(200 * spacing, terrain.surfaceHeight(200, 200), 200 * spacing),
               new Vector3(0, 1, 0), 0.79f);
            Components.Add(m3d);

            // Add Treasures To Map
            Treasure tr = new Treasure(this, "t1", "hash");
            tr.addObject(new Vector3(477 * spacing, terrain.surfaceHeight(477, 290) + 110, 290 * spacing),
                new Vector3(0, 1, 0), 0.79f);
            Components.Add(tr);
            treasures.Add(tr);
         
            // inside the wall, rotated to fit
            tr = new Treasure(this, "t2", "hash");
            tr.addObject(new Vector3(446 * spacing, terrain.surfaceHeight(446, 450) + 110, 450 * spacing),
                new Vector3(0, 1, 0), 1.45f);
           Components.Add(tr);
           treasures.Add(tr);


           // Before Gibson
           tr = new Treasure(this, "t5", "hash");
           tr.addObject(new Vector3(470 * spacing, terrain.surfaceHeight(470, 144) + 210, 144 * spacing),
               new Vector3(0, 1, 0), 0.79f);
           Components.Add(tr);
           treasures.Add(tr);

            // Inside Gibson
            tr = new Treasure(this, "t3", "hash");
            tr.addObject(new Vector3(507 * spacing, terrain.surfaceHeight(507, 425) + 110, 425 * spacing),
                new Vector3(0, 1, 0), 0.79f);
            Components.Add(tr);
            treasures.Add(tr);

            // On Hill
            tr = new Treasure(this, "t4", "hash");
            tr.addObject(new Vector3(470 * spacing, terrain.surfaceHeight(470, 144) + 210, 144 * spacing),
                new Vector3(0, 1, 0), 0.79f);
            Components.Add(tr);
            treasures.Add(tr);

            /* Add a grid of pillars */ 
            /* Not collidable for now because it has a HUGE bounding sphere */
            Model3D pillar;
            int offset = 12;           
            for (int i = 1; i < 12; i++)
            {
                for (int j = 1; j < 12; j++)
                {
                   /* Set Coordinators */
                    int x = (353 + (offset * i)) * spacing;
                    int z = (269 + (offset * j)) * spacing;
                    float y = terrain.surfaceHeight(x, z);

                    /* Add Small pillar inside big pillar to deal with collisions for now */ 
                    pillar = new Model3D(this, "p_sm" + i, "pillar_sm");
                    pillar.IsCollidable = true;
                    pillar.addObject(new Vector3(x + 570, y + 50, z-80), new Vector3(0, 1, 0), 0.79f);
                    Components.Add(pillar);
                    
                    /* Gibson Pillar */ 
                    pillar = new Model3D(this, "p" + i, "pillar");
                    pillar.addObject(new Vector3(x, y + 300, z),  new Vector3(0, 1, 0), 0.79f);
                    Components.Add(pillar);
                }
            }

            // create walls for obstacle avoidance or path finding algorithms
            Wall wall = new Wall(this, "wall", "100x100x100Brick");
            Components.Add(wall);


            graph = new Graph(this);
            //Components.Add( graph.GetAStarPath(new Vector3(510 * spacing, terrain.surfaceHeight(510, 507), 507 * spacing), new Vector3(410 * spacing, terrain.surfaceHeight(410, 414) + 110, 414 * spacing)));

            Random random = new Random();  // used for pack and cloud
            // create a Pack of dogs with Player as the leader
            pack = new Pack(this, "dog", "dogV3", player.AgentObject);
            Components.Add(pack);
            for (int x = -9; x < 10; x += 6)
                for (int z = -3; z < 4; z += 6)
                {
                    float scale = (float)(0.5 + random.NextDouble());
                    float xPos = (480 + x) * spacing;  // position of pack
                    float zPos = (480 + z) * spacing;
                    pack.addObject(
                       new Vector3(xPos, terrain.surfaceHeight((int)xPos / spacing, (int)zPos / spacing), zPos),
                       new Vector3(0, 1, 0), 0.0f,
                       new Vector3(scale, scale, scale));
                }
            // create some clouds
            Cloud cloud = new Cloud(this, "cloud", "cloudV3");
            Components.Add(cloud);
            // add 9 cloud instances
            for (int x = range / 4; x < range; x += (range / 4))
                for (int z = range / 4; z < range; z += (range / 4))
                    cloud.addObject(
                       new Vector3(x * spacing, terrain.surfaceHeight(x, z) + 2000, z * spacing),
                       new Vector3(0, 1, 0), 0.0f,
                       new Vector3(random.Next(3) + 1, random.Next(3) + 1, random.Next(3) + 1));
            // ----------- end of optional content
            // Set initial camera and projection matrix
            nextCamera();  // select the first camera
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
        /// Uses an Inspector to display update and display information to player.
        /// All user input that affects rendering of the stage is processed either
        /// from the gamepad or keyboard.
        /// See Player.Update(...) for handling of user events that affect the player.
        /// The current camera's place is updated after all other GameComponents have 
        /// been updated.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // set info pane values
            fpsSecond += gameTime.ElapsedGameTime.TotalSeconds;
            updates++;
            TimeSpan ts = gameTime.TotalGameTime;
            if (fpsSecond >= 1.0)
            {
                inspector.setInfo(10,
                   String.Format("{0} camera    Game time {1:D2}::{2:D2}::{3:D2}    {4:D} Updates/Seconds {5:D} Draws/Seconds",
                      currentCamera.Name, ts.Hours, ts.Minutes, ts.Seconds, updates.ToString(), draws.ToString()));
                draws = updates = 0;
                fpsSecond = 0.0;
                inspector.setInfo(11,
                   string.Format("Player:   Treasures Tagged   " + player.TreasuresTagged + "   Location ({0,5:f0},{1,3:f0},{2,5:f0})  Looking at ({3,5:f2},{4,5:f2},{5,5:f2})",
                   player.AgentObject.Translation.X, player.AgentObject.Translation.Y, player.AgentObject.Translation.Z,
                   player.AgentObject.Forward.X, player.AgentObject.Forward.Y, player.AgentObject.Forward.Z));
                inspector.setInfo(12,
                   string.Format("npAgent:  Treasures Tagged   " + npAgent.TreasuresTagged + "   Location ({0,5:f0},{1,3:f0},{2,5:f0})  Looking at ({3,5:f2},{4,5:f2},{5,5:f2})",
                   npAgent.AgentObject.Translation.X, npAgent.AgentObject.Translation.Y, npAgent.AgentObject.Translation.Z,
                   npAgent.AgentObject.Forward.X, npAgent.AgentObject.Forward.Y, npAgent.AgentObject.Forward.Z));
                inspector.setInfo(13, string.Format("Flocking Level:    " + Pack.getLevelValue() + "%"));
                inspector.setMatrices("player", "npAgent", player.AgentObject.Orientation, npAgent.AgentObject.Orientation);
            }
            // Process user keyboard events that relate to the render state of the the stage
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape)) Exit();
            else if (keyboardState.IsKeyDown(Keys.B) && !oldKeyboardState.IsKeyDown(Keys.B))
                DrawBoundingSpheres = !DrawBoundingSpheres;
            else if (keyboardState.IsKeyDown(Keys.C) && !oldKeyboardState.IsKeyDown(Keys.C))
                nextCamera();
            else if (keyboardState.IsKeyDown(Keys.F) && !oldKeyboardState.IsKeyDown(Keys.F))
                Fog = !Fog;
            // key event handlers needed for Inspector
            // set help display on
            else if (keyboardState.IsKeyDown(Keys.H) && !oldKeyboardState.IsKeyDown(Keys.H))
            {
                inspector.ShowHelp = !inspector.ShowHelp;
                inspector.ShowMatrices = false;
            }
            // set info display on
            else if (keyboardState.IsKeyDown(Keys.I) && !oldKeyboardState.IsKeyDown(Keys.I))
                inspector.showInfo();
            // set miscellaneous display on
            else if (keyboardState.IsKeyDown(Keys.M) && !oldKeyboardState.IsKeyDown(Keys.M))
            {
                inspector.ShowMatrices = !inspector.ShowMatrices;
                inspector.ShowHelp = false;
            }
            // switch npAgent's navigation mode
            else if (keyboardState.IsKeyDown(Keys.N) && !oldKeyboardState.IsKeyDown(Keys.N))
            {
                npAgent.switchMode();
            }
            // toggle update speed between FixedStep and ! FixedStep
            else if (keyboardState.IsKeyDown(Keys.T) && !oldKeyboardState.IsKeyDown(Keys.T))
                FixedStepRendering = !FixedStepRendering;
            else if (keyboardState.IsKeyDown(Keys.Y) && !oldKeyboardState.IsKeyDown(Keys.Y))
                YonFlag = !YonFlag;  // toggle Yon clipping value.

            else if (keyboardState.IsKeyDown(Keys.P) && !oldKeyboardState.IsKeyDown(Keys.P))
            {
                pack.changeFlockLevel();
            }
            oldKeyboardState = keyboardState;    // Update saved state.
            checkForTreasure();
            base.Update(gameTime);  // update all GameComponents and DrawableGameComponents
            currentCamera.updateViewMatrix();
        }

        /// <summary>
        /// Draws information in the display viewport.
        /// Resets the GraphicsDevice's context and makes the sceneViewport active.
        /// Has Game invoke all DrawableGameComponents Draw(GameTime).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            draws++;
            display.Viewport = defaultViewport; //sceneViewport;
            display.Clear(Color.Black);
            // Draw into inspectorViewport
            display.Viewport = inspectorViewport;
            spriteBatch.Begin();
            inspector.Draw(spriteBatch);
            spriteBatch.End();
            // need to restore state changed by spriteBatch
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            // draw objects in stage 
            display.Viewport = sceneViewport;
            display.RasterizerState = RasterizerState.CullNone;
            base.Draw(gameTime);  // draw all GameComponents and DrawableGameComponents
        }

        // Checks whether an Agent has come into contact with a treasure
        // If it has, the treasure is tagged 
        // and the tagging Agent's TreasuresTagged is incremented.
        public void checkForTreasure()
        {
            for (int i = 0; i < treasures.Count; i++)
            {
                Agent tagger = null;
                if (!treasures[i].IsTagged && player.withinRange(treasures[i], 750))
                {
                    tagger = player;
                }
                else if (!treasures[i].IsTagged && npAgent.withinRange(treasures[i], 750))
                {
                    tagger = npAgent;
                }

                if (tagger != null)
                {
                    treasures[i].SetTagged(tagger);
                    tagger.TreasuresTagged++;
                    treasures[i].UpdateSprite("cracked");
                }
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Stage stage = new Stage()) { stage.Run(); }
        }
    }
}
