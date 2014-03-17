/* 
 * -------------------------------------------
 * HACKER TERRAIN AND TREASURE GAME v.01
 * -------------------------------------------
 * Class: Comp 565 - Spring 2014
 * Author: Ursula Messick <ursula.messick.95@my.csun.edu>
 * Author: Billy Morales <billy.morales.520@my.csun.edu>
 * 
*/

/*  
  The file TerrainMap.cs is part of AGMGSKv5 a port of AGXNASKv4 from
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
using System.IO;  // needed for TerrainMap's use of Stream class
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

namespace TerrainMap
{

    /// <summary>
    /// XN4 project that can create terrain data textures.
    /// MonoGame project use, see note at end of summary.
    /// 
    /// Generate and save two 2D textures:  heightTexture.png and colorTexture.png.
    /// File heightTexture.png stores a terrain's height values 0..255.
    /// File colorTexture.png stores the terrain's vertex color values.
    /// The files are saved in the execution directory.
    /// 
    /// Pressing 't' will toggle the display between the height and color
    /// texture maps.  As distributed, the heightTexture will look all black
    /// because the values range from 0 to 3.
    /// 
    /// The heightTexture will be mostly black since in the SK565v3 release there
    /// are two height areas:  grass plain and pyramid.  The pyramid (upper left corner)'
    /// will show grayscale values. 
    /// Grass height values range from 0..3 -- which is black in greyscale.
    /// 
    /// Note:  using grayscale in a texture to represent height constrains the 
    /// range of heights from 0 to 255.  Often you need to scale the values into this range
    /// before saving the texture.  In your world's terrain you can then scale these 
    /// values to the range you want.  This program does not scale since no values
    /// become greater than 255.
    /// 
    /// Normally one thinks of a 2D texture as having [u, v] coordinates. 
    /// In createHeightTexture() the height and in createColorTexture the color 
    /// values are created.
    /// The heightMap and colorMap used are [u, v] -- 2D.  They are converted to a 
    /// 1D textureMap1D[u*v] when the colorTexture's values are set.
    /// This is necessary because the method
    ///       newTexture.SetData<Color>(textureMap1D);
    /// requires a 1D array, not a 2D array.
    /// 
    /// Program design was influenced by Riemer Grootjans example 3.7
    /// Create a texture and save to file.
    /// In XNA 2.0 Grame Programming Recipies:  A Problem-Solution Approach,
    /// pp 176-178, Apress, 2008.
    /// 
    /// MonoGames Note:  This program will compile and run with MonoGames, but it can't write
    /// out created textures.  MonoGames has not implemented the Texture2D method
    /// SaveAsPng(....).  With MonoGames you can use this program to design 
    /// your textures, but you will have then run with XNA4 to get the textures.
    /// 
    /// Mike Barnes
    /// 1/31/2014
    /// </summary>

    public class TerrainMap : Game
    {
        int textureWidth = 512;  // textures should be powers of 2 for mipmapping
        int textureHeight = 512;
        GraphicsDeviceManager graphics;
        GraphicsDevice device;
        SpriteBatch spriteBatch;
        Texture2D heightTexture, colorTexture; // resulting textures 
        Color[,] colorMap, heightMap;  // values for the color and height textures
        Color[] textureMap1D;  // hold the generated values for a texture.
        Random random;
        bool showHeight = false;
        KeyboardState oldState;

        /// <summary>
        /// Constructor
        /// </summary>
        public TerrainMap()
        {
            graphics = new GraphicsDeviceManager(this);
            Window.Title = "Terrain Maps " + textureWidth + " by " + textureHeight + " to change map 't'";
            Content.RootDirectory = "Content";
            random = new Random();
        }

        /// <summary>
        /// Set the window size based on the texture dimensions.
        /// </summary>

        protected override void Initialize()
        {
            // Game object exists, set its window size 
            graphics.PreferredBackBufferWidth = textureWidth;
            graphics.PreferredBackBufferHeight = textureHeight;
            graphics.ApplyChanges();
            base.Initialize();
        }

        /// <summary>
        /// Create and save two textures:  
        ///   heightTexture.png 
        ///   colorTexture.png
        /// </summary>

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            device = graphics.GraphicsDevice;
            heightTexture = createHeightTexture();
            // MonoGames hasn't implemented SaveAsPng(...)
            // will compile, but throws NotImplementedException.
#if __XNA4__
            using (Stream stream = File.OpenWrite("heightTexture.png"))
            {
                heightTexture.SaveAsPng(stream, textureWidth, textureHeight);
            }
#endif
            colorTexture = createColorTexture();
#if __XNA4__
            using (Stream stream = File.OpenWrite("colorTexture.png"))
            {
                colorTexture.SaveAsPng(stream, textureWidth, textureHeight);
            }
#endif

        }

        /// <summary>
        /// Create a height map as a texture of byte values (0..255) 
        /// that can be viewed as a greyscale bitmap.  
        /// This function uses the Brownian motion algorithm
        /// from the lecture slides to generate a terrain.
        /// </summary>
        /// <returns>height texture</returns>

        private Texture2D createHeightTexture()
        {
            float height;
            Vector3 colorVec3;

            int step = 3;
            int radius = 10;
            int nCenter = 2; // Number of centers around which terrain is built
            int[,] heightMap2 = new int[textureWidth, textureWidth]; //Create an int heightmap

            // Create one center on upper-left of map and another on lower-right
            int[,] center = new int[nCenter, 2];
            center[0, 0] = 70;
            center[0, 1] = 70;
            center[1, 0] = 400;
            center[1, 1] = 400;

            heightMap = new Color[textureWidth, textureHeight];

            // first create the "plain" heights
            for (int x = 0; x < textureWidth; x++)
                for (int z = 0; z < textureHeight; z++)
                {
                    height = ((float)random.Next(3)) / 255.0f; // float version of byte value 
                    colorVec3 = new Vector3(height, height, height);
                    heightMap[x, z] = new Color(colorVec3);
                    heightMap2[x, z] = 0;
                }
            int xCoordinate, zCoordinate, r;

            // Choose a random center to start building around
            r = random.Next(nCenter);
            xCoordinate = center[r, 0];
            zCoordinate = center[r, 1];

            // Build terrain around the radius of current coordinates
            for (int k = 0; k < 10000; k++)
            {
                for (int x = xCoordinate - radius; x <= xCoordinate + radius; x++)
                    for (int z = zCoordinate - radius; z <= zCoordinate + radius; z++)
                        if (x >= 20 && x < textureWidth - 20 && z >= 20 && z < textureHeight - 20)
                            heightMap2[x, z] += 1;
                
                // Randomly walk around terrain
                int first = random.Next(2);
                int second = random.Next(2);
                if (first == 0)
                    xCoordinate += step;
                else
                    xCoordinate -= step;
                if (second == 0)
                    zCoordinate += step;
                else
                    zCoordinate -= step;

                // If we walk out of the terrain bounds, start building around another center
                if (xCoordinate < 0 || xCoordinate >= textureWidth || zCoordinate < 0 || zCoordinate >= textureHeight)
                {
                    r = random.Next(nCenter);
                    xCoordinate = center[r, 0];
                    zCoordinate = center[r, 1];
                }
            }

            for (int x = 0; x < textureWidth; x++)
                for (int z = 0; z < textureHeight; z++)
                {
                    height = heightMap2[x, z] / 255.0f;  // convert to grayscale 0.0 to 255.0f
                    heightMap[x, z] =
                       new Color(new Vector3(height, height, height));
                }


            // convert heightMap[,] to textureMap1D[]
            textureMap1D = new Color[textureWidth * textureHeight];
            int j = 0;
            for (int x = 0; x < textureWidth; x++)
                for (int z = 0; z < textureHeight; z++)
                {
                    textureMap1D[j] = heightMap[x, z];
                    j++;
                }

            // create the texture to return.       
            Texture2D newTexture = new Texture2D(device, textureWidth, textureHeight);
            newTexture.SetData<Color>(textureMap1D);
            return newTexture;
        }

        /// <summary>
        /// Create a color texture that will be used to "color" the terrain.
        /// Some comments about color that might explain some of the code in createColorTexture().
        /// Colors can be converted to vector4s.   vector4Value =  colorValue / 255.0
        /// color's (RGBA), color.ToVector4()
        /// Color.DarkGreen (R:0 G:100 B:0 A:255)    vector4 (X:0 Y:0.392 Z:0 W:1)  
        /// Color.Green     (R:0 G:128 B:0 A:255)    vector4 (X:0 Y:0.502 Z:0 W:1)  
        /// Color.OliveDrab (R:107 G:142 B:35 A:255) vector4 (X:0.420 Y:0.557 Z:0.137, W:1) 
        /// You can create colors with new Color(byte, byte, byte, byte) where byte = 0..255
        /// or, new Color(byte, byte, byte).
        /// 
        /// The Color conversion to Vector4 and back is used to add noise.
        /// You could just have Color.
        /// </summary>
        /// <returns>color texture</returns>

        private Texture2D createColorTexture()
        {
            int grassHeight = 5;
            Vector4 colorVec4 = new Vector4();
            colorMap = new Color[textureWidth, textureHeight];
            for (int x = 0; x < textureWidth; x++)
                for (int z = 0; z < textureHeight; z++)
                {
                    if (heightMap[x, z].R < grassHeight) // make random grass
                        switch (random.Next(3))
                        {
                            case 0: colorVec4 = new Color(0, 0, 0, 255).ToVector4(); break;  // Color.DarkGreen
                            case 1: colorVec4 = Color.Black.ToVector4(); break;
                            case 2: colorVec4 = Color.LightGreen.ToVector4(); break;
                        }
                    else if (heightMap[x, z].R < 50) colorVec4 = Color.Black.ToVector4();
                    else if (heightMap[x, z].R < 90) colorVec4 = Color.LightSkyBlue.ToVector4();
                    else if (heightMap[x, z].R < 130) colorVec4 = Color.Black.ToVector4();
                    else if (heightMap[x, z].R < 170) colorVec4 = Color.DarkBlue.ToVector4();
                    else colorVec4 = Color.White.ToVector4();
                    // add some noise to the color
                    colorVec4 = colorVec4 + new Vector4((float)(random.NextDouble() / 20.0));
                    colorMap[x, z] = new Color(colorVec4);
                }
            // convert colorMap[,] to textureMap1D[]
            textureMap1D = new Color[textureWidth * textureHeight];
            int i = 0;
            for (int x = 0; x < textureWidth; x++)
                for (int z = 0; z < textureHeight; z++)
                {
                    textureMap1D[i] = colorMap[x, z];
                    i++;
                }
            // create the texture to return.   
            Texture2D newTexture = new Texture2D(device, textureWidth, textureHeight);
            newTexture.SetData<Color>(textureMap1D);
            return newTexture;
        }
        /*
           /// <summary>
           /// UnloadContent will be called once per game and is the place to unload
           /// all content.
           /// </summary>
           protected override void UnloadContent() {
              // TODO: Unload any non ContentManager content here
              }
        */
        /// <summary>
        /// Process user keyboard input.
        /// Pressing 'T' or 't' will toggle the display between the height and color textures
        /// </summary>

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape)) Exit();
            else if (Keyboard.GetState().IsKeyDown(Keys.T) && !oldState.IsKeyDown(Keys.T))
                showHeight = !showHeight;
            oldState = keyboardState;    // Update saved state.
            base.Update(gameTime);
        }

        /// <summary>
        /// Display the textures.
        /// </summary>
        /// <param name="gameTime"></param>

        protected override void Draw(GameTime gameTime)
        {
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1, 0);
            spriteBatch.Begin();
            if (showHeight)
                spriteBatch.Draw(heightTexture, Vector2.Zero, Color.White);
            else
                spriteBatch.Draw(colorTexture, Vector2.Zero, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        static void Main2(string[] args)
        {
            using (TerrainMap game = new TerrainMap())
            {
                game.Run();
            }
        }
    }
}
