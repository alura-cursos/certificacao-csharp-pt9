using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cinema.Dados
{
    public class CinemaDB
    {
        //bool ModoDebug = false;

        private readonly string databaseServer;
        private readonly string masterDatabase;
        private readonly string databaseName;

        public CinemaDB(string databaseServer, string masterDatabase, string databaseName)
        {
            this.databaseServer = databaseServer;
            this.masterDatabase = masterDatabase;
            this.databaseName = databaseName;
        }

        public async Task CriarBancoDeDadosAsync()
        {
            Trace.WriteLine("Entrando no método CriarBancoDeDadosAsync", "MÉTODO");
            Trace.Indent();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await CriarBancoAsync();
            await CriarTabelasAsync();
            stopwatch.Stop();
            Console.WriteLine("Criação do banco e tabelas: {0} milissegundos", stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            await InserirRegistrosAsync();
            stopwatch.Stop();
            Console.WriteLine("Inserção no banco de dados: {0} milissegundos", stopwatch.ElapsedMilliseconds);

            Trace.Unindent();
            Trace.WriteLine("Saindo do método CriarBancoDeDadosAsync", "MÉTODO");
        }

        private async Task CriarBancoAsync()
        {
            string sql = $@"IF EXISTS (SELECT * FROM sys.databases WHERE name = N'{databaseName}')
                    BEGIN
                        DROP DATABASE [{databaseName}]
                    END;
                    CREATE DATABASE [{databaseName}];";
            await ExecutarComandoAsync(sql, masterDatabase);
        }

        private async Task CriarTabelasAsync()
        {
            string sql = $@"CREATE TABLE [dbo].[Diretores] (
                        [Id]   INT           IDENTITY (1, 1) NOT NULL,
                        [Nome] VARCHAR (255) NOT NULL
                    );
                    CREATE TABLE [dbo].[Filmes] (
                        [Id]        INT           IDENTITY (1, 1) NOT NULL,
                        [DiretorId] INT           NOT NULL,
                        [Titulo]    VARCHAR (255) NOT NULL,
                        [Ano]       INT           NOT NULL,
                        [Minutos]   INT           NOT NULL
                    );";
            await ExecutarComandoAsync(sql, databaseName);
        }

        private async Task InserirRegistrosAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var sql = new StringBuilder();

            using (Stream stream = assembly.GetManifestResourceStream("Cinema.Dados.Diretores.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    sql.AppendLine($"INSERT Diretores (Nome) VALUES ('{line}');");
                }
            }

            using (Stream stream = assembly.GetManifestResourceStream("Cinema.Dados.Filmes.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] fields = line.Split(',');
                    string diretorId = fields[0];
                    string titulo = fields[1];
                    string ano = fields[2];
                    string minutos = fields[3];
                    sql.AppendLine($"INSERT Filmes (DiretorId, Titulo, Ano, Minutos) VALUES ({diretorId},'{titulo}',{ano},{minutos})");
                }
            }

            await ExecutarComandoAsync(sql.ToString(), databaseName);
        }

        private async Task ExecutarComandoAsync(string sql, string banco)
        {
            SqlConnection conexao = new SqlConnection($"Server={databaseServer};Integrated security=SSPI;database={banco}");
            SqlCommand comando = new SqlCommand(sql, conexao);
            try
            {
                conexao.Open();
                await comando.ExecuteNonQueryAsync();
                Trace.WriteLine($"Script executado com sucesso: {sql}", "SCRIPT");
            }
            catch (System.Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
            finally
            {
                if (conexao.State == ConnectionState.Open)
                {
                    conexao.Close();
                }
            }
        }

        public async Task<IList<Filme>> GetFilmes()
        {
            Trace.WriteLine("Entrando no método GetFilmes", "MÉTODO");
            Trace.Indent();
            IList<Filme> filmes = new List<Filme>();
            string connectionString = $"Server={databaseServer};Integrated security=SSPI;database={databaseName}";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    " SELECT d.Nome AS Diretor, f.Titulo AS Titulo" +
                    " FROM Filmes AS f" +
                    " INNER JOIN Diretores AS d" +
                    "   ON d.Id = f.DiretorId"
                    , connection);
                SqlDataReader reader = await command.ExecuteReaderAsync();

#line hidden
                while (reader.Read())
                {
                    string diretor = reader["Diretor"].ToString();
                    string titulo = reader["Titulo"].ToString();
                    filmes.Add(new Filme(diretor, titulo));
                }
#line default
            }

#if MODO_DEBUG && MODO_DEBUG_DETALHADO
#error Você não pode usar mais de um modo de debug!
#endif

#if MODO_DEBUG
            Trace.WriteLine("O método GetFilmes() foi executado com sucesso.");
#elif MODO_DEBUG_QUANTIDADE
                Trace.WriteLine("O método GetFilmes() foi executado com sucesso. {0} filmes retornados.", filmes.Count);
//#elif MODO_DEBUG_DETALHADO
//            ExibirFilmesJson(filmes);            
#endif

#pragma warning disable CS0618 // Type or member is obsolete
            ExibirFilmesJson(filmes);
#pragma warning restore CS0618 // Type or member is obsolete
            Trace.Unindent();
            Trace.WriteLine("Saindo do método GetFilmes", "MÉTODO");
            return filmes;
        }

        [Conditional("MODO_DEBUG_DETALHADO")]
        [Obsolete("Este método está obsoleto. Utilize o novo método ExibirFilmesJsonFormatado")]
        [DebuggerStepThrough]
        void ExibirFilmesJson(IList<Filme> filmes)
        {
            Trace.WriteLine("O método GetFilmes() foi executado com sucesso. {0}", JsonConvert.SerializeObject(filmes));
        }

        [Conditional("MODO_DEBUG_DETALHADO")]
        void ExibirFilmesJsonFormatado(IList<Filme> filmes)
        {
            Trace.WriteLine("O método GetFilmes() foi executado com sucesso. {0}", JsonConvert.SerializeObject(filmes, Formatting.Indented));
        }
    }
}
