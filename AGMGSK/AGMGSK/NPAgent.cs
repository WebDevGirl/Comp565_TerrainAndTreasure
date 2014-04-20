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
 
  The file NPAgent.cs is part of AGMGSKv5 a port of AGXNASKv4 from
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
using System.Diagnostics;
#if ! __XNA4__  // when __XNA4__ == true build for MonoGames
   using Microsoft.Xna.Framework.Storage; 
#endif
using Microsoft.Xna.Framework.GamerServices;
#endregion

namespace AGMGSK
{

    /// <summary>
    /// A non-playing character that moves.  Override the inherited Update(GameTime)
    /// to implement a movement (strategy?) algorithm.
    /// Distribution NPAgent moves along an "exploration" path that is created by
    /// method makePath().  The exploration path is traversed in a reverse path loop.
    /// Paths can also be specified in text files of Vector3 values, see alternate
    /// Path class constructor.
    /// 
    /// 2/2/2014 last changed
    /// </summary>

    public class NPAgent : Agent
    {
        private NavNode nextGoal;
        private NavNode savedGoal;
        private Path path;
        private Path terrian_path;
        private Path treasure_path;
        private int snapDistance = 20;
        private int turnCount = 0;
        private int mode = 0;


        /// <summary>
        /// Create a NPC. 
        /// AGXNASK distribution has npAgent move following a Path.
        /// </summary>
        /// <param name="theStage"> the world</param>
        /// <param name="label"> name of </param>
        /// <param name="pos"> initial position </param>
        /// <param name="orientAxis"> initial rotation axis</param>
        /// <param name="radians"> initial rotation</param>
        /// <param name="meshFile"> Direct X *.x Model in Contents directory </param>

        public NPAgent(Stage theStage, string label, Vector3 pos, Vector3 orientAxis,
           float radians, string meshFile)
            : base(theStage, label, pos, orientAxis, radians, meshFile)
        {  // change names for on-screen display of current camera
            first.Name = "npFirst";
            follow.Name = "npFollow";
            above.Name = "npAbove";
            // path is built to work on specific terrain
            terrian_path = new Path(stage, makePath(), Path.PathType.REVERSE); // continuous search path
            stage.Components.Add(terrian_path);


            path = terrian_path; // start off in path finding mode
            nextGoal = path.NextNode;  // get first path goal
            agentObject.turnToFace(nextGoal.Translation);  // orient towards the first path goal
        }

        /// <summary>
        /// Switch the npAgent's navigation mode to either treasure or path finding
        /// 0: Path Finding Mode
        /// 1: Treasure Finding Mode
        /// </summary>
        /// <returns></returns>
        public void switchMode()
        {
            switch (mode)
            {
                case 0: // Switch to Mode: EM --> TM
                    Debug.WriteLine("Switching Mode: Explore Mode --> Treasure Mode (" + mode + " --> "+ (mode+1) % 3 +" )");
                    mode = 1;
                    
                    // Save EM goal
                    savedGoal = nextGoal; 

                    // Set current treasure finding path to closest treasure
                    treasure_path = new Path(stage, chooseClosestTreasure(stage.Treasures), Path.PathType.SINGLE);

                    // if treasure found find it by setting path nextGoal
                    if (treasure_path.Count != 0)
                    {                      
                        restart(); // Start moving again incase we are stopped
                        stage.Components.Add(treasure_path);
                        path = treasure_path;
                        nextGoal = path.NextNode;
                        agentObject.turnToFace(nextGoal.Translation);
                    } else {
                        Debug.WriteLine("No treasures found, stop"); 
                        base.reset();
                    }

                    break;
                case 1: // Switch to Mode: TM --> RM
                    Debug.WriteLine("Switching Mode: Treasure Mode --> Return Mode (" + mode + " --> " + (mode + 1) % 3 + " )");
                    mode = 2;                  
                    path = terrian_path;  // Set path back to Explore Mode Path   
                    nextGoal = savedGoal;
                    agentObject.turnToFace(nextGoal.Translation);
                    break;
                case 2: // Switch to Mode: RM --> EM
                    Debug.WriteLine("Switching Mode: Return Mode --> Explore Mode (" + mode + " --> " + (mode + 1) % 3 + " )");
                    mode = 0;
                    path = terrian_path; // Set path back to Explore Mode Path  
                   
                    // Update goal if we reached our last saved goal, otherwise keep savedGoal
                    if (nextGoal != savedGoal)
                    {
                        nextGoal = path.NextNode;
                    }
                    agentObject.turnToFace(nextGoal.Translation);
                    break;
                default:
                    Debug.WriteLine("Bad Toggle Mode");
                    break;
            }

        }

        /// <summary>
        /// Switch the npAgent's navigation back to path finding
        /// </summary>
        /// <returns></returns>
        public void switchModeToPathFinding()
        {
            mode = 0;
            path = terrian_path;            
        }

        /// <summary>
        /// Sets next goal based on mode and position
        /// </summary>
        /// <returns>NavNode</returns>
        public void setNextGoal()
        {
            if (path.Done && mode == 1) { // Current path is done as we are in TreasureMode
                switchMode();               
            }
            else if (mode == 2) { // Current path is done and we are in ReturnMode
                switchMode();                
            } else { 
                nextGoal = path.NextNode;
                agentObject.turnToFace(nextGoal.Translation);
            }     
      
            
        }


