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
  Copyright (C) 2014 G. Michael Barnes
 
  The file Pack.cs is part of AGMGSKv5 a port of AGXNASKv4 from
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
    /// Pack represents a "flock" of MovableObject3D's Object3Ds.
    /// Usually the "player" is the leader and is set in the Stage's LoadContent().
    /// With no leader, determine a "virtual leader" from the flock's members.
    /// Model3D's inherited List<Object3D> instance holds all members of the pack.
    /// 
    /// 1/25/2012 last changed
    /// </summary>
    /// 
    enum FlockLevel { LOW = 0, LOW_MID = 33, HIGH_MID = 66, HIGH = 99 }

    public class Pack : MovableModel3D
    {
        Object3D leader;
        Random random = null;

        // Pack flocking level
        static FlockLevel level = FlockLevel.LOW;

        /// <summary>
        /// Construct a leaderless pack.
        /// </summary>
        /// <param name="theStage"> the scene</param>
        /// <param name="label"> name of pack</param>
        /// <param name="meshFile"> model of pack instance</param>
        public Pack(Stage theStage, string label, string meshFile)
            : base(theStage, label, meshFile)
        {
            isCollidable = true;
            leader = null;
            random = new Random();
        }

        /// <summary>
        /// Construct a pack with an Object3D leader
        /// </summary>
        /// <param name="theStage"> the scene </param>
        /// <param name="label"> name of pack</param>
        /// <param name="meshFile"> model of a pack instance</param>
        /// <param name="aLeader"> Object3D alignment and pack center </param>
        public Pack(Stage theStage, string label, string meshFile, Object3D aLeader)
            : base(theStage, label, meshFile)
        {
            isCollidable = true;
            leader = aLeader;
            random = new Random();
        }

        /// <summary>
        /// Each pack member's orientation matrix will be updated.
        /// Distribution has pack of dogs moving randomly.  
        /// Supports leaderless and leader based "flocking" 
        /// </summary>      
        public override void Update(GameTime gameTime)
        {
            // if (leader == null) need to determine "virtual leader from members"
            float angle = 0.3f;

            if (leader == null)
            {
                foreach (Object3D obj in instance)
                {
                    obj.Yaw = 0.0f;
                    // change direction 4 time a second  0.07 = 4/60
                    if (random.NextDouble() < 0.07)
                    {
                        if (random.NextDouble() < 0.5) obj.Yaw -= angle; // turn left
                        else obj.Yaw += angle; // turn right
                    }
                    obj.updateMovableObject();
                    stage.setSurfaceHeight(obj);
                }
            }

            else
            {
                foreach (Object3D obj in instance)
                {

                    obj.Translation += getCohesion(obj) + getSeparation(obj);
                    
                    obj.Forward = leader.Forward;

                    
                    obj.updateMovableObject();
                    stage.setSurfaceHeight(obj);
                }

            }

            base.Update(gameTime);  // MovableMesh's Update(); 
        }

        public static void changeFlockLevel() 
        {
            switch (level)
            {
                case FlockLevel.LOW:
                    level = FlockLevel.LOW_MID;
                    break;
                case FlockLevel.LOW_MID:
                    level = FlockLevel.HIGH_MID;
                    break;
                case FlockLevel.HIGH_MID:
                    level = FlockLevel.HIGH;
                    break;
                case FlockLevel.HIGH:
                    level = FlockLevel.LOW;
                    break;
            }
        }

        public Vector3 getAlignment(Object3D current)
        {
           
            return Vector3.Normalize(leader.Forward);

        }

        public Vector3 getCohesion(Object3D current)
        {
            if (current != leader)
            {
                Vector3 cohesion = current.Translation - leader.Translation;
                
                cohesion.Normalize();
                return cohesion * -10;
            }
            return Vector3.Zero;

        }

        public Vector3 getSeparation(Object3D current)
        {
            Vector3 separation = Vector3.Zero;
            float distanceRadius = 5000;
            foreach (Object3D obj in instance)
            {
                if (current != obj && current != leader)
                {

                    if (Vector3.Distance(current.Translation, leader.Translation) < distanceRadius)
                    {
                        separation = current.Translation - leader.Translation;
                        float scale = separation.Length() / distanceRadius;

                        separation.Normalize();
                        separation /= scale;

                    }
                }
            }
            return separation;
        }

        public static int getLevelValue()
        {
            return (int)level;
        }

        public Object3D Leader
        {
            get { return leader; }
            set { leader = value; }
        }
    }
}
