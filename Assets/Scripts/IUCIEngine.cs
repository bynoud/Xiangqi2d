using System.Threading.Tasks;
using UnityXiangqi;

namespace UnityXiangqi.Engine
{
    public interface IUCIEngine
    {
        void Start();

        void ShutDown();

        Task SetupNewGame(Game game);

        Task<Movement> GetBestMove(int timeoutMS);
    }
}