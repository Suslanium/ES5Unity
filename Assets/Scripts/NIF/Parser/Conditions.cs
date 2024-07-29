using NIF.Parser.NiObjects;

namespace NIF.Parser
{
    /// <summary>
    /// Commonly used version expressions.
    /// </summary>
    public static class Conditions
    {
        /// <summary>
        /// NiStream that are not Bethesda.
        /// </summary>
        public static bool NiStream(Header header)
        {
            return header.BethesdaVersion == 0;
        }

        /// <summary>
        /// NiStream that are Bethesda.
        /// </summary>
        public static bool BsStream(Header header)
        {
            return header.BethesdaVersion > 0;
        }

        /// <summary>
        /// All NI + BS until BSVER 16.
        /// </summary>
        public static bool NiBsLte16(Header header)
        {
            return header.BethesdaVersion <= 16;
        }

        /// <summary>
        /// All NI + BS before Fallout 3.
        /// </summary>
        public static bool NiBsLtFo3(Header header)
        {
            return header.BethesdaVersion < 34;
        }

        /// <summary>
        /// All NI + BS until Fallout 3.
        /// </summary>
        public static bool NiBsLteFo3(Header header)
        {
            return header.BethesdaVersion <= 34;
        }
        
        /// <summary>
        /// All NI + BS before SSE.
        /// </summary>
        public static bool NiBsLtSse(Header header)
        {
            return header.BethesdaVersion < 100;
        }
        
        /// <summary>
        /// All NI + BS before Fallout 4.
        /// </summary>
        public static bool NiBsLtFo4(Header header)
        {
            return header.BethesdaVersion < 130;
        }

        /// <summary>
        /// All NI + BS until Fallout 4.
        /// </summary>
        public static bool NiBsLteFo4(Header header)
        {
            return header.BethesdaVersion <= 139;
        }
        
        /// <summary>
        /// Skyrim, SSE, and Fallout 4
        /// </summary>
        public static bool BsGtFo3(Header header)
        {
            return header.BethesdaVersion > 34;
        }

        /// <summary>
        /// FO3 and later.
        /// </summary>
        public static bool BsGteFo3(Header header)
        {
            return header.BethesdaVersion >= 34;
        }
        
        /// <summary>
        /// Skyrim and later.
        /// </summary>
        public static bool BsGteSky(Header header)
        {
            return header.BethesdaVersion >= 83;
        }

        /// <summary>
        /// SSE and later.
        /// </summary>
        public static bool BsGteSse(Header header)
        {
            return header.BethesdaVersion >= 100;
        }

        /// <summary>
        /// SSE only.
        /// </summary>
        public static bool BsSse(Header header)
        {
            return header.BethesdaVersion == 100;
        }

        /// <summary>
        /// Fallout 4 strictly, excluding stream 132 and 139 in dev files.
        /// </summary>
        public static bool BsFo4(Header header)
        {
            return header.BethesdaVersion == 130;
        }
        
        /// <summary>
        /// Fallout 4/76 including dev files.
        /// </summary>
        public static bool BsFo4_2(Header header)
        {
            return header.BethesdaVersion is >= 130 and <= 139;
        }

        /// <summary>
        /// Later than Bethesda 130.
        /// </summary>
        public static bool BsGt130(Header header)
        {
            return header.BethesdaVersion > 130;
        }

        /// <summary>
        /// Bethesda 130 and later.
        /// </summary>
        public static bool BsGte130(Header header)
        {
            return header.BethesdaVersion >= 130;
        }

        /// <summary>
        /// Bethesda 132 and later.
        /// </summary>
        public static bool BsGte132(Header header)
        {
            return header.BethesdaVersion >= 132;
        }
        
        /// <summary>
        /// Bethesda 152 and later.
        /// </summary>
        public static bool BsGte152(Header header)
        {
            return header.BethesdaVersion >= 152;
        }

        /// <summary>
        /// Fallout 76 stream 155 only.
        /// </summary>
        public static bool BsF76(Header header)
        {
            return header.BethesdaVersion == 155;
        }

        /// <summary>
        /// Bethesda 20.2 only.
        /// </summary>
        public static bool Bs202(Header header)
        {
            return header.Version == 0x14020007 && header.BethesdaVersion > 0;
        }
    }
}