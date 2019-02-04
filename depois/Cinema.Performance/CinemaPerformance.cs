using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Cinema.Performance
{
    public class CinemaPerformance
    {
        private const string NomeCategoriaContadores = "_Cinema";
        private const string NomeContadorAverageTimer32 = "AverageTimer32Sample";
        private const string NomeContadorAverageTimer32Base = "AverageTimer32SampleBase";
        public static PerformanceCounter ContadorAverageTimer32;
        public static PerformanceCounter ContadorAverageTimer32Base;

        /// <summary>
        //Se a categoria não existir, crie a categoria e saia.
        //Os contadores de desempenho não devem ser criados e usados imediatamente.
        //Há um tempo de latência para ativar os contadores, eles devem ser criados
        //antes de executar o aplicativo que usa os contadores.
        //Execute esta amostra uma segunda vez para usar a categoria.
        /// </summary>
        /// <returns></returns>
        public static bool ConfigurarCategoria()
        {
            if (!PerformanceCounterCategory.Exists(NomeCategoriaContadores))
            {
                //1) criar uma coleção de dados com CounterCreationDataCollection
                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection();

                //2) criar os contadores com CounterCreationData
                CounterCreationData dadosContador = new CounterCreationData();
                //3) definir as propriedades do contador
                dadosContador.CounterType = PerformanceCounterType.AverageTimer32;
                dadosContador.CounterName = NomeContadorAverageTimer32;
                counterDataCollection.Add(dadosContador);

                //2) criar os contadores com CounterCreationData
                CounterCreationData dadosContadorBase = new CounterCreationData();
                //3) definir as propriedades do contador
                dadosContadorBase.CounterType = PerformanceCounterType.AverageBase;
                dadosContadorBase.CounterName = NomeContadorAverageTimer32Base;
                counterDataCollection.Add(dadosContadorBase);

                //4) criar a categoria e passar a coleção de dados com os contadores
                PerformanceCounterCategory.Create(NomeCategoriaContadores,
                    "Demonstra o uso do contador de performance de tipo AverageTimer32.",
                    PerformanceCounterCategoryType.SingleInstance, counterDataCollection);

                Console.WriteLine("Tecle algo para fechar o programa");
                Console.WriteLine("e rode novamente para utilizar os contadores de performance.");
                Console.ReadKey();

                return (true);
            }
            else
            {
                Console.WriteLine("Categoria já existe - " + NomeCategoriaContadores);
                return (false);
            }
        }

        public static void CriarContadores()
        {
            // Cria os contadores.
            ContadorAverageTimer32 = new PerformanceCounter(NomeCategoriaContadores,
                NomeContadorAverageTimer32,
                false);

            ContadorAverageTimer32Base = new PerformanceCounter(NomeCategoriaContadores,
                NomeContadorAverageTimer32Base,
                false);

            ContadorAverageTimer32.RawValue = 0;
            ContadorAverageTimer32Base.RawValue = 0;
        }

        public static async Task ColetarAmostrasAsync(List<CounterSample> amostras)
        {
            Random r = new Random(DateTime.Now.Millisecond);

            Stopwatch stopwatch = new Stopwatch();

            for (int j = 0; j < 100; j++)
            {
                stopwatch.Start();

                await Task.Delay(r.Next(500, 1000));
                stopwatch.Stop();

                var performanceCounterTicks = stopwatch.Elapsed.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;
                ContadorAverageTimer32.IncrementBy(performanceCounterTicks);
                ContadorAverageTimer32Base.Increment();

                stopwatch.Reset();
            }

        }
    }

}
