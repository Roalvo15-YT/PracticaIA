using System;
using System.Linq;
using QMind;

namespace GrupoB
{
    public class QTable
    {
        private readonly QTableStorage _storage;
        private readonly string[] _actionNames;

        public QTable(QTableStorage storage)
        {
            _storage = storage;
            _actionNames = Enum.GetNames(typeof(QAction));
        }

        private void EnsureState(string stateKey)
        {
            if (!_storage.Data.ContainsKey(stateKey))
            {
                _storage.Data[stateKey] = new float[_actionNames.Length];
            }
        }

        public float GetQ(string stateKey, QAction action)
        {
            EnsureState(stateKey);
            return _storage.Data[stateKey][(int)action];
        }

        public void SetQ(string stateKey, QAction action, float value)
        {
            EnsureState(stateKey);
            _storage.Data[stateKey][(int)action] = value;
        }

        public float GetMaxQ(string stateKey)
        {
            EnsureState(stateKey);
            return _storage.Data[stateKey].Max();
        }

        public QAction GetBestAction(string stateKey)
        {
            EnsureState(stateKey);
            var q = _storage.Data[stateKey];
            return (QAction)Array.IndexOf(q, q.Max());
        }

        public void SaveToCsv()
        {
            _storage.Save();
        }
    }
}