        /// <summary>
        /// Procedurally make a path for NPAgent to traverse to the closest un-tagged treasure
        /// </summary>
        /// <returns></returns>
        private List<NavNode> chooseClosestTreasure(List<Treasure> treasures)
        {
            float distance = 0;
            float minDistance = 10000000000000000;
            Treasure minTreasure = null;
            List<NavNode> aPath = new List<NavNode>();
            
            /* Loop through all treasures to find one that is closest to npAgent */
            foreach (Treasure treasure in treasures)
            {
                /* Find the closest non-tagged treasure */
                if (treasure.IsTagged == false)
                {
                    /* Compare distances of treasure and npAgent */ 
                    distance = Vector3.Distance(
                        treasure.position,
                        new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z)
                    );

                    /* If closer treasure found, set as potential next treasure */ 
                    if (distance < minDistance)
                    {
                        minTreasure = treasure;
                        minDistance = distance;
                    }
                }
            }
                                  
            /* Set path to treasure or if untagged one found */
            if (minTreasure != null)
            { 
                // create a path to the treasure   
                aPath.Add(new NavNode(minTreasure.position, NavNode.NavNodeEnum.PATH));
            }
         
            return (aPath);
        }

        /// <summary>
        /// Procedurally make a path for NPAgent to traverse
        /// </summary>
        /// <returns></returns>
        private List<NavNode> makePath()
        {
            List<NavNode> aPath = new List<NavNode>();
            int spacing = stage.Spacing;
            // make a simple path, show how to set the type of the NavNode outside of construction.
            NavNode n;
            // path section near Player for testing 
            n = new NavNode(new Vector3(505 * spacing, stage.Terrain.surfaceHeight(505, 505), 505 * spacing));
            n.Navigatable = NavNode.NavNodeEnum.PATH;
            aPath.Add(n);
            n = new NavNode(new Vector3(500 * spacing, stage.Terrain.surfaceHeight(500, 500), 500 * spacing));
            n.Navigatable = NavNode.NavNodeEnum.VERTEX;
            aPath.Add(n);
            aPath.Add(new NavNode(new Vector3(495 * spacing, stage.Terrain.surfaceHeight(495, 495), 495 * spacing),
                     NavNode.NavNodeEnum.A_STAR));
            aPath.Add(new NavNode(new Vector3(495 * spacing, stage.Terrain.surfaceHeight(495, 505), 505 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            aPath.Add(new NavNode(new Vector3(383 * spacing, stage.Terrain.surfaceHeight(383, 500), 500 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            //// go by wall   
            //aPath.Add(new NavNode(new Vector3(445 * spacing, stage.Terrain.surfaceHeight(445, 438), 438 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(500 * spacing, stage.Terrain.surfaceHeight(500, 383), 383 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(500 * spacing, stage.Terrain.surfaceHeight(500, 100), 100 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(105 * spacing, stage.Terrain.surfaceHeight(105, 105), 105 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(105 * spacing, stage.Terrain.surfaceHeight(105, 495), 495 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //// turning circle 
            //aPath.Add(new NavNode(new Vector3(80 * spacing, stage.Terrain.surfaceHeight(80, 505), 505 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(60 * spacing, stage.Terrain.surfaceHeight(60, 490), 490 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            //aPath.Add(new NavNode(new Vector3(105 * spacing, stage.Terrain.surfaceHeight(105, 495), 495 * spacing),
            //         NavNode.NavNodeEnum.WAYPOINT));
            return (aPath);
        }

        // Checks whether an NPAgent can sense a treasure within a certain radius
        public void senseTreasures()
        {
            // Only sense if not in TM            
            if (mode != 0) { return; }
            foreach (Treasure treasure in stage.Treasures)
            {
               float distance = Vector3.Distance(
                        treasure.position,
                        new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z)
                    );

               if (distance < 1000)
                {
                    Debug.WriteLine("Treasure Detected!! Search for it. " + distance);
                    switchMode();
                    return;
                }
            }                     
        }


        /// <summary>
        /// Simple path following.  If within "snap distance" of a the nextGoal (a NavNode) 
        /// move to the NavNode, get a new nextGoal, turnToFace() that goal.  Otherwise 
        /// continue making steps towards the nextGoal.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            stage.setInfo(15,
               string.Format("npAvatar:  Location ({0:f0},{1:f0},{2:f0})  Looking at ({3:f2},{4:f2},{5:f2})",
                  agentObject.Translation.X, agentObject.Translation.Y, agentObject.Translation.Z,
                  agentObject.Forward.X, agentObject.Forward.Y, agentObject.Forward.Z));

            stage.setInfo(16,
               string.Format("nextGoal:  ({0:f0},{1:f0},{2:f0})", nextGoal.Translation.X, nextGoal.Translation.Y, nextGoal.Translation.Z));

            // See if at or close to nextGoal, distance measured in the flat XZ plane
            float distance = Vector3.Distance(
               new Vector3(nextGoal.Translation.X, 0, nextGoal.Translation.Z),
               new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z));
            stage.setInfo(15, string.Format("Next Node = {0}", path.nextNode));
            stage.setInfo(16, string.Format("distance to goal = {0,5:f2}", distance));

            if (distance <= snapDistance)
            {
                // Clear saved goal if its set and we just reached it
                if ((savedGoal != null) && (nextGoal == savedGoal))
                {
                    savedGoal = null;
                }

                // Set Next Goal, includes switching to the correct mode
                setNextGoal();                

                // See if we should stop or turn twoards next goal
                if (path.Done)
                {
                    stage.setInfo(18, "path traversal is done");
                    base.reset(); // Stop NP Agent
                } else {
                    turnCount++;
                    stage.setInfo(17, string.Format("turnToFace count = {0}", turnCount));
                }
            }

            senseTreasures();

            base.Update(gameTime);  // Agent's Update();
        }
    }
}
