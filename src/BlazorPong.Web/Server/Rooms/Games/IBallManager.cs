using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Rooms.Games
{
    public interface IBallManager
    {
        void OnPlayer1Hit(ref GameObject ball);
        void OnPlayer2Hit(ref GameObject ball);
        string Update(ref GameObject ball);
        bool VerifyObjectsCollision(GameObject gameObjectA, GameObject gameObjectB);
    }
}