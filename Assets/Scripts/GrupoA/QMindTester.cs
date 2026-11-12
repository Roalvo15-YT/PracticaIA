using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;

namespace GrupoA
{
    public class QMindTester : IQMind
    {
        private WorldInfo _worldInfo;
        private QTableStorage _qStorage;
        private QTable _qTable;

        public void Initialize(WorldInfo worldInfo)
        {
            //UnityEngine.Debug.Log("[Test] Initialize llamado");
            _worldInfo = worldInfo;

            _qStorage = new QTableStorage("TablaQ.csv");
            _qTable = new QTable(_qStorage);
        }

        public CellInfo GetNextStep(CellInfo currentPosition, CellInfo otherPosition)
        {
            string stateKey = BuildStateKey(currentPosition, otherPosition, _worldInfo);

            //UnityEngine.Debug.Log($"[Test] {stateKey} | en tabla: {_qStorage.Data.ContainsKey(stateKey)} | estados: {_qStorage.Data.Count}");

            QAction bestAction = _qTable.GetBestAction(stateKey);

            CellInfo nextPosition = ApplyAction(currentPosition, bestAction);

            return nextPosition;
        }

        // Debe ser idéntico al de QMindTrainer
        private string BuildStateKey(CellInfo agent, CellInfo other, WorldInfo _worldInfo)
        {
            var state = new QState(agent, other, _worldInfo);
            return state.ToKey();
        }

        // Debe ser idéntico al de QMindTrainer (si el destino es muro/fuera del tablero, se queda quieto)
        private CellInfo ApplyAction(CellInfo agentCell, QAction action)
        {
            int nx = agentCell.x;
            int ny = agentCell.y;

            switch (action)
            {
                case QAction.Up: ny += 1; break;
                case QAction.Down: ny -= 1; break;
                case QAction.Right: nx += 1; break;
                case QAction.Left: nx -= 1; break;
                case QAction.Stay: return agentCell;
            }

            CellInfo next = _worldInfo[nx, ny];
            return next.Walkable ? next : agentCell;
        }
    }
}