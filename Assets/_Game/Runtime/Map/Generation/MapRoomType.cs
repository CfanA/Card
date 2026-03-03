using System;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
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


