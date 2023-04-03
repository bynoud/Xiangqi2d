using System.Threading.Tasks;
using UnityXiangqi;

namespace UnityXiangqi.Engine
{
    public interface IUCIEngine
    {
        void Start();

        void ShutDown();

        void SetupNewGame(Game game);

        Movement GetBestMove(int timeoutMS);
    }
}