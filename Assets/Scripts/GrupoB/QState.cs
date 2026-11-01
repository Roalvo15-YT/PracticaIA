using NavigationDJIA.World;

namespace GrupoA
{
    /// <summary>
    /// Estado = (posición RELATIVA del enemigo recortada a [-4,4]) + (4 bits de muros adyacentes).
    /// Al no usar coordenadas absolutas, sirve para cualquier disposición de muros.
    /// </summary>
    public sealed class QState
    {
        private const int MaxDist = 4;

        public int DX { get; }      // other.x - agent.x (recortado)
        public int DY { get; }      // other.y - agent.y (recortado)
        public int Walls { get; }   // bits: 1=Arriba, 2=Abajo, 4=Izquierda, 8=Derecha bloqueados

        public QState(CellInfo agent, CellInfo other, WorldInfo world)
        {
            DX = System.Math.Clamp(other.x - agent.x, -MaxDist, MaxDist);
            DY = System.Math.Clamp(other.y - agent.y, -MaxDist, MaxDist);

            Walls = (world[agent.x, agent.y + 1].Walkable ? 0 : 1)
                  | (world[agent.x, agent.y - 1].Walkable ? 0 : 2)
                  | (world[agent.x - 1, agent.y].Walkable ? 0 : 4)
                  | (world[agent.x + 1, agent.y].Walkable ? 0 : 8);
        }

        public string ToKey()
        {
            return $"{DX},{DY}|{Walls}";
        }
    }
}