using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace AGMGSK
{
    /// <summary>
    /// Treasure is a Model3D Object that can be tagged or not once per game. 
    /// </summary>
    public class Treasure : Model3D
    {
        protected Boolean isTagged = false;
        protected Player taggedBy = null;
        public Vector3 position;


        public Treasure(Stage theStage, string label, string meshFile)
            : base(theStage, label, meshFile)
        {
            Debug.WriteLine("Creating a new treasure");
        }

        override public Object3D addObject(Vector3 position, Vector3 orientAxis, float radians)
        {
            this.position = position;
            return base.addObject(position, orientAxis, radians);

        }

    }




}
