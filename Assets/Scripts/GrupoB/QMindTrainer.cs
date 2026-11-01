using System;
using NavigationDJIA.Interfaces;
using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;

namespace GrupoB
{
    public class QMindTrainer : IQMindTrainer
    {
        private QMindTrainerParams _params;
        private WorldInfo _worldInfo;
        INavigationAlgorithm _navigationAlgorithm;

        private QTableStorage _qStorage;
        private QTable _qTable;

        private CellInfo _agentPosition;
        private CellInfo _otherPosition;

        private float _return;
        private float _returnAveraged;
        private System.Random _random = new System.Random();

        #region IQMindTrainer implementation

        public CellInfo AgentPosition => _agentPosition;
        public CellInfo OtherPosition => _otherPosition;

        public int CurrentEpisode { get; private set; }
        public int CurrentStep { get; private set; }

        public float Return => _return;
        public float ReturnAveraged => _returnAveraged;

        public event EventHandler OnEpisodeStarted;
        public event EventHandler OnEpisodeFinished;

        #endregion

        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            _params = qMindTrainerParams;
            _worldInfo = worldInfo;
            _navigationAlgorithm = navigationAlgorithm;
            _navigationAlgorithm.Initialize(worldInfo);

            _qStorage = new QTableStorage("TablaQ.csv");
            _qTable = new QTable(_qStorage);

            CurrentEpisode = 0;
            StartNewEpisode();
        }

        private void StartNewEpisode()
        {
            CurrentEpisode++;
            CurrentStep = 0;
            _return = 0f;
            _returnAveraged = 0f;

            _agentPosition = _worldInfo.RandomCell();
            _otherPosition = _worldInfo.RandomCell();

            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
        }

        private void EndEpisode()
        {
            //UnityEngine.Debug.Log($"Episodio {CurrentEpisode}: {CurrentStep} pasos");
            _qTable.SaveToCsv();

            OnEpisodeFinished?.Invoke(this, EventArgs.Empty);

            if (_params.episodes > 0 && CurrentEpisode >= _params.episodes)
            {
                return;
            }

            StartNewEpisode();
        }

        public void DoStep(bool train)
        {
            // Estado actual del agente
            string stateKey = BuildStateKey(_agentPosition, _otherPosition, _worldInfo);

            // Seleciona la acción a realizar
            QAction action = ChooseAction(stateKey, train);

            // Nuevos estados del agente y del oponente
            CellInfo newAgentPos = ApplyAction(_agentPosition, action);
            CellInfo newOtherPos = MoveOpponent(_otherPosition, newAgentPos.Walkable ? newAgentPos : _agentPosition);

            // Nuevo estado del agente
            string nextStateKey = BuildStateKey(newAgentPos, newOtherPos, _worldInfo);

            // Calcula la recompensa
            float reward = ComputeReward(newAgentPos, newOtherPos);

            if (train)
            {
                UpdateQ(stateKey, action, reward, nextStateKey);
            }

            // actualiza las posiciones
            _agentPosition = newAgentPos;
            _otherPosition = newOtherPos;

            // Actualizamos estadísticas de recompensas
            CurrentStep++;
            _return += reward;
            _returnAveraged = (_returnAveraged * (CurrentStep - 1) + reward) / CurrentStep;

            // Comprobación de si estamos en el fin de episodio
            if (IsTerminalState(_agentPosition, _otherPosition))
            {
                EndEpisode();
            }
        }

        #region Parte a implementar por el alumno

        private string BuildStateKey(CellInfo agent, CellInfo other, WorldInfo _worldInfo)
        {
            var state = new QState(agent, other, _worldInfo);
            return state.ToKey();
        }

        /// <summary>
        /// ε-greedy: en entrenamiento, con probabilidad epsilon acción aleatoria;
        /// en el resto de casos, la mejor acción según la tabla Q.
        /// </summary>
        private QAction ChooseAction(string stateKey, bool train)
        {
            if (train && _random.NextDouble() < _params.epsilon)
                return (QAction)_random.Next(Enum.GetNames(typeof(QAction)).Length);

            return _qTable.GetBestAction(stateKey);
        }

        /// <summary>
        /// Q(s,a) = (1 - alpha) * Q(s,a) + alpha * (reward + gamma * max_a' Q(s',a')).
        /// En estados terminales (captura) Q(s',·) nunca se actualiza y vale 0,
        /// por lo que el objetivo se reduce a "reward" sin código adicional.
        /// </summary>
        private void UpdateQ(string stateKey, QAction action, float reward, string nextStateKey)
        {
            float oldQ = _qTable.GetQ(stateKey, action);
            float target = reward + _params.gamma * _qTable.GetMaxQ(nextStateKey);
            _qTable.SetQ(stateKey, action, (1 - _params.alpha) * oldQ + _params.alpha * target);
        }

        /// <summary>
        /// Capturado: -100. Sobrevive un paso: +1.
        /// </summary>
        private float ComputeReward(CellInfo agent, CellInfo other)
        {
            return agent.Equals(other) ? -100f : 1f;
        }

        /// <summary>
        /// Fin de episodio: el enemigo atrapa al agente, o se alcanza Max Steps
        /// (si vale -1 no se aplica). Así el episodio termina aunque el agente sea muy bueno.
        /// </summary>
        private bool IsTerminalState(CellInfo agent, CellInfo other)
        {
            return agent.Equals(other) || (_params.maxSteps > 0 && CurrentStep >= _params.maxSteps);
        }


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

            // Si la celda destino es un muro o está fuera del tablero, el agente se queda quieto.
            CellInfo next = _worldInfo[nx, ny];
            return next.Walkable ? next : agentCell;
        }


        private CellInfo MoveOpponent(CellInfo opponent, CellInfo target)
        {
            var path = _navigationAlgorithm.GetPath(opponent, target, 1);
            if (path == null) UnityEngine.Debug.Log("Sin camino al agente");
            if (path != null && path.Length > 0)
                return path[0];

            return opponent;
        }
        #endregion
    }
}