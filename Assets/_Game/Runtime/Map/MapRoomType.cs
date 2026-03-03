using System;

namespace Game.Map
{
    /// <summary>
    /// Room category used by the generated map nodes.
    /// </summary>
    [Serializable]
    public enum MapRoomType
    {
        Monster,
        Elite,
        Event,
        Shop,
        Rest,
        Treasure,
        Boss,
        Start
    }
}
