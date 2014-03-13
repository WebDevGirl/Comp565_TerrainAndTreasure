/*  
    Copyright (C) 2014 G. Michael Barnes
 
    The file Terrain.cs is part of AGMGSKv5 a port of AGXNASKv4 from
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
    /// Terrain represents a ground.
    /// The vertices have position and color.  Terrain width = height.  
    /// Reads two textures to set terrain height and color values.
    /// You might want to pre-compute and store heights of surfaces to be 
    /// returned by the surfaceHeight(x, z) method.
    /// 
    /// 1/25/2012 last changed
    /// </summary>
    /// 
    public class Terrain : IndexVertexBuffers
    {
        protected VertexPositionColor[] vertex;  // stage vertices    
        private int height, width, multiplier = 20, spacing;
        private int[,] terrainHeight;
        private BasicEffect effect;
        private GraphicsDevice display;
        private Texture2D heightTexture, colorTexture;
        private Microsoft.Xna.Framework.Color[] heightMap, colorMap;


        public Terrain(Stage theStage, string label, string heightFile, string colorFile)
            : base(theStage, label)
        {
            int i;
            range = stage.Range;
            width = height = range;
            nVertices = width * height;
            terrainHeight = new int[width, height];
            vertex = new VertexPositionColor[nVertices];
            nIndices = (width - 1) * (height - 1) * 6;
            indices = new int[nIndices];  // there are 6 indices 2 faces / 4 vertices 
            spacing = stage.Spacing;
            // set display information 
            display = stage.Display;
            effect = stage.SceneEffect;
            heightTexture = stage.Content.Load<Texture2D>(heightFile);
            heightMap =
               new Microsoft.Xna.Framework.Color[width * height];
            heightTexture.GetData<Microsoft.Xna.Framework.Color>(heightMap);
            // create colorMap values from colorTexture
            colorTexture = stage.Content.Load<Texture2D>(colorFile);
            colorMap =
               new Microsoft.Xna.Framework.Color[width * height];
            colorTexture.GetData<Microsoft.Xna.Framework.Color>(colorMap);
            // create  vertices for terrain
            Vector4 vector4;
            int vertexHeight;
            i = 0;
            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                {
                    vector4 = heightMap[i].ToVector4();       // convert packed Rgba32 values to floats
                    vertexHeight = (int)(vector4.X * 255);   // scale vertexHeight 0..255
                    vertexHeight *= multiplier;               // multiply height
                    terrainHeight[x, z] = vertexHeight;       // save height for navigation
                    vertex[i] = new VertexPositionColor(
                       new Vector3(x * spacing, vertexHeight, z * spacing),
                       new Color(colorMap[i].ToVector4()));
                    i++;
                }
            // free up unneeded maps
            colorMap = null;
            heightMap = null;
            // set indices clockwise from point of view
            i = 0;
            for (int z = 0; z < height - 1; z++)
                for (int x = 0; x < width - 1; x++)
                {
                    indices[i++] = z * width + x;
                    indices[i++] = z * width + x + 1;
                    indices[i++] = (z + 1) * width + x;
                    indices[i++] = (z + 1) * width + x;
                    indices[i++] = z * width + x + 1;
                    indices[i++] = (z + 1) * width + x + 1;
                }

            // create VertexBuffer and store on GPU
            vb = new VertexBuffer(display, typeof(VertexPositionColor), vertex.Length, BufferUsage.WriteOnly);
            vb.SetData<VertexPositionColor>(vertex); // , 0, vertex.Length);
            // create IndexBuffer and store on GPU
            ib = new IndexBuffer(display, typeof(int), indices.Length, BufferUsage.WriteOnly);
            IB.SetData<int>(indices);
        }

        // Properties

        public int Spacing
        {
            get { return stage.Spacing; }
        }

        // Methods

        ///<summary>
        /// Height of  surface containing position (x,z) terrain coordinates.
        /// Uses the HeightMap interpolation algorithm from
        /// XNA 3.0 Game Programming Recipes by Riemer Grootjans
        /// </summary>
        /// <param name="x"> left -- right terrain position </param>
        /// <param name="z"> forward -- backward terrain position</param>
        /// <returns> vertical height of surface containing position (x,z)</returns>
        public float surfaceHeight(float x, float z)
        {
            if (x < 0 || x > 511 || z < 0 || z > 511) return 0.0f;  // index valid ?

            // Find relative x value
            int xLower = (int)x;
            int xHigher = xLower + 1;
            float xRelative = (x - xLower) / ((float)xHigher - (float)xLower);

            // Find relative z value
            int zLower = (int)z;
            int zHigher = zLower + 1;
            float zRelative = (z - zLower) / ((float)zHigher - (float)zLower);

            // Find minimum and maximum height values
            int heightLxLz = terrainHeight[xLower, zLower];
            int heightLxHz = terrainHeight[xLower, zHigher];
            int heightHxLz = terrainHeight[xHigher, zLower];
            int heightHxHz = terrainHeight[xHigher, zHigher];

            float finalHeight;

            // Determine whether the (x, z) coordinate is in the
            // upper or lower triangle

            if (xRelative + zRelative < 1) // Above lower triangle
            {
                finalHeight = heightLxLz;
                finalHeight += zRelative * (heightLxHz - heightLxLz);
                finalHeight += xRelative * (heightHxLz - heightLxLz);
            }
            else
            {
                finalHeight = heightHxHz;
                finalHeight += (1.0f - zRelative) * (heightHxLz - heightHxHz);
                finalHeight += (1.0f - xRelative) * (heightLxHz - heightHxHz);
            }
         
            return (float)finalHeight;
        }

        public override void Draw(GameTime gameTime)
        {
            effect.VertexColorEnabled = true;
            if (stage.Fog)
            {
                effect.FogColor = Color.CornflowerBlue.ToVector3();
                effect.FogStart = stage.FogStart;
                effect.FogEnd = stage.FogEnd;
                effect.FogEnabled = true;
            }
            else effect.FogEnabled = false;
            effect.DirectionalLight0.DiffuseColor = stage.DiffuseLight;
            effect.AmbientLightColor = stage.AmbientLight;
            effect.DirectionalLight0.Direction = stage.LightDirection;
            effect.DirectionalLight0.Enabled = true;
            effect.View = stage.View;
            effect.Projection = stage.Projection;
            effect.World = Matrix.Identity;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                display.SetVertexBuffer(vb);
                display.Indices = ib;
                display.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, nVertices,
                   0, nIndices / 3);
            }
        }
    }
}
