using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Communication
{
    public class UserUtils
    {
        //UserUtils.getDroneType() == 1 ? "FYLO" : UserUtils.getDroneType() == 2 ? "LEDDI" : UserUtils.getDroneType() == 3 ? "EDU" : "FYLOS"

        internal static int getDroneType()
        {
            return 1;
        }
    }
}
