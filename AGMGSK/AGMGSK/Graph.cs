using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace AGMGSK {

    public class Graph{
        const float agentRadius = 161.274811f;
        List<GraphNode> open;
        List<GraphNode> closed;
        Stage stage;

        public Graph(Stage stage) {
            this.stage = stage;
           
        
        }
        /// <summary>
        /// Checks if position is navigatable through all collidable objects
        /// </summary>
        /// <param name="pos">The position being checked</param>
        /// <param name="boundingRadius">The bounding radius to check for collisions</param>
        /// <returns></returns>
        private bool IsPositionNavigatable(Vector3 pos, float boundingRadius)
        {
            foreach (Object3D obj in stage.Collidable)
            {
                if (obj.Name.Contains("Chaser")||obj.Name.Contains("dog"))
                {
                    continue;
                }
                if (obj.CheckForCollision(pos, boundingRadius))
                {
                    return false;    
                }
            }
            return true;
        }


        /// <summary>
        /// Generates a nodes neighbors
        /// </summary>
        /// <param name="node">The node to add the neighbors too</param>
        private void AddNodeNeighbors(GraphNode node)
        {
            int spacing = stage.Spacing;
            int lowerBound = spacing;
            int upperBound = spacing * 510;

            //Check surrounding nodes.
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    //If it is itself.
                    if(j == 0 && i ==0){
                        continue;
                    }
                    //New x and z coordinates based on offsets. 
                    int xOffset = (int)node.Translation.X + i * spacing;
                    int zOffset = (int)node.Translation.Z + j * spacing;
                    //are offset in bounds 
                    if (xOffset < lowerBound || xOffset > upperBound || zOffset < lowerBound || zOffset > upperBound)
                    {
                        continue;
                    }
                    // Check if neighbors exists already in open or closed sets.
                    GraphNode neighbor = open.Find(x => x.Translation.X == xOffset && x.Translation.Z == zOffset);
                    if (neighbor == null)
                    {
                        closed.Find(x => x.Translation.X == xOffset && x.Translation.Z == zOffset);
                    }
                    //If doesn't exists generate a new node.
                    if (neighbor == null)
                    {
                        neighbor = GenerateNode(xOffset, zOffset);
                    }
                    //If the neighbor isn't null add to adjacent list. 
                    if (neighbor != null)
                    {
                        node.AddAdjacent(neighbor);
                    }
                }

            }
        }

        /// <summary>
        /// Generates a node if position is navigatable.
        /// </summary>
        /// <param name="x">x position of new node</param>
        /// <param name="z">z position of new node</param>
        /// <returns></returns>
        private GraphNode GenerateNode(int x, int z)
        {
            Vector3 pos = new Vector3(x, stage.Terrain.surfaceHeight(x/stage.Spacing, z/stage.Spacing), z);
            if (!IsPositionNavigatable(pos, agentRadius))
            {
                return null;
            }
            return (new GraphNode(pos));
        }

        /// <summary>
        /// Snaps a position to the grid.
        /// </summary>
        /// <param name="pos">Position to be snapped</param>
        /// <returns></returns>
        private Vector3 SnapToGrid(Vector3 pos)
        {
            int x = (int)pos.X / stage.Spacing;
            int z = (int)pos.Z / stage.Spacing;
            float y = stage.Terrain.surfaceHeight(x, z);
            return new Vector3(x*stage.Spacing, y, z*stage.Spacing);
        }

        /// <summary>
        /// Returns a path from S to E.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public Path GetAStarPath(Vector3 s, Vector3 e){
            DateTime time = DateTime.Now;
            open = new List<GraphNode>();
            closed = new List<GraphNode>();
            GraphNode goal = new GraphNode(SnapToGrid(e));
            GraphNode start = new GraphNode(SnapToGrid(s));
            start.distanceFromSource = 0;
            start.distanceToGoal = (int)Vector3.Distance(start.Translation, goal.Translation);

            //GraphNode end = new GraphNode(e);
            open.Add(start);
            GraphNode current = null ;
            while (open.Count != 0)
            {
                current = open[0];
                open.RemoveAt(0);
                if (current.Equals(goal))
                {
                    break;
                }
                else
                {
                    closed.Add(current);
                    if (!current.hasNeighbors)
                    {
                        AddNodeNeighbors(current);
                        current.hasNeighbors = true;
                    }
                    foreach (GraphNode node in current.adjacent)
                    {
                        if(closed.Contains(node)){
                            continue;
                        }
                        
                        int distanceToSource = current.distanceFromSource + (int)Vector3.Distance(current.Translation, node.Translation);

                        if (!open.Contains(node) || distanceToSource < node.distanceFromSource)
                        {
                            node.pathPredecessor = current;
                            node.distanceFromSource = distanceToSource;
                            node.distanceToGoal = (int)Vector3.Distance(node.Translation, goal.Translation);
                            if(!open.Contains(node)){
                                open.Add(node);
                            }
                        }

                    }

                }
                open.Sort();                
            }
            List<NavNode> aPath = new List<NavNode>();
            
            while (current.pathPredecessor != null)
            {
                aPath.Add(current);
                current = current.pathPredecessor;
            }
            TimeSpan t = DateTime.Now - time;
            Console.WriteLine(t.Seconds);

            return new Path(stage, aPath, Path.PathType.SINGLE);
        }




    }
}
