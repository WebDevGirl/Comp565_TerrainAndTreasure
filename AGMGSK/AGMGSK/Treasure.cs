/* 
 * -------------------------------------------
 * HACKER TERRAIN AND TREASURE GAME v.01
 * -------------------------------------------
 * Class: Comp 565 - Spring 2014
 * Author: Ursula Messick <ursula.messick.95@my.csun.edu>
 * Author: Billy Morales <billy.morales.520@my.csun.edu>
 * 
*/

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
        protected String taggedBy = null;


        public Treasure(Stage theStage, string label, string meshFile)
            : base(theStage, label, meshFile)
        {
            Debug.WriteLine("Creating a new treasure");
        }

        public Boolean IsTagged
        {
            get { return isTagged; }
        }

        // store position of treasure before calling the addObject function.
        override public Object3D addObject(Vector3 position, Vector3 orientAxis, float radians)
        {
            this.position = position;
            return base.addObject(position, orientAxis, radians);

        }

        // Sets isTagged to true and outputs the Agent who tagged the treasure
        public void SetTagged(Agent agent)
        {
            this.isTagged = true;
            Debug.WriteLine(agent.GetType().Name + " tagged the treasure!");
        }

    }




}
