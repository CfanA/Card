namespace Game.Map
{
    /// <summary>
    /// Result of resolving a room encounter.
    /// </summary>
    public enum RoomCompletionResult
    {
        Cleared,
        Failed
    }

    /// <summary>
    /// Unified room completion sink implemented by map run flow.
    /// </summary>
    public interface IRoomCompletionSink
    {
        void CompleteRoom(int nodeId, MapRoomType roomType, RoomCompletionResult result, string rewardedCardId = null);
    }
}
