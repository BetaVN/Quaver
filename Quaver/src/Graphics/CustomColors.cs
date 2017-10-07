﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Quaver.Graphics
{
    public struct CustomColors
    {
        /// <summary>
        /// Swan's favorite color; #db88c2. This color is used for the developers of the game.
        /// </summary>
        public static readonly Color NameTag_Admin = new Color(219, 136, 194, 1);

        /// <summary>
        /// Nametag color for Community Managers, Map Nominators, ect.
        /// </summary>
        public static readonly Color NameTag_Moderator = new Color(59, 233, 106, 1);

        /// <summary>
        /// Nametag color for Quaver Supporters/Donators.
        /// </summary>
        public static readonly Color NameTag_Supporter = new Color(76, 146, 211, 1);

        /// <summary>
        /// Nametag color for regular users.
        /// </summary>
        public static readonly Color NameTag_Regular = new Color(76, 146, 211, 1);
    }
}