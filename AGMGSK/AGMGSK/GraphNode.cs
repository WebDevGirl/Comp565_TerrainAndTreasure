using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if ! __XNA4__  // when __XNA4__ == true build for MonoGames
   using Microsoft.Xna.Framework.Storage; 
#endif
using Microsoft.Xna.Framework.GamerServices;
namespace AGMGSK
{
    public class GraphNode : NavNode, IEquatable<GraphNode>, IComparable<GraphNode>
    {
        public int distanceFromSource, distanceToGoal;
        public List<GraphNode> adjacent;
        public bool hasNeighbors;
        public GraphNode pathPredecessor;

        public int Heuristic
        {
            get { return distanceFromSource + distanceToGoal; }
        }



        public GraphNode(Vector3 pos):base(pos)
        {
            adjacent = new List<GraphNode>();
            pathPredecessor = null;

        }


        public void AddAdjacent(GraphNode node)
        {
            adjacent.Add(node);
        }

        public string ToString()
        {
            return "X: " + Translation.X + " Z: " + Translation.Z + "\n";
        }

        public bool Equals(GraphNode n)
        {
            return this.Translation.X == n.Translation.X && this.Translation.Z == n.Translation.Z;
        }


        public int CompareTo(GraphNode n)
        {
            if (Heuristic < n.Heuristic) return -1;
            else if (Heuristic > n.Heuristic) return 1;
            else return 0;
        }

    }



}
