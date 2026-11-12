using JetBrains.Annotations;
using NavigationDJIA.World;
using QMind;

/// <summary>
/// TODO(alumno):
/// Define el "estado" que usará la Tabla Q para identificar cada situación del agente.
/// 
/// El estado debe contener toda la información necesaria para que el agente pueda
/// tomar decisiones informadas. Tú decides qué características incluir según lo
/// que consideres relevante para resolver el problema.
/// 
/// Ejemplos típicos de información que puede formar un estado:
///   - Posición del agente en la grid.
///   - Posición del otro personaje (enemigo).
///   - Distancia relativa entre agente y enemigo.
///   - Si hay muros en direcciones cercanas.
///   - Cualquier otro dato que consideres útil.
/// 
/// En este ejercicio te damos un ejemplo simple basado únicamente en las posiciones
/// del agente y del oponente. Puedes usarlo tal cual o ampliarlo.
/// 
/// IMPORTANTE: 
///  El estado debe poder convertirse a una clave única (string) mediante ToKey(),
///  ya que esa clave se usará como índice en la TablaQ y en el archivo CSV.
/// </summary>

namespace GrupoB
{
    public sealed class QState
    {
        public int AgentX { get; }
        public int AgentY { get; }
        public int OtherX { get; }
        public int OtherY { get; }

        public CeldaAdyacente[] CeldasAdyacentes = new CeldaAdyacente[4];
        public CeldaAdyacente GetCADistance(int i)
        {
            return this.CeldasAdyacentes[i];
        }

        public class CeldaAdyacente
        {
            public int CAAgentX;
            public int CAAgentY;
            public int Prioridad;
            public float Distance;

            public CeldaAdyacente(int x, int y)
            {
                this.CAAgentX = x;
                this.CAAgentY = y;
                this.Prioridad = 0;
                this.Distance = 0;
            }

            public int GetPriorty()
            {
                return this.Prioridad;
            }
            public void SetPriority(int p)
            {
                this.Prioridad = p;
            }
            public int GetCoorX()
            {
                return this.CAAgentX;
            }
            public int GetCoorY()
            {
                return this.CAAgentY;
            }
            public float GetDistance()
            {
                return this.Distance;
            }
            public void SetDistance(float d)
            {
                this.Distance = d;
            }
        }

        public QState(CellInfo agent, CellInfo other, WorldInfo worldInfo)
            //añadir al constructor la información de mundo
        {
            //Posición agente
            AgentX = agent.x;
            AgentY = agent.y;
            //Posición jugador
            OtherX = other.x;
            OtherY = other.y;
            //Posiciones adyacentes del agente (U. D. L. R)
            CeldasAdyacentes[0] = new CeldaAdyacente(AgentX, AgentY + 1); //U
            CeldasAdyacentes[1] = new CeldaAdyacente(AgentX, AgentY - 1); //D
            CeldasAdyacentes[2] = new CeldaAdyacente(AgentX - 1, AgentY); //L
            CeldasAdyacentes[3] = new CeldaAdyacente(AgentX + 1, AgentY); //R
            //Clasificar las celdas por prioridad
                //0--> Trnasitable (caso por defecto)
                //1--> No Transitable
                //2--> jugador
            for (int i=0; i<CeldasAdyacentes.Length; i++)
            {
                //obtener la celda real
                int coorX = CeldasAdyacentes[i].GetCoorX();
                int coorY = CeldasAdyacentes[i].GetCoorY();
                CellInfo celda = worldInfo[coorX, coorY];
                CeldasAdyacentes[i].SetDistance(celda.Distance(other, CellInfo.DistanceType.Euclidean));
                //Comprobar si es Trnasitable
                if (celda.Walkable)
                {
                    CeldasAdyacentes[i].SetPriority(0);
                }
                //Comprobar si es No Transitable
                else
                {
                    CeldasAdyacentes[i].SetPriority(1);
                }
                //Comprobar si es jugador
                if (CeldasAdyacentes[i].GetCoorX() == OtherX && CeldasAdyacentes[i].GetCoorY() == OtherY)
                //celda.Equals(other) es otra opción
                {
                    CeldasAdyacentes[i].SetPriority(2);
                }
            }

        }

        public string ToKey()
        {
            //Clave que representa el estado
            return $"{AgentX},{AgentY}|{OtherX},{OtherY}||" + 
                $"U_{CeldasAdyacentes[0].GetPriorty()}|" + 
                $"D_{CeldasAdyacentes[1].GetPriorty()}|" + 
                $"L_{CeldasAdyacentes[2].GetPriorty()}|" + 
                $"R_{CeldasAdyacentes[3].GetPriorty()}|";
        }
    }
}