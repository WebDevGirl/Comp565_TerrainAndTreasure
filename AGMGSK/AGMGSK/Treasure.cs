using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AGMGSK
{
    /// <summary>
    /// Treasure is a Model3D Object that can be tagged or not once per game. 
    /// </summary>
    class Treasure : Model3D 
    {

        public Treasure(Stage theStage, string label, string meshFile): base(theStage, label, meshFile) 
        {

        }
    }
}
